using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.MemoryStorage.Repositories;
using Bazlama.AsyncOperationSuite.Models;

namespace Bazlama.AsyncOperationSuite.MemoryStorage;

public class AsyncOperationMemoryStorage : IAsyncOperationStorage
{
    public IAsyncOperationRepository<AsyncOperation>? Operation { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationPayloadBase>? Payload { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationProgress>? Progress { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationResult>? Result { get; }

    public AsyncOperationMemoryStorage()
    {
        Operation = new AsyncOperationMemoBaseRepo<AsyncOperation>();
        Payload = new AsyncOperationMemoBaseChildRepo<AsyncOperationPayloadBase>();
        Progress = new AsyncOperationMemoBaseChildRepo<AsyncOperationProgress>();
        Result = new AsyncOperationMemoBaseChildRepo<AsyncOperationResult>();
    }

    public string CreateNewId()
    {
        return Guid.NewGuid().ToString();
    }
}
