using Bazlama.AsyncOperationSuite.Dto;
using Bazlama.AsyncOperationSuite.Models;
using Microsoft.Extensions.Hosting;

namespace Bazlama.AsyncOperationSuite.Services;

public partial class AsyncOperationService : BackgroundService
{
	#region Engine ...
    public EngineInfoDto GetEngineInfo()
	{
		var engineInfo = new EngineInfoDto
		{
			StartedAt = _engineStartedAt,
			WorkerCount = _config.WorkerCount,
			QueueSize = _config.QueueSize,
			CurrentQueueSize = _channel.Reader.Count,
			ActiveProcessCount = _activeProcesses.Count,
			PayloadConcurrentConstraints = _config.PayloadConcurrentConstraints,
			StorageType = _storage?.GetType().Name ?? "Unknown"
		};
		return engineInfo;
	}
	#endregion

	#region Payload ...
	public List<RegisteredPayloadDto> GetRegisteredPayloads()
    {
        var registeredTypes = new List<RegisteredPayloadDto>();
        foreach (var (payloadType, processType) in GetRegisteredTypes())
        {   
            registeredTypes.Add(new RegisteredPayloadDto
            {
                PayloadTypeName = payloadType?.Name ?? "Unknown",
                ProcessTypeName = processType?.Name ?? "Unknown"
            });
        }

        return registeredTypes;
    }

    public Type? GetPayloadTypeByName(string name)
	{
		if (string.IsNullOrWhiteSpace(name)) return null;
		if (_payloadTypes.ContainsKey(name))
		{
			return _payloadTypes[name];
		}
		return null;
	}

	public List<Type> GetRegisteredPayloadTypes()
	{
		return [.. _payloadTypes.Values];
	}
	#endregion

	#region Active Process ...
	public AsyncOperationActiveProcess? GetActiveProcess(string operationId) {
		if (string.IsNullOrWhiteSpace(operationId)) return null;
		if (!_activeProcesses.ContainsKey(operationId)) return null;

		var activeProcess = _activeProcesses[operationId];
        return activeProcess;
    }

    public List<ActiveProcessDto> GetActiveProcesses()
    {
        var activeProcesses = new List<ActiveProcessDto>();
        foreach (var activeProcess in _activeProcesses.Values)
        {
			activeProcesses.Add(new ActiveProcessDto
            {
                CreatedAt = activeProcess.CreatedAt,
                RunningTimeMs = TimeSpan.FromTicks(DateTime.Now.Ticks - activeProcess.CreatedAt.Ticks).TotalMilliseconds,
                OwnerId = activeProcess.OwnerId,
                OperationId = activeProcess.OperationId,
                PayloadId = activeProcess.PayloadId,
                PayloadType = activeProcess.PayloadType,
                OperationName = activeProcess.OperationName,
				OperationDescription = activeProcess.OperationDescription,
				Status = activeProcess.Status,
				Progress = activeProcess.Progress,
				ProgressMessage = activeProcess.ProgressMessage
			});
        }

        return activeProcesses;
    }

    public void RemoveActiveProcess(string operationId, 
        CancellationToken cancellationToken = default)
    {
        if (_activeProcesses.ContainsKey(operationId))
        {
            _activeProcesses.Remove(operationId, out _);
        }
    }
	#endregion

	#region Operation ...
	public async Task<AsyncOperation?> GetOperation(string operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operationId)) return null;
        if (_storage == null || _storage.Operation == null) return null;

        return await _storage.Operation.GetAsync(operationId, cancellationToken);
    }

    #region Payload ...
    public async Task<AsyncOperationPayloadBase?> GetOperationPayload(string operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operationId)) return null;
        if (_storage == null || _storage.Operation == null || _storage.Payload == null) return null;

        return await _storage.Payload.GetByOperationIdAsync(operationId, cancellationToken);
    }

    public async Task<AsyncOperationPayloadBase?> GetOperationPayloadById(string payloadId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadId)) return null;
        if (_storage == null || _storage.Payload == null) return null;

        return await _storage.Payload.GetAsync(payloadId, cancellationToken);
    }
    #endregion

    #region Progres ...
    public async Task<AsyncOperationProgress?> GetOperationProgress(string operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operationId)) return null;
        if (_storage == null || _storage.Progress == null) return null;

        return await _storage.Progress.GetByOperationIdAsync(operationId, cancellationToken);
    }

    public async Task<AsyncOperationProgress?> GetOperationProgressById(string progressId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(progressId)) return null;
        if (_storage == null || _storage.Progress == null) return null;

        return await _storage.Progress.GetAsync(progressId, cancellationToken);
    }

    public async Task<IEnumerable<AsyncOperationProgress>> GetAllOperationProgress(string operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operationId)) return [];
        if (_storage == null || _storage.Progress == null) return [];

        return await _storage.Progress.GetAllByOprationId(operationId, cancellationToken);
    }
    #endregion

    #region Result ...
    public async Task<AsyncOperationResult?> GetOperationResult(string operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operationId)) return null;
        if (_storage == null || _storage.Result == null) return null;

        return await _storage.Result.GetAsync(operationId, cancellationToken);
    }

    public async Task<AsyncOperationResult?> GetOperationResultById(string resultId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resultId)) return null;
        if (_storage == null || _storage.Result == null) return null;

        return await _storage.Result.GetAsync(resultId, cancellationToken);
    }
	#endregion

	#endregion
}