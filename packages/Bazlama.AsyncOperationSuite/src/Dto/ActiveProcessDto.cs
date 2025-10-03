using Bazlama.AsyncOperationSuite.Models;

namespace Bazlama.AsyncOperationSuite.Dto;

public record ActiveProcessDto
{
    public required DateTime CreatedAt { get; set; }
    public required double RunningTimeMs { get; set; }
    public required string OwnerId { get; set; }
    public required string OperationId { get; set; }
    public required string PayloadId { get; set; }
    public required string PayloadType { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public string OperationDescription { get; set; } = string.Empty;

	public AsyncOperationStatus Status { get; set; } = AsyncOperationStatus.Pending;
	public int Progress { get; set; }
	public string? ProgressMessage { get; set; }
}
