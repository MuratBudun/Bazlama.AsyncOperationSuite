using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Repositories;
using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Configurations;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage;

namespace Bazlama.AsyncOperationSuite.Storage.MemoryStorage;

public class AsyncOperationMemoryStorage : IAsyncOperationStorage
{
	private readonly MemoryStorageConfiguration _config;
	private readonly ILogger<AsyncOperationMemoryStorage> _logger;

	public IAsyncOperationRepository<AsyncOperation>? Operation { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationPayloadBase>? Payload { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationProgress>? Progress { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationResult>? Result { get; }

	public AsyncOperationMemoryStorage(
		ILogger<AsyncOperationMemoryStorage> logger,
		IOptions<MemoryStorageConfiguration> config)
	{
		_logger = logger;
		_config = config.Value;


		// Calculate auto cleanup batch sizes
		if (_config.CleanupBatchSize == 0)
		{
			_config.CleanupBatchSize = Math.Max(1, _config.MaxOperations / 10);
		}

		Operation = new AsyncOperationMemoBaseRepo<AsyncOperation>(_config, logger);
        Payload = new AsyncOperationMemoBaseChildRepo<AsyncOperationPayloadBase>(_config, logger);
        Progress = new AsyncOperationMemoBaseChildRepo<AsyncOperationProgress>(_config, logger);
		Result = new AsyncOperationMemoBaseChildRepo<AsyncOperationResult>(_config, logger);

		_logger.LogInformation("Initialized AsyncOperationMemoryStorage with MaxOperations: {MaxOperations}, MaxPayloads: {MaxPayloads}, MaxProgress: {MaxProgress}, MaxResults: {MaxResults}, CleanupBatchSize: {CleanupBatchSize}, CleanupStrategy: {CleanupStrategy}, EnableAutoCleanup: {EnableAutoCleanup}, CleanupThreshold: {CleanupThreshold}",
			_config.MaxOperations, _config.MaxPayloads, _config.MaxProgress, _config.MaxResults, _config.CleanupBatchSize, _config.CleanupStrategy, _config.EnableAutoCleanup, _config.CleanupThreshold);
    }

	public string CreateNewId()
	{
		return Guid.NewGuid().ToString();
	}
}
