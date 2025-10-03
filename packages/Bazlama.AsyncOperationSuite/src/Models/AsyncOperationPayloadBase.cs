using Bazlama.AsyncOperationSuite.Interfaces;

namespace Bazlama.AsyncOperationSuite.Models;

public abstract class AsyncOperationPayloadBase : IAsyncOperationStorableChild
{
    public string _id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string OwnerId { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;

    public string PayloadType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}