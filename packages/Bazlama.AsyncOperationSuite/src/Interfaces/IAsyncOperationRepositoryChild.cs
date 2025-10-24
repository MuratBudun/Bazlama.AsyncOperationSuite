namespace Bazlama.AsyncOperationSuite.Interfaces;

public interface IAsyncOperationRepositoryChild<T>
    where T : IAsyncOperationStorableChild
{
    Task<T?> CreateAsync(T item, CancellationToken cancellationToken = default);
    Task<T?> UpsertAsync(T item, CancellationToken cancellationToken = default);
	Task<T?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<T?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllByOprationId(string operationId, CancellationToken cancellationToken = default);
    Task<T?> UpdateAsync(T item, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
