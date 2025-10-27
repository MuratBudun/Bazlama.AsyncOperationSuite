using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Bazlama.AsyncOperationSuite.src.Storage.MSSQLStorage.Repositories;

public class AsyncOperationSqlBaseRepo<T> : IAsyncOperationRepository<T> where T : IAsyncOperationStorable
{
    private readonly MSSQLStorageConfiguration _config;
    private readonly ILogger _logger;
    private readonly string _tableName;
    private readonly string _connectionString;

    public AsyncOperationSqlBaseRepo(MSSQLStorageConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _tableName = GetTableName();
        _connectionString = BuildConnectionString(config);
    }

    private static string BuildConnectionString(MSSQLStorageConfiguration config)
    {
        var baseConnectionString = config.ConnectionString;
        
        // Check if connection string already contains pool size settings
        if (baseConnectionString.Contains("Max Pool Size", StringComparison.OrdinalIgnoreCase) ||
            baseConnectionString.Contains("Min Pool Size", StringComparison.OrdinalIgnoreCase))
        {
            // User has already set pool sizes in the connection string, don't override
            return baseConnectionString;
        }
        
        // Build connection string with pool size settings
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            MaxPoolSize = config.MaxPoolSize,
            MinPoolSize = config.MinPoolSize
        };
        
