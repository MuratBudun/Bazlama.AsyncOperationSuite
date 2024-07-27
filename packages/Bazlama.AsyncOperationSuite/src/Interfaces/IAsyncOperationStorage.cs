using Bazlama.AsyncOperationSuite.Models;

namespace Bazlama.AsyncOperationSuite.Interfaces;

public interface IAsyncOperationStorage
{
    string CreateNewId();
    IAsyncOperationRepository<AsyncOperation>? Operation { get; }
    IAsyncOperationRepositoryChild<AsyncOperationPayloadBase>? Payload { get; }
    IAsyncOperationRepositoryChild<AsyncOperationProgress>? Progress { get; }
    IAsyncOperationRepositoryChild<AsyncOperationResult>? Result { get; }
}
