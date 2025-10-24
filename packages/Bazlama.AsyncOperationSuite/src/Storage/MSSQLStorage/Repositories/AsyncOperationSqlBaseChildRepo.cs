using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Repositories;

public class AsyncOperationSqlBaseChildRepo<T> : IAsyncOperationRepositoryChild<T> 
    where T : IAsyncOperationStorableChild
{
    private readonly MSSQLStorageConfiguration _config;
    private readonly ILogger _logger;
    private readonly string _tableName;

    public AsyncOperationSqlBaseChildRepo(MSSQLStorageConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _tableName = GetTableName();
    }

    private string GetTableName()
    {
        return typeof(T).Name switch
        {
            nameof(AsyncOperationPayloadBase) => "AsyncOperationPayloads",
            nameof(AsyncOperationProgress) => "AsyncOperationProgress",
            nameof(AsyncOperationResult) => "AsyncOperationResults",
            _ => typeof(T).Name + "s"
        };
    }

    public async Task<T?> CreateAsync(T item, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = GetInsertSql();

            using var command = new SqlCommand(sql, connection);
            AddParameters(command, item);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item in {TableName}", _tableName);
            throw;
        }
    }

    public async Task<T?> UpsertAsync(T item, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = GetUpsertSql();

            using var command = new SqlCommand(sql, connection);
            AddParameters(command, item);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting item in {TableName}", _tableName);
            throw;
        }
    }

    public async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"SELECT * FROM {_tableName} WHERE _id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapFromReader(reader);
            }

            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item from {TableName} with id {Id}", _tableName, id);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetAllByOprationId(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"SELECT * FROM {_tableName} WHERE OperationId = @OperationId ORDER BY CreatedAt";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@OperationId", SqlDbType.NVarChar).Value = operationId;

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
            _logger.LogError(ex, "Error getting items from {TableName} with operation id {OperationId}", _tableName, operationId);
            throw;
        }
    }

    public async Task<T?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"SELECT TOP(1) * FROM {_tableName} WHERE OperationId = @OperationId ORDER BY CreatedAt DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@OperationId", SqlDbType.NVarChar).Value = operationId;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapFromReader(reader);
            }

            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest item from {TableName} with operation id {OperationId}", _tableName, operationId);
            throw;
        }
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"DELETE FROM {_tableName} WHERE _id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Id", SqlDbType.NVarChar).Value = id;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from {TableName} with id {Id}", _tableName, id);
            throw;
        }
    }

    public async Task<T?> UpdateAsync(T item, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = GetUpdateSql();

            using var command = new SqlCommand(sql, connection);
            AddParameters(command, item);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0 ? item : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item in {TableName}", _tableName);
            throw;
        }
    }

    private string GetInsertSql()
    {
        return _tableName switch
        {
            "AsyncOperationPayloads" => $@"
                INSERT INTO {_tableName} 
                (_id, CreatedAt, OwnerId, OperationId, PayloadType, Name, Description, PayloadData)
                VALUES 
                (@Id, @CreatedAt, @OwnerId, @OperationId, @PayloadType, @Name, @Description, @PayloadData)",
            
            "AsyncOperationProgress" => $@"
                INSERT INTO {_tableName} 
                (_id, CreatedAt, OwnerId, OperationId, Status, Progress, ProgressMessage)
                VALUES 
                (@Id, @CreatedAt, @OwnerId, @OperationId, @Status, @Progress, @ProgressMessage)",
            
            "AsyncOperationResults" => $@"
                INSERT INTO {_tableName} 
                (_id, CreatedAt, OwnerId, OperationId, Result, ResultMessage)
                VALUES 
                (@Id, @CreatedAt, @OwnerId, @OperationId, @Result, @ResultMessage)",
            
            _ => $@"
                INSERT INTO {_tableName} 
                (_id, CreatedAt, OwnerId, OperationId)
                VALUES 
                (@Id, @CreatedAt, @OwnerId, @OperationId)"
        };
    }

    private string GetUpdateSql()
    {
        return _tableName switch
        {
            "AsyncOperationPayloads" => $@"
                UPDATE {_tableName} 
                SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, OperationId = @OperationId, 
                    PayloadType = @PayloadType, Name = @Name, Description = @Description, PayloadData = @PayloadData
                WHERE _id = @Id",
            
            "AsyncOperationProgress" => $@"
                UPDATE {_tableName} 
                SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, OperationId = @OperationId, 
                    Status = @Status, Progress = @Progress, ProgressMessage = @ProgressMessage
                WHERE _id = @Id",
            
            "AsyncOperationResults" => $@"
                UPDATE {_tableName} 
                SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, OperationId = @OperationId, 
                    Result = @Result, ResultMessage = @ResultMessage
                WHERE _id = @Id",
            
            _ => $@"
                UPDATE {_tableName} 
                SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, OperationId = @OperationId
                WHERE _id = @Id"
        };
    }

    private string GetUpsertSql()
    {
        return _tableName switch
        {
            "AsyncOperationPayloads" => $@"
                MERGE {_tableName} AS target
                USING (SELECT @Id AS _id) AS source ON target._id = source._id
                WHEN MATCHED THEN
                    UPDATE SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, OperationId = @OperationId, 
                               PayloadType = @PayloadType, Name = @Name, Description = @Description, PayloadData = @PayloadData
                WHEN NOT MATCHED THEN
                    INSERT (_id, CreatedAt, OwnerId, OperationId, PayloadType, Name, Description, PayloadData)
                    VALUES (@Id, @CreatedAt, @OwnerId, @OperationId, @PayloadType, @Name, @Description, @PayloadData);",
            
            "AsyncOperationProgress" => $@"
                MERGE {_tableName} AS target
                USING (SELECT @Id AS _id) AS source ON target._id = source._id
                WHEN MATCHED THEN
                    UPDATE SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, OperationId = @OperationId, 
                               Status = @Status, Progress = @Progress, ProgressMessage = @ProgressMessage
                WHEN NOT MATCHED THEN
                    INSERT (_id, CreatedAt, OwnerId, OperationId, Status, Progress, ProgressMessage)
                    VALUES (@Id, @CreatedAt, @OwnerId, @OperationId, @Status, @Progress, @ProgressMessage);",
            
            "AsyncOperationResults" => $@"
                MERGE {_tableName} AS target
                USING (SELECT @Id AS _id) AS source ON target._id = source._id
                WHEN MATCHED THEN
                    UPDATE SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, OperationId = @OperationId, 
                               Result = @Result, ResultMessage = @ResultMessage
                WHEN NOT MATCHED THEN
                    INSERT (_id, CreatedAt, OwnerId, OperationId, Result, ResultMessage)
                    VALUES (@Id, @CreatedAt, @OwnerId, @OperationId, @Result, @ResultMessage);",
            
            _ => $@"
                MERGE {_tableName} AS target
                USING (SELECT @Id AS _id) AS source ON target._id = source._id
                WHEN MATCHED THEN
                    UPDATE SET CreatedAt = @CreatedAt, OwnerId = @OwnerId, OperationId = @OperationId
                WHEN NOT MATCHED THEN
                    INSERT (_id, CreatedAt, OwnerId, OperationId)
                    VALUES (@Id, @CreatedAt, @OwnerId, @OperationId);"
        };
    }

    private void AddParameters(SqlCommand command, T item)
    {
        command.Parameters.Add("@Id", SqlDbType.NVarChar).Value = item._id;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = item.CreatedAt;
        command.Parameters.Add("@OwnerId", SqlDbType.NVarChar).Value = item.OwnerId ?? (object)DBNull.Value;
        command.Parameters.Add("@OperationId", SqlDbType.NVarChar).Value = item.OperationId ?? (object)DBNull.Value;

        // Add specific parameters based on type
        if (item is AsyncOperationPayloadBase payload)
        {
            command.Parameters.Add("@PayloadType", SqlDbType.NVarChar).Value = payload.PayloadType ?? (object)DBNull.Value;
            command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = payload.Name ?? (object)DBNull.Value;
            command.Parameters.Add("@Description", SqlDbType.NVarChar).Value = payload.Description ?? (object)DBNull.Value;
            command.Parameters.Add("@PayloadData", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(payload, payload.GetType()) ?? (object)DBNull.Value;
        }
        else if (item is AsyncOperationProgress progress)
        {
            command.Parameters.Add("@Status", SqlDbType.Int).Value = (int)progress.Status;
            command.Parameters.Add("@Progress", SqlDbType.Int).Value = progress.Progress;
            command.Parameters.Add("@ProgressMessage", SqlDbType.NVarChar).Value = progress.ProgressMessage ?? (object)DBNull.Value;
        }
        else if (item is AsyncOperationResult result)
        {
            command.Parameters.Add("@Result", SqlDbType.NVarChar).Value = result.Result ?? (object)DBNull.Value;
            command.Parameters.Add("@ResultMessage", SqlDbType.NVarChar).Value = result.ResultMessage ?? (object)DBNull.Value;
        }
    }

	private T? MapFromReader(SqlDataReader reader)
	{
		if (typeof(T) == typeof(AsyncOperationPayloadBase))
		{
			var payloadData = reader["PayloadData"].ToString();
            if (payloadData == null) return default;

			var payloadObject = JsonSerializer.Deserialize<AsyncOperationPayloadDynamic>(payloadData);
            if (payloadObject == null) return default;
			
            return (T)(object)payloadObject;
		}

		if (typeof(T) == typeof(AsyncOperationProgress))
		{
			var progress = new AsyncOperationProgress
			{
				_id = reader["_id"].ToString()!,
				CreatedAt = (DateTime)reader["CreatedAt"],
				OwnerId = reader["OwnerId"].ToString()!,
				OperationId = reader["OperationId"].ToString()!,
				Status = (AsyncOperationStatus)(int)reader["Status"],
				Progress = (int)reader["Progress"],
				ProgressMessage = reader["ProgressMessage"]?.ToString()
			};
			return (T)(object)progress;
		}

	    if (typeof(T) == typeof(AsyncOperationResult))
		{
			var result = new AsyncOperationResult
			{
				_id = reader["_id"].ToString()!,
				CreatedAt = (DateTime)reader["CreatedAt"],
				OwnerId = reader["OwnerId"].ToString()!,
				OperationId = reader["OperationId"].ToString()!,
				Result = reader["Result"].ToString()!,
				ResultMessage = reader["ResultMessage"]?.ToString()
			};
			return (T)(object)result;
		}

		return default;
	}
}
