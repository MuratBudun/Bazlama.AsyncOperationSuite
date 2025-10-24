using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Configurations;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Repositories;

public class AsyncOperationMemoBaseRepo<T> : IAsyncOperationRepository<T> where T : IAsyncOperationStorable
{
    private readonly ConcurrentDictionary<string, T> _items = new();
	
	private readonly MemoryStorageConfiguration _config;
	private readonly ILogger _logger;
	private readonly AsyncOperationMemoCleanup<T> _cleanup;

	public AsyncOperationMemoBaseRepo(MemoryStorageConfiguration config, ILogger logger)
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

	public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		if (_items.TryGetValue(id, out var item))
		{
			return Task.FromResult<T?>(item);
		}

		return Task.FromResult<T?>(default);
	}

	public Task<T?> UpdateAsync(T item, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(item);
		if (!_items.ContainsKey(item._id))
		{
			_logger.LogError("Attempt to update a non-existing item with id: {ItemId}", item._id);
			throw new ArgumentException("Item not found");
		}

		_items[item._id] = item;

		return Task.FromResult<T?>(item);
	}

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        _items.TryRemove(id, out _);
        return Task.CompletedTask;
    }

	public Task<T?> UpsertAysnc(T item, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(item);
		_items[item._id] = item;
		CheckLimitsAndCleanup();
		return Task.FromResult<T?>(item);
	}

    public Task<IEnumerable<T>> GetLastOperationsAsync(
		int count, List<AsyncOperationStatus> status,
		string? ownerId, CancellationToken cancellationToken = default)
	{
		var results = _items.Values
			.Where(x => (status.Count == 0 || status.Contains(x.Status))
				&& (string.IsNullOrEmpty(ownerId) || x.OwnerId == ownerId))
			.OrderByDescending(x => x.CreatedAt)
			.Take(count);
		return Task.FromResult(results);
	}

	public Task<IEnumerable<T>> GetOperationsAsync(
        DateTime startDate, 
        DateTime endDate, 
        List<AsyncOperationStatus> status, 
        string? ownerId, string? search, 
        bool isDesc = true, int pageNumber = 1, int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var results = _items.Values
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate
                && (status.Count == 0 || status.Contains(x.Status))
                && (string.IsNullOrEmpty(ownerId) || x.OwnerId == ownerId)
                && (string.IsNullOrEmpty(search) || 
                    x.Name != null && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Description != null && x.Description.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(x => isDesc ? x.CreatedAt : DateTime.MinValue)
            .ThenBy(x => isDesc ? DateTime.MinValue : x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return Task.FromResult(results);
    }

	private void CheckLimitsAndCleanup()
	{
		_cleanup.CheckLimitsAndCleanup(_items);
	}
}