using Bazlama.AsyncOperationSuite.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bazlama.AsyncOperationSuite.Services;

public partial class AsyncOperationService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AsyncOperationManager service is starting.");

        var tasks = Enumerable.Range(0, _config.WorkerCount).Select(_ => Task.Run(() => Worker(stoppingToken)));
        await Task.WhenAll(tasks);

        _logger.LogInformation("AsyncOperationManager service is stopping.");
    }

    private async Task Worker(CancellationToken stoppingToken)
    {
        await foreach (var payload in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            if (payload == null) continue;
            var payloadType = payload.GetType().Name;

            var operation = await OperationRepository.GetAsync(payload.OperationId, stoppingToken);
            if (operation == null)
            {
                _logger.LogError("Operation {OperationId} not found", payload.OperationId);
                continue;
            }
           
            var processType = GetTypesFromPayload(payload.GetType().Name).OperationType;
            if (processType == null)
            {
                _logger.LogError("Operation process type not found for payload type {PayloadType}", payload.GetType());
                continue;
            }

            if (_payloadSemaphores.TryGetValue(payloadType, out var semaphore))
            {
                await semaphore.WaitAsync(stoppingToken);
            }

            var process = CreateOperationProcessFromPayload(payload, operation);
            if (process.processInstance == null)
            {
                _logger.LogError("Operation process not found for payload type {PayloadType}", payload.GetType());
                continue;
            }

            if (process.executeMetodInfo == null)
            {
                _logger.LogError("ExecuteAsync method not found for process type {ProcessType}", processType.Name);
                continue;
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationTokenSource.Token, stoppingToken);

            var activeProcess = new AsyncOperationActiveProcess
            {
                CreatedAt = DateTime.Now,
                OwnerId = operation.OwnerId,
                OperationId = operation._id,
                PayloadId = payload._id,
                PayloadType = payload.GetType().Name,
                CancellationTokenSource = cancellationTokenSource
            };

            if (!_activeProcesses.TryAdd(activeProcess.OperationId, activeProcess))
            {
                _logger.LogError("Failed to add operation process {OperationId} to the dictionary.", activeProcess.OperationId);
                continue;
            }

            try
            {
                _logger.LogInformation("Processing operation {OperationId} with payload {PayloadId}", operation._id, payload._id);

                if (process.executeMetodInfo.Invoke(process.processInstance, 
                    [_serviceProvider, _logger, linkedCancellationTokenSource.Token]) is Task task)
                {
                    await task;
                }
                else
                {
                    _logger.LogError("ExecuteAsync method did not return a Task for process type {ProcessType}", processType.Name);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operation {OperationId} was cancelled", activeProcess.OperationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing operation {OperationId}", activeProcess.OperationId);
            }
            finally
            {
                _channelCapacitySemaphore.Release();
                semaphore?.Release();
                if (!_activeProcesses.TryRemove(activeProcess.OperationId, out _))
                {
                    _logger.LogError("Failed to remove operation process {OperationId} from the dictionary.", activeProcess.OperationId);
                }
            }
        }
    }
}