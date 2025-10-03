using Bazlama.AsyncOperationSuite.Models;

namespace Bazlama.AsyncOperationSuite.Interfaces;

public interface IAsyncOperationStorable: IAsyncOperationStorableBase
{
    string Name { get; set; }
    string Description { get; set; }
    AsyncOperationStatus Status { get; set; }
}
