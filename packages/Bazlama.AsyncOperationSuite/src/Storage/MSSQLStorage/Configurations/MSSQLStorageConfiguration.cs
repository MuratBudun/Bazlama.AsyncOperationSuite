namespace Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Configurations;

public class MSSQLStorageConfiguration
{
    public const string Name = "AsyncOperationSuiteConfiguration:MSSQLStorage";

    /// <summary>
    /// Connection string for the SQL Server database
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Command timeout in seconds (default: 30)
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Enable detailed logging for SQL operations
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Pool size for connection pooling (default: 100)
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Minimum pool size for connection pooling (default: 5)
    /// </summary>
    public int MinPoolSize { get; set; } = 5;
}