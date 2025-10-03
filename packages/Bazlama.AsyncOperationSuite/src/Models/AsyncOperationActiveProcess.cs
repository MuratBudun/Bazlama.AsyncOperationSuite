namespace Bazlama.AsyncOperationSuite.Models;

public class AsyncOperationActiveProcess
{
    public required DateTime CreatedAt { get; set; }
    public required string OwnerId { get; set; }
    public required string OperationId { get; set; }
    public required string PayloadId { get; set; }
    public required string PayloadType { get; set; }
    public required string ProcessType { get; set; }
	public string OperationName { get; set; } = string.Empty;
    public string OperationDescription { get; set; } = string.Empty;
	public required CancellationTokenSource CancellationTokenSource { get; set; }

	public AsyncOperationStatus Status { get; set; } = AsyncOperationStatus.Pending;
	public int Progress { get; set; }
	public string? ProgressMessage { get; set; }
}
