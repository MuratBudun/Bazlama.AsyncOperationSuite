using Bazlama.AsyncOperationSuite.Interfaces;
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bazlama.AsyncOperationSuite.Storage.MSSQLStorage;

public static class ServiceCollectionExtensions
{
    public static void AddAsyncOperationSuiteMSSQLStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MSSQLStorageConfiguration>(config =>
			configuration.GetSection(MSSQLStorageConfiguration.Name).Bind(config));
		services.AddSingleton<IAsyncOperationStorage, AsyncOperationMSSQLStorage>();
    }
}