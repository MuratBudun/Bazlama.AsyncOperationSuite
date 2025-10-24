using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Configurations;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Services;

public class AsyncOperationMemoCleanup<T> where T : IAsyncOperationStorableBase
{
    private readonly MemoryStorageConfiguration _config;
    private readonly int _maxItems;
    private readonly ILogger _logger;
    private readonly object _cleanupLock = new();

    public AsyncOperationMemoCleanup(MemoryStorageConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;

        _maxItems = typeof(T).Name switch
        {
            nameof(AsyncOperation) => _config.MaxOperations,
            nameof(AsyncOperationPayloadBase) => _config.MaxPayloads,
            nameof(AsyncOperationProgress) => _config.MaxProgress,
            nameof(AsyncOperationResult) => _config.MaxResults,
            _ => 100
        };
    }

    public void CheckLimitsAndCleanup(ConcurrentDictionary<string, T> items)
    {
        if (!_config.EnableAutoCleanup) return;

        // Double-check locking pattern for performance
        int currentCount = items.Count;
        int threshold = (int)(_maxItems * _config.CleanupThreshold);

        if (currentCount < threshold) return;

        lock (_cleanupLock)
        {
            // Re-check count after acquiring lock
            currentCount = items.Count;

            if (currentCount >= threshold)
            {
                PerformCleanup(items);
            }
            else if (currentCount >= _maxItems)
            {
                // Hard limit reached
                if (_config.CleanupStrategy == MemoryCleanupStrategy.ThrowException)
                {
                    throw new InvalidOperationException(
                        $"Memory storage limit reached. Current: {currentCount}, Max: {_maxItems}, Strategy: {_config.CleanupStrategy}, Type: {typeof(T).Name}");
                }
                else
                {
                    PerformCleanup(items);
                }
            }
        }
    }

    private void PerformCleanup(ConcurrentDictionary<string, T> items)
    {
        // Take a snapshot of current items to avoid collection modified exception
        var currentItems = items.ToArray();

        var itemsToRemove = _config.CleanupStrategy switch
        {
            MemoryCleanupStrategy.RemoveOldest => currentItems
                .OrderBy(x => x.Value.CreatedAt)
                .Take(_config.CleanupBatchSize)
                .Select(x => x.Value),

            // For child repos, we don't have Status property, so fallback to RemoveOldest
            MemoryCleanupStrategy.RemoveCompletedFirst => GetItemsWithStatusCleanup(currentItems, AsyncOperationStatus.Completed),

            MemoryCleanupStrategy.RemoveFailedFirst => GetItemsWithStatusCleanup(currentItems, AsyncOperationStatus.Failed),

            _ => currentItems
                .OrderBy(x => x.Value.CreatedAt)
                .Take(_config.CleanupBatchSize)
                .Select(x => x.Value)
        };

        var removedCount = 0;
        foreach (var item in itemsToRemove.ToList())
        {
            if (items.TryRemove(item._id, out _))
            {
                removedCount++;
            }
        }

        _logger.LogInformation("Memory cleanup performed. Removed {RemovedCount} items. " +
            "Current: {CurrentCount}/{MaxCount}. Strategy: {Strategy}",
            removedCount, items.Count, _maxItems, _config.CleanupStrategy);
    }

    private IEnumerable<T> GetItemsWithStatusCleanup(KeyValuePair<string, T>[] currentItems, AsyncOperationStatus targetStatus)
    {
        // Check if T has Status property (IAsyncOperationStorable)
        if (typeof(IAsyncOperationStorable).IsAssignableFrom(typeof(T)))
        {
            var itemsWithStatus = currentItems
                .Where(x => ((IAsyncOperationStorable)x.Value).Status == targetStatus)
                .OrderBy(x => x.Value.CreatedAt)
                .Take(_config.CleanupBatchSize)
                .Select(x => x.Value);

            var remainingCount = _config.CleanupBatchSize - itemsWithStatus.Count();
            if (remainingCount > 0)
            {
                var otherItems = currentItems
                    .Where(x => ((IAsyncOperationStorable)x.Value).Status != targetStatus)
                    .OrderBy(x => x.Value.CreatedAt)
                    .Take(remainingCount)
                    .Select(x => x.Value);

                return itemsWithStatus.Concat(otherItems);
            }

            return itemsWithStatus;
        }
        else
        {
            // For types without Status property, fallback to RemoveOldest
            return currentItems
                .OrderBy(x => x.Value.CreatedAt)
                .Take(_config.CleanupBatchSize)
                .Select(x => x.Value);
        }
    }
}