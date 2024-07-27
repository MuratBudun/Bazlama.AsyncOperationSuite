using Bazlama.AsyncOperationSuite.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Bazlama.AsyncOperationSuite.Services;

public partial class AsyncOperationService : BackgroundService
{
    private void RegisterPayloadTypes()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
            RegisterFromAsembly(assembly);

        CheckPayloadProcessIntegrity();
    }

    private void RegisterFromAsembly(Assembly assembly)
    {
        var payloadTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AsyncOperationPayloadBase)))
            .ToList();

        var processTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && IsSubclassOfGeneric(t, typeof(AsyncOperationProcess<>)))
            .ToList();

        foreach (var payloadType in payloadTypes)
        {
            var name = payloadType.Name;
            if (name == null) continue;

            if (_payloadTypes.ContainsKey(name))
            {
                _logger.LogWarning("Payload type {PayloadTypeName} already registered", name);
                continue;
            }

            _payloadTypes[name] = payloadType;
            _logger.LogInformation("Payload type {PayloadTypeName} registered", name);
        }

        foreach (var processType in processTypes)
        {
            var genericArgument = processType.BaseType?.GetGenericArguments().FirstOrDefault();
            if (genericArgument == null) continue;

            var name = genericArgument.Name;
            if (name == null) continue;

            if (_processTypes.ContainsKey(genericArgument))
            {
                _logger.LogWarning("Process type {ProcessTypeName} already registered", name);
                continue;
            }

            _processTypes[genericArgument] = processType;
            _logger.LogInformation("Process type {ProcessTypeName} registered", name);
        }
    }

    private bool IsSubclassOfGeneric(Type type, Type genericType)
    {
        var currType = type;
        while (currType != null && currType != typeof(object))
        {
            var cur = currType.IsGenericType ? currType.GetGenericTypeDefinition() : type;
            if (genericType == cur)
            {
                return true;
            }
            currType = currType.BaseType;
        }
        return false;
    }

    private void CheckPayloadProcessIntegrity()
    {
        Dictionary<string, Type> newPayloadTypes = [];
        Dictionary<Type, Type> newProcessTypes = [];

        foreach (var payloadType in _payloadTypes)
        {
            if (!_processTypes.ContainsKey(payloadType.Value))
            {
                _logger.LogWarning("Process type not found for payload type {PayloadTypeName}", payloadType.Key);
                continue;
            }
            newPayloadTypes[payloadType.Key] = payloadType.Value;
        }

        foreach (var processType in _processTypes)
        {
            if (!_payloadTypes.ContainsValue(processType.Key))
            {
                _logger.LogWarning("Payload type not found for process type {ProcessTypeName}", processType.Key);
                continue;
            }
            newProcessTypes[processType.Key] = processType.Value;
        }

        _payloadTypes.Clear();
        _processTypes.Clear();

        foreach (var payloadType in newPayloadTypes)
            _payloadTypes[payloadType.Key] = payloadType.Value;

        foreach (var processType in newProcessTypes)
            _processTypes[processType.Key] = processType.Value;
    }
}