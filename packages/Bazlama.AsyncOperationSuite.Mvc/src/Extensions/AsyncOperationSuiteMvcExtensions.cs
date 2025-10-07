using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

using Bazlama.AsyncOperationSuite.Mvc.Controllers;

namespace Bazlama.AsyncOperationSuite.Mvc.Extensions;

public static class AsyncOperationSuiteMvcExtensions
{
	public static IMvcBuilder AddAsyncOperationSuiteMvcAllControllers(
		this IServiceCollection services,
		bool requireAuthorization = false,
		string? policyName = null,
		string prefix = "api/aos")
	{
		var types = new[]
		{
			typeof(AsyncOperationPayloadController),
			typeof(AsyncOperationPublishController),
			typeof(AsyncOperationQueryController)
		};
		return services.AddAsyncOperationSuiteControllers(types, requireAuthorization, policyName, prefix);
	}

	public static IMvcBuilder AddAsyncOperationSuiteMvcOperationPayload(
		this IServiceCollection services,
		bool requireAuthorization = false,
		string? policyName = null,
		string prefix = "api/aos")
	{
		var types = new[] { typeof(AsyncOperationPayloadController) };
		return services.AddAsyncOperationSuiteControllers(types, requireAuthorization, policyName, prefix);
	}

	public static IMvcBuilder AddAsyncOperationSuiteMvcOperationPublish(
		this IServiceCollection services,
		bool requireAuthorization = false,
		string? policyName = null,
		string prefix = "api/aos")
	{
		var types = new[] { typeof(AsyncOperationPublishController) };
		return services.AddAsyncOperationSuiteControllers(types, requireAuthorization, policyName, prefix);
	}

	public static IMvcBuilder AddAsyncOperationSuiteMvcOperationQuery(
		this IServiceCollection services, 
		bool requireAuthorization = false,
		string? policyName = null,
		string prefix = "api/aos")
	{
		var types = new[] { typeof(AsyncOperationQueryController) };
		return services.AddAsyncOperationSuiteControllers(types, requireAuthorization, policyName, prefix);
	}

	private static IMvcBuilder AddAsyncOperationSuiteControllers(
		this IServiceCollection services, 
		Type[] controllerTypes, 
		bool requireAuthorization,
		string? policyName,
		string prefix)
	{
		var builder = services.AddControllers();

		builder.ConfigureApplicationPartManager(pm =>
		{
			var asm = typeof(AsyncOperationSuiteMvcExtensions).Assembly;
			if (!pm.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == asm))
			{
				pm.ApplicationParts.Add(new AssemblyPart(asm));
			}

			pm.FeatureProviders.Add(new ExplicitControllerFeatureProvider(controllerTypes));
		});

		if (requireAuthorization)
		{
			builder.AddMvcOptions(options =>
			{
				var policy = new AuthorizationPolicyBuilder()
					.RequireAuthenticatedUser();

				if (!string.IsNullOrEmpty(policyName)) {
					policy.RequireRole(policyName);
				}

				options.Filters.Add(new AuthorizeFilter(policy.Build()));
			});
		}

		// Controller prefix mapping
		foreach (var ctrl in controllerTypes)
		{
			AsyncOperationSuiteRegistry.PrefixMappings.Add(new ControllerPrefixRegistration(ctrl, prefix));
		}

		// Singleton convention instance
		builder.AddMvcOptions(options =>
		{
			if (!options.Conventions.OfType<MultiPrefixConvention>().Any())
				options.Conventions.Insert(0, new MultiPrefixConvention());
		});

		return builder;
	}
}

internal class ExplicitControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
	private readonly Type[] _controllerTypes;
	public ExplicitControllerFeatureProvider(Type[] controllerTypes) => _controllerTypes = controllerTypes;

	public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
	{
		foreach (var type in _controllerTypes)
		{
			var ti = type.GetTypeInfo();
			if (!feature.Controllers.Contains(ti))
			{
				feature.Controllers.Add(ti);
			}
		}
	}
}


internal record ControllerPrefixRegistration(Type ControllerType, string Prefix);

internal static class AsyncOperationSuiteRegistry
{
	public static readonly List<ControllerPrefixRegistration> PrefixMappings = new();
}

public class MultiPrefixConvention : IApplicationModelConvention
{
	public void Apply(ApplicationModel application)
	{
		foreach (var controller in application.Controllers)
		{
			var mapping = AsyncOperationSuiteRegistry.PrefixMappings
				.FirstOrDefault(m => m.ControllerType == controller.ControllerType.AsType());

			if (mapping == null)
				continue; // No prefix mapping for this controller

			var prefix = mapping.Prefix.Trim('/');

			foreach (var selector in controller.Selectors)
			{
				if (selector.AttributeRouteModel != null)
				{
					selector.AttributeRouteModel.Template =
						$"{prefix}/{selector.AttributeRouteModel.Template?.TrimStart('/')}".Trim('/');
				}
				else
				{
					selector.AttributeRouteModel = new AttributeRouteModel
					{
						Template = prefix
					};
				}
			}
		}
	}
}