        return builder.ConnectionString;
    }

    private string GetTableName()
    {
        return typeof(T).Name switch
        {
            nameof(AsyncOperation) => "AsyncOperations",
            _ => typeof(T).Name + "s"
        };
    }

    public async Task<T?> CreateAsync(T item, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $@"
                INSERT INTO {_tableName} 
                (_id, CreatedAt, OwnerId, Name, Status, Description, StartedAt, CompletedAt, FailedAt, CanceledAt, ErrorMessage, InnerErrorMessage, ErrorStackTrace, ExecutionTimeMs)
                VALUES 
                (@Id, @CreatedAt, @OwnerId, @Name, @Status, @Description, @StartedAt, @CompletedAt, @FailedAt, @CanceledAt, @ErrorMessage, @InnerErrorMessage, @ErrorStackTrace, @ExecutionTimeMs)";

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = _config.CommandTimeout;
            AddParametersForAsyncOperation(command, item);

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (_config.EnableDetailedLogging)
            {
                _logger.LogInformation("Created new item in {TableName} with id {Id}", _tableName, (item as AsyncOperation)?._id);
            }
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item in {TableName}", _tableName);
            throw;
        }
    }

    public async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"SELECT * FROM {_tableName} WHERE _id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = _config.CommandTimeout;
            command.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapFromReader(reader);
            }

            if (_config.EnableDetailedLogging)
            {
                _logger.LogInformation("Item with id {Id} not found in {TableName}", id, _tableName);
            }

            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item from {TableName} with id {Id}", _tableName, id);
            throw;
        }
    }

    public async Task<T?> UpdateAsync(T item, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $@"
                UPDATE {_tableName} 
                SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, Name = @Name, Status = @Status, 
                    Description = @Description, StartedAt = @StartedAt, CompletedAt = @CompletedAt, 
                    FailedAt = @FailedAt, CanceledAt = @CanceledAt, ErrorMessage = @ErrorMessage, 
                    InnerErrorMessage = @InnerErrorMessage, ErrorStackTrace = @ErrorStackTrace, 
                    ExecutionTimeMs = @ExecutionTimeMs
                WHERE _id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = _config.CommandTimeout;
            AddParametersForAsyncOperation(command, item);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (_config.EnableDetailedLogging)
            {
                _logger.LogInformation("Updated item in {TableName} with id {Id}", _tableName, (item as AsyncOperation)?._id);
            }

            return rowsAffected > 0 ? item : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item in {TableName}", _tableName);
            throw;
        }
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // For AsyncOperation table, we need to manually handle cascade delete
            // since foreign keys are set to NO ACTION to avoid SQL Server cascade conflicts
            if (_tableName == "AsyncOperations")
            {
                using var transaction = connection.BeginTransaction();
                try
                {
                    // Delete related records first
                    var deleteChildrenSql = @"
                        DELETE FROM [AsyncOperationResults] WHERE [OperationId] = @Id;
                        DELETE FROM [AsyncOperationProgress] WHERE [OperationId] = @Id;
                        DELETE FROM [AsyncOperationPayloads] WHERE [OperationId] = @Id;";
                    
                    using var childCommand = new SqlCommand(deleteChildrenSql, connection, transaction);
                    childCommand.CommandTimeout = _config.CommandTimeout;
                    childCommand.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;
                    await childCommand.ExecuteNonQueryAsync(cancellationToken);
                    
                    // Delete the main operation
                    var sql = $"DELETE FROM {_tableName} WHERE _id = @Id";
                    using var command = new SqlCommand(sql, connection, transaction);
                    command.CommandTimeout = _config.CommandTimeout;
                    command.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    
                    transaction.Commit();
                    
                    if (_config.EnableDetailedLogging)
                    {
                        _logger.LogInformation("Cascade deleted operation {OperationId} and all related records", id);
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            else
            {
                // Regular delete for non-AsyncOperation tables
                var sql = $"DELETE FROM {_tableName} WHERE _id = @Id";
                using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = _config.CommandTimeout;
                command.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from {TableName} with id {Id}", _tableName, id);
            throw;
        }
    }

    public async Task<T?> UpsertAysnc(T item, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $@"
                MERGE {_tableName} AS target
                USING (SELECT @Id AS _id) AS source ON target._id = source._id
                WHEN MATCHED THEN
                    UPDATE SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, Name = @Name, Status = @Status, 
                               Description = @Description, StartedAt = @StartedAt, CompletedAt = @CompletedAt, 
                               FailedAt = @FailedAt, CanceledAt = @CanceledAt, ErrorMessage = @ErrorMessage, 
                               InnerErrorMessage = @InnerErrorMessage, ErrorStackTrace = @ErrorStackTrace, 
                               ExecutionTimeMs = @ExecutionTimeMs
                WHEN NOT MATCHED THEN
                    INSERT (_id, CreatedAt, OwnerId, Name, Status, Description, StartedAt, CompletedAt, FailedAt, CanceledAt, ErrorMessage, InnerErrorMessage, ErrorStackTrace, ExecutionTimeMs)
                    VALUES (@Id, @CreatedAt, @OwnerId, @Name, @Status, @Description, @StartedAt, @CompletedAt, @FailedAt, @CanceledAt, @ErrorMessage, @InnerErrorMessage, @ErrorStackTrace, @ExecutionTimeMs);";

            using var command = new SqlCommand(sql, connection);
            AddParametersForAsyncOperation(command, item);

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (_config.EnableDetailedLogging)
            {
                _logger.LogInformation("Upserted item in {TableName} with id {Id}", _tableName, (item as AsyncOperation)?._id);
            }

            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting item in {TableName}", _tableName);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetLastOperationsAsync(
        int count, List<AsyncOperationStatus> status,
        string? ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var whereClause = BuildWhereClause(status, ownerId);
            var sql = $@"
                SELECT TOP(@Count) * FROM {_tableName}
                {whereClause}
                ORDER BY CreatedAt DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Count", SqlDbType.Int).Value = count;
            AddStatusAndOwnerParameters(command, status, ownerId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var results = new List<T>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var item = MapFromReader(reader);
                if (item != null)
                    results.Add(item);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last operations from {TableName}", _tableName);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetOperationsAsync(
        DateTime startDate, DateTime endDate, List<AsyncOperationStatus> status,
        string? ownerId, string? search,
        bool isDesc = true, int pageNumber = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var whereClause = BuildWhereClauseWithDatesAndSearch(status, ownerId, search);
            var orderClause = isDesc ? "ORDER BY CreatedAt DESC" : "ORDER BY CreatedAt ASC";
            var offset = (pageNumber - 1) * pageSize;

            var sql = $@"
                SELECT * FROM {_tableName}
                {whereClause}
                {orderClause}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@StartDate", SqlDbType.DateTime2).Value = startDate;
            command.Parameters.Add("@EndDate", SqlDbType.DateTime2).Value = endDate;
            command.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
            command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
            AddStatusAndOwnerParameters(command, status, ownerId);
            
            if (!string.IsNullOrEmpty(search))
            {
                command.Parameters.Add("@Search", SqlDbType.NVarChar).Value = $"%{search}%";
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var results = new List<T>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var item = MapFromReader(reader);
                if (item != null)
                    results.Add(item);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting operations from {TableName}", _tableName);
            throw;
        }
    }

    private void AddParametersForAsyncOperation(SqlCommand command, T item)
    {
        if (item is AsyncOperation operation)
        {
            command.Parameters.Add("@Id", SqlDbType.NVarChar).Value = operation._id;
            command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = operation.CreatedAt;
            command.Parameters.Add("@OwnerId", SqlDbType.NVarChar).Value = operation.OwnerId ?? (object)DBNull.Value;
            command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = operation.Name ?? (object)DBNull.Value;
            command.Parameters.Add("@Status", SqlDbType.Int).Value = (int)operation.Status;
            command.Parameters.Add("@Description", SqlDbType.NVarChar).Value = operation.Description ?? (object)DBNull.Value;
            command.Parameters.Add("@StartedAt", SqlDbType.DateTime2).Value = operation.StartedAt ?? (object)DBNull.Value;
            command.Parameters.Add("@CompletedAt", SqlDbType.DateTime2).Value = operation.CompletedAt ?? (object)DBNull.Value;
            command.Parameters.Add("@FailedAt", SqlDbType.DateTime2).Value = operation.FailedAt ?? (object)DBNull.Value;
            command.Parameters.Add("@CanceledAt", SqlDbType.DateTime2).Value = operation.CanceledAt ?? (object)DBNull.Value;
            command.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar).Value = operation.ErrorMessage ?? (object)DBNull.Value;
            command.Parameters.Add("@InnerErrorMessage", SqlDbType.NVarChar).Value = operation.InnerErrorMessage ?? (object)DBNull.Value;
            command.Parameters.Add("@ErrorStackTrace", SqlDbType.NVarChar).Value = operation.ErrorStackTrace ?? (object)DBNull.Value;
            command.Parameters.Add("@ExecutionTimeMs", SqlDbType.Int).Value = operation.ExecutionTimeMs;
        }
    }

    private T? MapFromReader(SqlDataReader reader)
    {
        if (typeof(T) == typeof(AsyncOperation))
        {
            var operation = new AsyncOperation(new DummyPayload())
            {
                _id = reader["_id"].ToString()!,
                CreatedAt = (DateTime)reader["CreatedAt"],
                OwnerId = reader["OwnerId"].ToString()!,
                Name = reader["Name"].ToString()!,
                Status = (AsyncOperationStatus)(int)reader["Status"],
                Description = reader["Description"].ToString()!,
                StartedAt = reader["StartedAt"] as DateTime?,
                CompletedAt = reader["CompletedAt"] as DateTime?,
                FailedAt = reader["FailedAt"] as DateTime?,
                CanceledAt = reader["CanceledAt"] as DateTime?,
                ErrorMessage = reader["ErrorMessage"].ToString(),
                InnerErrorMessage = reader["InnerErrorMessage"].ToString(),
                ErrorStackTrace = reader["ErrorStackTrace"].ToString(),
                ExecutionTimeMs = (int)reader["ExecutionTimeMs"]
            };
            return (T)(object)operation;
        }

        return default;
    }

    private string BuildWhereClause(List<AsyncOperationStatus> status, string? ownerId)
    {
        var conditions = new List<string>();

        if (status.Count > 0)
        {
            var statusConditions = string.Join(",", status.Select((_, i) => $"@Status{i}"));
            conditions.Add($"Status IN ({statusConditions})");
        }

        if (!string.IsNullOrEmpty(ownerId))
        {
            conditions.Add("OwnerId = @OwnerId");
        }

        return conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";
    }

    private string BuildWhereClauseWithDatesAndSearch(List<AsyncOperationStatus> status, string? ownerId, string? search)
    {
        var conditions = new List<string>
        {
            "CreatedAt >= @StartDate",
            "CreatedAt <= @EndDate"
        };

        if (status.Count > 0)
        {
            var statusConditions = string.Join(",", status.Select((_, i) => $"@Status{i}"));
            conditions.Add($"Status IN ({statusConditions})");
        }

        if (!string.IsNullOrEmpty(ownerId))
        {
            conditions.Add("OwnerId = @OwnerId");
        }

        if (!string.IsNullOrEmpty(search))
        {
            conditions.Add("(Name LIKE @Search OR Description LIKE @Search)");
        }

        return $"WHERE {string.Join(" AND ", conditions)}";
    }

    private void AddStatusAndOwnerParameters(SqlCommand command, List<AsyncOperationStatus> status, string? ownerId)
    {
        for (int i = 0; i < status.Count; i++)
        {
            command.Parameters.Add($"@Status{i}", SqlDbType.Int).Value = (int)status[i];
        }

        if (!string.IsNullOrEmpty(ownerId))
        {
            command.Parameters.Add("@OwnerId", SqlDbType.NVarChar).Value = ownerId;
        }
    }

    // Dummy payload class for AsyncOperation constructor
    private class DummyPayload : AsyncOperationPayloadBase
    {
        public DummyPayload()
        {
            OwnerId = string.Empty;
            Name = string.Empty;
            Description = string.Empty;
        }
    }
}
