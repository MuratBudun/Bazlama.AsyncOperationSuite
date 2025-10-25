using Bazlama.AsyncOperationSuite.Dto;
using Bazlama.AsyncOperationSuite.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using NJsonSchema;

namespace Bazlama.AsyncOperationSuite.Mvc.Controllers;

[ApiController]
[Route("payload", Name = "Async Operation Suite Payload")]
internal class AsyncOperationPayloadController : ControllerBase
{
	private readonly ILogger _logger;
	private readonly AsyncOperationService _asyncOperationService;

	public AsyncOperationPayloadController(
		AsyncOperationService asyncOperationService,
		ILogger<AsyncOperationQueryController> logger)
	{
		ArgumentNullException.ThrowIfNull(asyncOperationService);

		_logger = logger;
		_asyncOperationService = asyncOperationService;
	}

	[HttpGet]
	[EndpointDescription("Get the list of registered payload types and their corresponding operation types.")]
	[ProducesResponseType<RegisteredPayloadDto>(StatusCodes.Status200OK)]
	public ActionResult<RegisteredPayloadDto> GetRegisteredPayloads()
	{
		var result = _asyncOperationService.GetRegisteredPayloads();
		return Ok(result);
	}

	[HttpGet("type")]
	[EndpointDescription("Get the list of registered payload types with their JSON schemas.")]
	[ProducesResponseType<List<Type>>(StatusCodes.Status200OK)]
	public ActionResult<Dictionary<string, object>> GetRegisteredPayloadTypes()
	{
		var payloadTypes = _asyncOperationService.GetRegisteredPayloadTypes();
		var result = new Dictionary<string, object>();

		foreach (var type in payloadTypes)
		{
			var schema = JsonSchema.FromType(type);
			result[type.Name] = JsonDocument.Parse(schema.ToJson()).RootElement;
		}

		return Ok(result);
	}

	[HttpGet("type/{name}")]
	[EndpointDescription("Get the JSON schema of a registered payload type by its name.")]
	[ProducesResponseType<List<Type>>(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<JsonDocument> GetRegisteredPayloadType(String name)
	{
		var result = new Dictionary<string, object>();
		var payloadType = _asyncOperationService.GetPayloadTypeByName(name);
		if (payloadType == null) return NotFound(new { message = "Payload type not found" });

		var schema = JsonSchema.FromType(payloadType);
		result[payloadType.Name] = JsonDocument.Parse(schema.ToJson()).RootElement;
		return Ok(result);
	}
}
