using Bazlama.AsyncOperationSuite.Configurations;
using Bazlama.AsyncOperationSuite.Exceptions;
using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;

namespace Bazlama.AsyncOperationSuite.Services;

public partial class AsyncOperationService : BackgroundService
{
    private readonly Dictionary<string, Type> _payloadTypes = [];
    private readonly Dictionary<Type, Type> _processTypes = [];

    private readonly ILogger<AsyncOperationService> _logger;
    private readonly IAsyncOperationStorage _storage;
    private readonly IServiceProvider _serviceProvider;

    private readonly Channel<AsyncOperationPayloadBase> _channel;
    private readonly ConcurrentDictionary<string, AsyncOperationActiveProcess> _activeProcesses;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _payloadSemaphores;
    private readonly SemaphoreSlim _channelCapacitySemaphore;
    private readonly AsyncOperationSuiteConfiguration _config;

    public IAsyncOperationRepository<AsyncOperation> OperationRepository { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationPayloadBase> PayloadRepository { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationProgress> ProgressRepository { get; }
    public IAsyncOperationRepositoryChild<AsyncOperationResult> ResultRepository { get; }

    public AsyncOperationService(
        IServiceProvider serviceProvider,
        IAsyncOperationStorage storage,
        ILogger<AsyncOperationService> logger,
        IOptions<AsyncOperationSuiteConfiguration> config)
    {
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _storage = storage;
        _logger = logger;

        if (_storage == null || _storage.Operation == null || _storage.Payload == null ||
            _storage.Progress == null || _storage.Result == null)
        {
            _logger.LogError("Storage is null");
            throw new ArgumentNullException(nameof(storage));
        }

        OperationRepository = _storage.Operation;
        PayloadRepository = _storage.Payload;
        ProgressRepository = _storage.Progress;
        ResultRepository = _storage.Result;

        _activeProcesses = new ConcurrentDictionary<string, AsyncOperationActiveProcess>();

        _payloadSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        _config.PayloadConcurrentConstraints
            .Where(item => item.Value > 0)
            .Select(item => _payloadSemaphores.TryAdd(item.Key, new SemaphoreSlim(item.Value, item.Value)));

        _channel = Channel.CreateBounded<AsyncOperationPayloadBase>(
            new BoundedChannelOptions(_config.QueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        _channelCapacitySemaphore = new SemaphoreSlim(_config.QueueSize, _config.QueueSize);

        _logger.LogInformation("AsyncOperationManager was created.");
        _logger.LogInformation("Worker count: {WorkerCount}", _config.WorkerCount);
        _logger.LogInformation("Queue size: {QueueSize}", _config.QueueSize);
        _logger.LogInformation("Storage: {StorageType}", _storage.GetType().Name);

        RegisterPayloadTypes();
    }

    public override void Dispose()
    {
        if (_channel.Writer.TryComplete())
            _logger.LogInformation("Channel writer completed.");

        foreach (var process in _activeProcesses.Values)
        {
            process.CancellationTokenSource.Cancel();
            process.CancellationTokenSource.Dispose();
        }

        _activeProcesses.Clear();

        base.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<AsyncOperation> PublishPayloadAsync(
        AsyncOperationPayloadBase payload, 
        bool waitForQueueSpace = false,
        bool waitForPayloadSlotSpace = false,
        CancellationToken cancellationToken = default)
    {
        if (payload == null)
        {
            _logger.LogError("Payload is null");
            throw new ArgumentNullException(nameof(payload));
        }

        if (!IsExistOperationProcessForPayload(payload))
        {
            _logger.LogError("Operation process not found for payload type {PayloadType}", payload.GetType());
            throw new OperationProcessNotFoundForPayloadException($"Operation process not found for payload type {payload.GetType()}");
        }

        payload._id = _storage.CreateNewId();
        var operation = new AsyncOperation(payload)
        {
            _id = _storage.CreateNewId()
        };

        SemaphoreSlim? semaphore = null;
        if (waitForPayloadSlotSpace)
        {
            var payloadType = payload.GetType().Name;
            if (_payloadSemaphores.TryGetValue(payloadType, out semaphore) && semaphore.CurrentCount == 0)
            {
                if (semaphore.CurrentCount == 0)
                {
                    _logger.LogWarning("No available slot for payload type {PayloadType}", payloadType);
                    throw new PayloadTypeLimitExceededException($"No available slot for payload type {payloadType}");
                }
            }
        }

        if (_channelCapacitySemaphore.CurrentCount <= 0 && !waitForQueueSpace)
        {
            _logger.LogWarning("The channel queue is full.");
            throw new QueueFullException("The channel queue is full.");
        }

        try
        {
            var storedOperation = await OperationRepository.CreateAsync(operation, cancellationToken);
            if (storedOperation == null)
            {
                _logger.LogError("Stored operation entity not found for {OperationId}, {OperationName}", 
                    operation._id, operation.Name);

                throw new AsyncOperationStorageException(
                    $"Stored operation entity not found for {operation._id}, {operation.Name}");
            }
            payload.OperationId = storedOperation._id;

            var storedPayload = await PayloadRepository.CreateAsync(payload, cancellationToken);
            if (storedPayload == null)
            {
                _logger.LogError("Stored payload entity not found for {PayloadId}, {PayloadName}",
                    payload._id, payload.Name);

                throw new AsyncOperationStorageException(
                    $"Stored payload entity not found for {payload._id}, {payload.Name}");
            }

            if (waitForQueueSpace)
            {
                _channelCapacitySemaphore.Wait();
                await _channel.Writer.WriteAsync(payload, cancellationToken);
                return storedOperation;
            }

            _channelCapacitySemaphore.Wait();
            if (_channel.Writer.TryWrite(payload))
                return storedOperation;

            try
            {
                await PayloadRepository.RemoveAsync(payload._id, cancellationToken);
                await OperationRepository.RemoveAsync(operation._id, cancellationToken);
            }
            catch(Exception exc)
            {
                _logger.LogError(exc, "Rollback failed for operation {OperationId} and payload {PayloadId}",
                    operation._id, payload._id);

                throw new AsyncOperationStorageException("Rollback failed for operation and payload");
            }

            _logger.LogWarning("The channel queue is full.");
            throw new QueueFullException("The channel queue is full.");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    public void CancelProcess(string operationId, 
        bool useThrowIfCancellationRequested = false, 
        bool waitForCompletion = false, 
        int timeoutMs = 30000)
    {
        if (!_activeProcesses.TryRemove(operationId, out var activeProcess))
        {
            _logger.LogWarning("Async Operation Process {OperationId} not found.", operationId);
            throw new AsyncOperationNotFoundException(operationId);
        }

        activeProcess.CancellationTokenSource.Cancel();
        _logger.LogInformation("Async Operation Process {OperationId}, {OperationName} canceled.",
            activeProcess.OperationId, activeProcess.OperationName);

        if (useThrowIfCancellationRequested)
        {
            activeProcess.CancellationTokenSource.Token.ThrowIfCancellationRequested();
            return;
        }

        if (!waitForCompletion) return;

        if (timeoutMs == 0) {
            activeProcess.CancellationTokenSource.Token.WaitHandle.WaitOne();
            return;
        }

        if (!activeProcess.CancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs)))
        {
            throw new TimeoutException("The operation timed out.");
        }
    }

    private (Object? processInstance, MethodInfo? executeMetodInfo)
        CreateOperationProcessFromPayload(AsyncOperationPayloadBase payload, AsyncOperation asyncOperation)
    {
        if (payload == null) return (null, null);

        var payloadTypeName = payload.GetType().Name;
        if (payloadTypeName == null) return (null, null);

        if (!_payloadTypes.TryGetValue(payloadTypeName, out var payloadTypeClass))
        {
            _logger.LogWarning("Payload type {PayloadTypeName} not found", payloadTypeName);
            return (null, null);
        }

        if (!_processTypes.TryGetValue(payloadTypeClass, out var processType))
        {
            _logger.LogWarning("Process type not found for payload type {PayloadTypeName}", payloadTypeName);
            return (null, null);
        }

        var processInstance = Activator.CreateInstance(processType, payload, asyncOperation, this);
        var executeMethod = processType.GetMethod("ExecuteAsync");
        if (executeMethod == null)
        {
            _logger.LogWarning("ExecuteAsync method not found for process type {ProcessType}", processType.Name);
            return (null, null);
        }

        return (processInstance, executeMethod);
    }
}
