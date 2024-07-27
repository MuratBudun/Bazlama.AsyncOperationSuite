using Bazlama.Mvc.AsyncOperationSuite.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Bazlama.Mvc.AsyncOperationSuite.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAsyncOperationSuiteMvc(this IServiceCollection services, 
        bool requireAuthorization = false)
    {
        var mvcBuilder = services.AddControllers().AddApplicationPart(typeof(AsyncOperationController).Assembly);

        if (requireAuthorization)
        {
            mvcBuilder.AddMvcOptions(options =>
            {
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
            });
        }

        return services;
    }

}