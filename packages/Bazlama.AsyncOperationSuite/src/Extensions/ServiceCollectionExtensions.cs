using Bazlama.AsyncOperationSuite.Configurations;
using Bazlama.AsyncOperationSuite.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bazlama.AsyncOperationSuite.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAsyncOperationSuiteService(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AsyncOperationSuiteConfiguration>(config =>
            configuration.GetSection(AsyncOperationSuiteConfiguration.Name).Bind(config));

        services.AddSingleton<AsyncOperationService>();
        services.AddHostedService((sp) => sp.GetRequiredService<AsyncOperationService>());
    }
}
