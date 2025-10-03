namespace Bazlama.AsyncOperationSuite.Configurations;

public class AsyncOperationSuiteConfiguration
{
    public const string Name = "AsyncOperationSuiteConfiguration";
	/// <summary>
	/// The number of worker threads to process asynchronous operations.
	/// </summary>
	public int WorkerCount { get; set; } = AsyncOperationSuiteConfigurationDefults.WorkerCount;
	
	/// <summary>
	/// The maximum number of operations that can be queued for processing.
	/// </summary>
	public int QueueSize { get; set; } = AsyncOperationSuiteConfigurationDefults.QueueSize;

	/// <summary>
	/// A dictionary to define maximum concurrent processing constraints for different payload types.
	/// Key: Payload type name (string)
	/// Value: Maximum number of concurrent operations allowed for that payload type (int)
	/// </summary>
	public Dictionary<string, int> PayloadConcurrentConstraints { get; set; } = [];

    public static AsyncOperationSuiteConfiguration Default => new();
}
