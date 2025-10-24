using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage.Configurations;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bazlama.AsyncOperationSuite.Storage.MemoryStorage;

public static class ServiceCollectionExtensions
{
    public static void AddAsyncOperationSuiteMemoryStorage(
		this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<MemoryStorageConfiguration>(config =>
			configuration.GetSection(MemoryStorageConfiguration.Name).Bind(config));
		services.AddSingleton<IAsyncOperationStorage, AsyncOperationMemoryStorage>();
    }
}