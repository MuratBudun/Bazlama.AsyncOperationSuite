using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bazlama.AsyncOperationSuite;

public abstract class AsyncOperationProcess<T> where T : AsyncOperationPayloadBase
{
    public T Payload { get; private set; }
    public AsyncOperation AsyncOperation { get; private set; }
    public AsyncOperationService AsyncOperationService { get; private set; }
    public AsyncOperationProgress? Progress { get; private set; }
    public AsyncOperationResult? Result { get; private set; }

    public AsyncOperationProcess(
        T payload,
        AsyncOperation asyncOperation,
        AsyncOperationService asyncOperationService)
    {
        Payload = payload;
        AsyncOperation = asyncOperation;
        AsyncOperationService = asyncOperationService;

        ArgumentNullException.ThrowIfNull(Payload);
        ArgumentNullException.ThrowIfNull(AsyncOperation);
        ArgumentNullException.ThrowIfNull(AsyncOperationService);
    }

    protected virtual Task OnExecuteAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected virtual Task OnAfterExecuteAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected async Task PublishProgress(
        string message,
        int progress,
        CancellationToken cancellationToken)
    {
        var progressPayload = new AsyncOperationProgress
        {
            _id = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.Now,
            OperationId = AsyncOperation._id,
            ProgressMessage = message,
            Progress = progress,
            Status = AsyncOperation.Status
        };

        Progress = await AsyncOperationService.ProgressRepository.UpsertAysnc(progressPayload, cancellationToken);
    }

    private async Task UpdateStatus(AsyncOperationStatus status, CancellationToken cancellationToken)
    {
        AsyncOperation.Status = status;
        await AsyncOperationService.OperationRepository.UpdateAsync(AsyncOperation, cancellationToken);

        Progress ??= new AsyncOperationProgress
        {
            _id = Guid.NewGuid().ToString(),
            OwnerId = AsyncOperation.OwnerId,
            CreatedAt = DateTime.Now,
            OperationId = AsyncOperation._id,
            ProgressMessage = status.ToString(),
            Progress = 0,
            Status = status
        };
        Progress.Status = status;
        Progress = await AsyncOperationService.ProgressRepository.UpsertAysnc(Progress, cancellationToken);

    }

    private async Task WriteResult(CancellationToken cancellationToken)
    {
        if (Result == null) return;
        Result = await AsyncOperationService.ResultRepository.UpsertAysnc(Result, cancellationToken);
    }

    public async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        try
        {
            AsyncOperation.StartedAt = DateTime.Now;
            await UpdateStatus(AsyncOperationStatus.Running, cancellationToken);

            await OnExecuteAsync(serviceProvider, logger, cancellationToken);
            stopWatch.Stop();

            AsyncOperation.CompletedAt = DateTime.Now;
            AsyncOperation.ExecutionTimeMs = (int)stopWatch.ElapsedMilliseconds;
            await WriteResult(cancellationToken);
            await UpdateStatus(AsyncOperationStatus.Completed, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            stopWatch.Stop();

            AsyncOperation.CanceledAt = DateTime.Now;
            AsyncOperation.FailedAt = DateTime.Now;
            AsyncOperation.ErrorMessage = "Operation was cancelled";
            AsyncOperation.ExecutionTimeMs = (int)stopWatch.ElapsedMilliseconds;
            await UpdateStatus(AsyncOperationStatus.Canceled, cancellationToken);
        }
        catch (Exception ex)
        {
            stopWatch.Stop();

            AsyncOperation.FailedAt = DateTime.Now;
            AsyncOperation.ErrorMessage = ex.Message;
            AsyncOperation.InnerErrorMessage = ex.InnerException?.Message;
            AsyncOperation.ErrorStackTrace = ex.StackTrace;
            AsyncOperation.ExecutionTimeMs = (int)stopWatch.ElapsedMilliseconds;
            await UpdateStatus(AsyncOperationStatus.Failed, cancellationToken);
        }
        finally
        {
            await OnAfterExecuteAsync(serviceProvider, logger, cancellationToken);
        }
    }
}
