using Bazlama.AsyncOperationSuite.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Bazlama.AsyncOperationSuite.MemoryStorage;

public static class ServiceCollectionExtensions
{
    public static void AddAsyncOperationSuiteMemoryStorage(
        this IServiceCollection services)
    {
        services.AddSingleton<IAsyncOperationStorage, AsyncOperationMemoryStorage>();
    }
}