using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Repositories;
using Bazlama.AsyncOperationSuite.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Configurations;
using Bazlama.AsyncOperationSuite.src.Storage.MSSQLStorage.Repositories;

namespace Bazlama.AsyncOperationSuite.Storage.MSSQLStorage;

public class AsyncOperationMSSQLStorage : IAsyncOperationStorage
{
    private readonly string _connectionString;
    private readonly MSSQLStorageConfiguration _config;
    private readonly ILogger<AsyncOperationMSSQLStorage> _logger;

    public IAsyncOperationRepository<AsyncOperation>? Operation { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationPayloadBase>? Payload { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationProgress>? Progress { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationResult>? Result { get; }

    public AsyncOperationMSSQLStorage(
        ILogger<AsyncOperationMSSQLStorage> logger,
        IOptions<MSSQLStorageConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
        _connectionString = BuildConnectionString(_config);

        if (string.IsNullOrWhiteSpace(_connectionString))
		{
			throw new ArgumentNullException(
				nameof(_connectionString),
				"Connection string for MSSQL storage cannot be null or empty.");
		}

        // Ensure database schema exists
        CheckTableExists();

		Operation = new AsyncOperationSqlBaseRepo<AsyncOperation>(_config, _logger);
        Payload = new AsyncOperationSqlBaseChildRepo<AsyncOperationPayloadBase>(_config, _logger);
        Progress = new AsyncOperationSqlBaseChildRepo<AsyncOperationProgress>(_config, _logger);
        Result = new AsyncOperationSqlBaseChildRepo<AsyncOperationResult>(_config, _logger);
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
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConnectionString)
        {
            MaxPoolSize = config.MaxPoolSize,
            MinPoolSize = config.MinPoolSize
        };
        
        return builder.ConnectionString;
    }

    private void CheckTableExists()
	{
        try
        {
            _logger.LogInformation("Checking database schema...");
            
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            connection.Open();
            
            var schemaScript = GetDatabaseSchemaScript();
            
            using var command = new Microsoft.Data.SqlClient.SqlCommand(schemaScript, connection);
            command.CommandTimeout = _config.CommandTimeout;
            command.ExecuteNonQuery();
            
            _logger.LogInformation("Database schema verified/created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify/create database schema.");
            throw new InvalidOperationException("Database schema initialization failed.", ex);
        }
	}

    private static string GetDatabaseSchemaScript()
    {
        return @"
-- Create database schema for Bazlama.AsyncOperationSuite MSSQL Storage
-- This script is safe to run multiple times - it only creates tables and indexes if they don't exist

-- AsyncOperations table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AsyncOperations' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AsyncOperations](
        [_id] [nvarchar](255) NOT NULL PRIMARY KEY,
        [CreatedAt] [datetime2](7) NOT NULL,
        [OwnerId] [nvarchar](255) NULL,
        [Name] [nvarchar](255) NULL,
        [Status] [int] NOT NULL DEFAULT(0),
        [Description] [nvarchar](max) NULL,
        [StartedAt] [datetime2](7) NULL,
        [CompletedAt] [datetime2](7) NULL,
        [FailedAt] [datetime2](7) NULL,
        [CanceledAt] [datetime2](7) NULL,
        [ErrorMessage] [nvarchar](max) NULL,
        [InnerErrorMessage] [nvarchar](max) NULL,
        [ErrorStackTrace] [nvarchar](max) NULL,
        [ExecutionTimeMs] [int] NOT NULL DEFAULT(0)
    );
END

-- AsyncOperationPayloads table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AsyncOperationPayloads' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AsyncOperationPayloads](
        [_id] [nvarchar](255) NOT NULL PRIMARY KEY,
        [CreatedAt] [datetime2](7) NOT NULL,
        [OwnerId] [nvarchar](255) NULL,
        [OperationId] [nvarchar](255) NULL,
        [PayloadType] [nvarchar](255) NULL,
        [Name] [nvarchar](255) NULL,
        [Description] [nvarchar](max) NULL,
        [PayloadData] [nvarchar](max) NULL
    );
END

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AsyncOperationPayloads_OperationId')
BEGIN
    ALTER TABLE [dbo].[AsyncOperationPayloads]
    ADD CONSTRAINT FK_AsyncOperationPayloads_OperationId 
    FOREIGN KEY ([OperationId]) REFERENCES [AsyncOperations]([_id]) ON DELETE NO ACTION;
END

-- AsyncOperationProgress table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AsyncOperationProgress' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AsyncOperationProgress](
        [_id] [nvarchar](255) NOT NULL PRIMARY KEY,
        [CreatedAt] [datetime2](7) NOT NULL,
        [OwnerId] [nvarchar](255) NULL,
        [OperationId] [nvarchar](255) NULL,
        [Status] [int] NOT NULL DEFAULT(0),
        [Progress] [int] NOT NULL DEFAULT(0),
        [ProgressMessage] [nvarchar](max) NULL
    );
END

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AsyncOperationProgress_OperationId')
BEGIN
    ALTER TABLE [dbo].[AsyncOperationProgress]
    ADD CONSTRAINT FK_AsyncOperationProgress_OperationId 
    FOREIGN KEY ([OperationId]) REFERENCES [AsyncOperations]([_id]) ON DELETE NO ACTION;
END

-- AsyncOperationResults table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AsyncOperationResults' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AsyncOperationResults](
        [_id] [nvarchar](255) NOT NULL PRIMARY KEY,
        [CreatedAt] [datetime2](7) NOT NULL,
        [OwnerId] [nvarchar](255) NULL,
        [OperationId] [nvarchar](255) NULL,
        [Result] [nvarchar](max) NULL,
        [ResultMessage] [nvarchar](max) NULL
    );
