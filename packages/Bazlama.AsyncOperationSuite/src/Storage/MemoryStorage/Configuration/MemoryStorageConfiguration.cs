namespace Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Configurations;

public class MemoryStorageConfiguration
{
	public const string Name = "AsyncOperationSuiteConfiguration:MemoryStorage";

	/// <summary>
	/// Maximum number of operations to keep in memory (default: 100)
	/// </summary>
	public int MaxOperations { get; set; } = 100;

	/// <summary>
	/// Maximum number of payloads to keep in memory (default: 100)
	/// </summary>
	public int MaxPayloads { get; set; } = 100;

	/// <summary>
	/// Maximum number of progress records to keep in memory (default: 1000)
	/// </summary>
	public int MaxProgress { get; set; } = 1000;

	/// <summary>
	/// Maximum number of results to keep in memory (default: 1000)
	/// </summary>
	public int MaxResults { get; set; } = 100;

	/// <summary>
	/// Cleanup strategy when limit is reached
	/// </summary>
	public MemoryCleanupStrategy CleanupStrategy { get; set; } = MemoryCleanupStrategy.RemoveOldest;

	/// <summary>
	/// Number of items to remove when cleanup is triggered (default: 10% of max)
	/// </summary>
	public int CleanupBatchSize { get; set; } = 0; // 0 means auto-calculate as 10% of max

	/// <summary>
	/// Enable automatic cleanup when 90% of limit is reached
	/// </summary>
	public bool EnableAutoCleanup { get; set; } = true;

	/// <summary>
	/// Cleanup trigger threshold as percentage (default: 0.9 = 90%)
	/// </summary>
	public double CleanupThreshold { get; set; } = 0.9;
}

public enum MemoryCleanupStrategy
{
	/// <summary>
	/// Remove oldest items first (by CreatedAt)
	/// </summary>
	RemoveOldest,

	/// <summary>
	/// Remove completed operations first, then oldest
	/// </summary>
	RemoveCompletedFirst,

	/// <summary>
	/// Throw exception when limit is reached
	/// </summary>
	ThrowException,

	/// <summary>
	/// Remove failed operations first, then oldest
	/// </summary>
	RemoveFailedFirst
}