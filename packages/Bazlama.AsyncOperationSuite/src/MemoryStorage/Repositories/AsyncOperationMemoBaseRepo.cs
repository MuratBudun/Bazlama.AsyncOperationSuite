using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Models;

namespace Bazlama.AsyncOperationSuite.MemoryStorage.Repositories;

public class AsyncOperationMemoBaseRepo<T> : IAsyncOperationRepository<T> where T : IAsyncOperationStorable
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

	public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		if (_items.ContainsKey(id))
		{
			return Task.FromResult<T?>(_items[id]);
		}

		return Task.FromResult<T?>(default);
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

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_items.ContainsKey(id))
        {
            _items.Remove(id);
        }

        return Task.CompletedTask;
    }

	public Task<T?> UpsertAysnc(T item, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(item);
		_items[item._id] = item;
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
                    (x.Name != null && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Description != null && x.Description.Contains(search, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(x => isDesc ? x.CreatedAt : DateTime.MinValue)
            .ThenBy(x => isDesc ? DateTime.MinValue : x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return Task.FromResult(results);
    }
}
