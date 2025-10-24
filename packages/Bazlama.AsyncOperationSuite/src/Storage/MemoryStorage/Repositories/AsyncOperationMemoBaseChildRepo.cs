using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Configurations;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Repositories;

public class AsyncOperationMemoBaseChildRepo<T> : IAsyncOperationRepositoryChild<T> 
    where T : IAsyncOperationStorableChild
{
    private readonly ConcurrentDictionary<string, T> _items = new();
	private readonly MemoryStorageConfiguration _config;
	private readonly ILogger _logger;
	private readonly AsyncOperationMemoCleanup<T> _cleanup;

	public AsyncOperationMemoBaseChildRepo(MemoryStorageConfiguration config, ILogger logger)
	{
		_config = config;
		_logger = logger;
		_cleanup = new AsyncOperationMemoCleanup<T>(_config, _logger);
	}

    public Task<T?> CreateAsync(T item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.TryAdd(item._id, item))
        {
            _logger.LogError("Attempt to create an item that already exists with id: {ItemId}", item._id);
            throw new ArgumentException("Item already exists");
        }

        CheckLimitsAndCleanup();
        return Task.FromResult<T?>(item);
    }

    public Task<T?> UpsertAsync(T item, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(item);
		_items[item._id] = item;
		CheckLimitsAndCleanup();
		return Task.FromResult<T?>(item);
	}

	public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_items.TryGetValue(id, out var item))
        {
            return Task.FromResult<T?>(item);
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
        _items.TryRemove(id, out _);
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

	private void CheckLimitsAndCleanup()
	{
		_cleanup.CheckLimitsAndCleanup(_items);
	}
}
