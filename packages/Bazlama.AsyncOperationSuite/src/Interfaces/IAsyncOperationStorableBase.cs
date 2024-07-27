namespace Bazlama.AsyncOperationSuite.Interfaces;

public interface IAsyncOperationStorableBase
{
    string _id { get; set; }
    DateTime CreatedAt { get; set; }
    string OwnerId { get; set; }
}