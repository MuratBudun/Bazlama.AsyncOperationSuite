namespace Bazlama.AsyncOperationSuite.Dto;

public record EngineInfoDto
{
	public DateTime? StartedAt { get; set; }
	public int WorkerCount { get; set; }
	public int QueueSize {  get; set; }
	public int QueuePercentUsage
	{
		get
		{
			if (QueueSize == 0) return 0;
			return (int)Math.Round((double)CurrentQueueSize / QueueSize * 100);
		}
	}
	public int CurrentQueueSize { get; set; } = 0;
	public int ActiveProcessCount { get; set; } = 0;
	public Dictionary<string, int> PayloadConcurrentConstraints { get; set; } = [];
	public string StorageType { get; set; } = "Unknown";

}
