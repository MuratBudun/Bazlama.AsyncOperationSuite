using Bazlama.AsyncOperationSuite.Interfaces;

namespace Bazlama.AsyncOperationSuite.Models;

public class AsyncOperation : IAsyncOperationStorable
{
    public string _id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string OwnerId { get; set; }
    public string Name { get; set; }
    public AsyncOperationStatus Status { get; set; } = AsyncOperationStatus.Pending;

    public string? Description { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? InnerErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public int ExecutionTimeMs { get; set; }

    public AsyncOperation(AsyncOperationPayloadBase payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        OwnerId = payload.OwnerId;
        Name = payload.Name;
        Description = payload.Description;
        CreatedAt = DateTime.Now;
    }
}
