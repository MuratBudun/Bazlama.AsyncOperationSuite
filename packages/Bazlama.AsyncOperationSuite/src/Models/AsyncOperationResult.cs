using Bazlama.AsyncOperationSuite.Interfaces;

namespace Bazlama.AsyncOperationSuite.Models;

public class AsyncOperationResult : IAsyncOperationStorableChild
{
    public string _id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string OwnerId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;
    public string? ResultMessage { get; set; }
}
