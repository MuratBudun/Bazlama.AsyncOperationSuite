using Bazlama.AsyncOperationSuite.Interfaces;

namespace Bazlama.AsyncOperationSuite.MemoryStorage.Repositories;

public class AsyncOperationMemoBaseChildRepo<T> : IAsyncOperationRepositoryChild<T> 
    where T : IAsyncOperationStorableChild
{
    private readonly Dictionary<string, T> _items = [];

    public Task<T?> CreateAsync(T item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item._id))
        {
            throw new ArgumentException("Item already exists");
        }

        _items.Add(item._id, item);

        return Task.FromResult<T?>(item);
    }

    public Task<T?> UpsertAysnc(T item, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(item);
		_items[item._id] = item;
		return Task.FromResult<T?>(item);
	}

	public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_items.ContainsKey(id))
        {
            return Task.FromResult<T?>(_items[id]);
        }

        return Task.FromResult<T?>(default);
    }

    public Task<IEnumerable<T>> GetAllByOprationId(string operationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.Values.Where(x => x.OperationId == operationId));
    }

    public Task<T?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.Values.LastOrDefault(x => x.OperationId == operationId));
    }

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_items.ContainsKey(id))
        {
            _items.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task<T?> UpdateAsync(T item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item._id))
        {
            throw new ArgumentException("Item not found");
        }

        _items[item._id] = item;

        return Task.FromResult<T?>(item);
    }
}
