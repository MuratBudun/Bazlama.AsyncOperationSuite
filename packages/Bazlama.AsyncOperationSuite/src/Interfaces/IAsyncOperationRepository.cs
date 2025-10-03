using Bazlama.AsyncOperationSuite.Models;

namespace Bazlama.AsyncOperationSuite.Interfaces;

public interface IAsyncOperationRepository<T>
    where T : IAsyncOperationStorable
{
    Task<T?> CreateAsync(T item, CancellationToken cancellationToken = default);
    Task<T?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<T?> UpdateAsync(T item, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
    Task<T?> UpsertAysnc(T item, CancellationToken cancellationToken = default);

	Task<IEnumerable<T>> GetOperationsAsync(
        DateTime startDate, DateTime endDate, List<AsyncOperationStatus> status,
        string? ownerId, string? name,
        bool isDesc = true, int pageNumber = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);
}
