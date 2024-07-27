namespace Bazlama.AsyncOperationSuite.Configurations;

public class AsyncOperationSuiteConfiguration
{
    public const string Name = "AsyncOperationSuiteConfiguration";
    public int WorkerCount { get; set; } = AsyncOperationSuiteConfigurationDefults.WorkerCount;
    public int QueueSize { get; set; } = AsyncOperationSuiteConfigurationDefults.QueueSize;
    public Dictionary<string, int> PayloadConcurrentConstraints { get; set; } = [];

    public static AsyncOperationSuiteConfiguration Default => new();
}
