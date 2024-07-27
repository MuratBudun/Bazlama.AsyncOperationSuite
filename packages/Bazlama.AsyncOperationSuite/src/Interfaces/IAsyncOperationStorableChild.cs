namespace Bazlama.AsyncOperationSuite.Interfaces;

public interface IAsyncOperationStorableChild : IAsyncOperationStorableBase
{
    string OperationId { get; set; }
}