END

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AsyncOperationResults_OperationId')
BEGIN
    ALTER TABLE [dbo].[AsyncOperationResults]
    ADD CONSTRAINT FK_AsyncOperationResults_OperationId 
    FOREIGN KEY ([OperationId]) REFERENCES [AsyncOperations]([_id]) ON DELETE NO ACTION;
END

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperations_Status' AND object_id = OBJECT_ID('AsyncOperations'))
BEGIN
    CREATE INDEX IX_AsyncOperations_Status ON [AsyncOperations]([Status]);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperations_OwnerId' AND object_id = OBJECT_ID('AsyncOperations'))
BEGIN
    CREATE INDEX IX_AsyncOperations_OwnerId ON [AsyncOperations]([OwnerId]);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperations_CreatedAt' AND object_id = OBJECT_ID('AsyncOperations'))
BEGIN
    CREATE INDEX IX_AsyncOperations_CreatedAt ON [AsyncOperations]([CreatedAt]);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperations_Status_OwnerId' AND object_id = OBJECT_ID('AsyncOperations'))
BEGIN
    CREATE INDEX IX_AsyncOperations_Status_OwnerId ON [AsyncOperations]([Status], [OwnerId]);
END

-- Foreign key indexes for better JOIN performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperationPayloads_OperationId' AND object_id = OBJECT_ID('AsyncOperationPayloads'))
BEGIN
    CREATE INDEX IX_AsyncOperationPayloads_OperationId ON [AsyncOperationPayloads]([OperationId]);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperationProgress_OperationId' AND object_id = OBJECT_ID('AsyncOperationProgress'))
BEGIN
    CREATE INDEX IX_AsyncOperationProgress_OperationId ON [AsyncOperationProgress]([OperationId]);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AsyncOperationResults_OperationId' AND object_id = OBJECT_ID('AsyncOperationResults'))
BEGIN
    CREATE INDEX IX_AsyncOperationResults_OperationId ON [AsyncOperationResults]([OperationId]);
END
";
    }

	public string CreateNewId()
    {
        return Guid.NewGuid().ToString();
    }
}