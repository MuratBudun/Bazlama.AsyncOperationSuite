using Bazlama.AsyncOperationSuite.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bazlama.AsyncOperationSuite.Services;

public partial class AsyncOperationService : BackgroundService
{
    public List<(Type? PayloadType, Type? ProcessType)> GetRegisteredTypes()
    {
        var registeredTypes = new List<(Type? PayloadType, Type? ProcessType)>();

        foreach (var payloadType in _payloadTypes.Values)
        {
            if (!_processTypes.TryGetValue(payloadType, out var processType))
            {
                _logger.LogWarning("Operation type not found for payload type {PayloadType}", payloadType);
                processType = null;
            }
            registeredTypes.Add((payloadType, processType));
        }

        return registeredTypes;
    }

    public (Type? PayloadType, Type? OperationType) GetTypesFromPayload(string payloadType)
    {
        if (string.IsNullOrWhiteSpace(payloadType))
        {
            _logger.LogWarning("Payload type is null or empty");
            return (null, null);
        }

        if (!_payloadTypes.TryGetValue(payloadType, out var payloadTypeClass))
        {
            _logger.LogWarning("Payload type {PayloadType} not found", payloadType);
            return (null, null);
        }

        var operationType = _processTypes[payloadTypeClass].BaseType?.GetGenericArguments().FirstOrDefault();
        if (operationType == null)
        {
            _logger.LogWarning("Process type not found for payload type {PayloadType}", payloadType);
            return (null, null);
        }

        return (payloadTypeClass, operationType);
    }

    public bool IsExistOperationProcessForPayload(string payloadTypeName)
    {
        if (string.IsNullOrWhiteSpace(payloadTypeName)) return false;

        if (!_payloadTypes.TryGetValue(payloadTypeName, out var payloadType)) return false;
        return _processTypes.TryGetValue(payloadType, out var _);
    }

    public bool IsExistOperationProcessForPayload(AsyncOperationPayloadBase payload)
    {
        if (payload == null) return false;

        var payloadTypeName = payload.GetType().Name;
        if (payloadTypeName == null) return false;

        if (!_payloadTypes.TryGetValue(payloadTypeName, out var payloadType)) return false;
        return _processTypes.TryGetValue(payloadType, out var _);
    }
}