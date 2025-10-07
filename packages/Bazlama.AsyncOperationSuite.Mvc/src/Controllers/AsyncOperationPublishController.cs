using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Services;
using Bazlama.AsyncOperationSuite.Exceptions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bazlama.AsyncOperationSuite.Mvc.Controllers;

[ApiController]
[Route("publish", Name = "Async Operation Suite Publish")]
internal class AsyncOperationPublishController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly AsyncOperationService _asyncOperationService;

    public AsyncOperationPublishController(
        AsyncOperationService asyncOperationService,
        ILogger<AsyncOperationQueryController> logger)
    {
        ArgumentNullException.ThrowIfNull(asyncOperationService);

        _logger = logger;
        _asyncOperationService = asyncOperationService;
    }

	[HttpPost]
	[ProducesResponseType<AsyncOperationPayloadBase>(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<AsyncOperationPayloadBase>> PublishPayload(
		 [FromBody] JsonDocument payloadJson,
		 [FromQuery] string payloadType,
		 [FromQuery] string operationName = "",
		 [FromQuery] string operationDescription = "",
		 [FromQuery] bool waitForQueueSpace = false,
		 [FromQuery] bool waitForPayloadSlotSpace = false,
		 CancellationToken cancellationToken = default)
	{
		try
		{
			var types = _asyncOperationService.GetTypesFromPayload(payloadType);

			if (types.OperationType == null || types.PayloadType == null)
				return BadRequest(new { message = "Invalid payload type" });

			if (JsonSerializer.Deserialize(
				payloadJson.RootElement.GetRawText(), types.PayloadType) is not AsyncOperationPayloadBase payload)
				return BadRequest(new { message = "Invalid payload json dcoument" });

			if (operationName != null && operationName.Length > 0 && string.IsNullOrWhiteSpace(payload.Name))
				payload.Name = operationName;

			if (operationDescription != null && operationDescription.Length > 0 && string.IsNullOrWhiteSpace(payload.Description))
				payload.Description = operationDescription;

			var result = await _asyncOperationService.PublishPayloadAsync(
				payload: payload,
				waitForQueueSpace: waitForQueueSpace,
				waitForPayloadSlotSpace: waitForPayloadSlotSpace,
				cancellationToken: cancellationToken);

			return Ok(result);
		}
		catch (QueueFullException ex)
		{
			_logger.LogWarning(ex, "Queue is full");
			return StatusCode(429, new { message = ex.Message });
		}
		catch (PayloadTypeLimitExceededException ex)
		{
			_logger.LogWarning(ex, "Payload type limit exceeded");
			return StatusCode(429, new { message = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error publishing payload");
			return StatusCode(500, new { message = ex.Message });
		}
	}

	[HttpPost("cancel/{operationId}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status408RequestTimeout)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public IActionResult CancelProcess(string operationId,
		[FromQuery] bool useThrowIfCancellationRequested = false,
		[FromQuery] bool waitForCompletion = false,
		[FromQuery] int timeoutMs = 30000)
	{
		try
		{
			_logger.LogInformation("Cancelling operation {OperationId}", operationId);
			_asyncOperationService.CancelProcess(
				operationId: operationId,
				useThrowIfCancellationRequested: useThrowIfCancellationRequested,
				waitForCompletion: waitForCompletion,
				timeoutMs: timeoutMs);
			return Ok();
		}
		catch (AsyncOperationNotFoundException)
		{
			_logger.LogWarning("Operation {OperationId} not found", operationId);
			return NotFound();
		}
		catch (TimeoutException)
		{
			_logger.LogWarning("Timeout while cancelling operation {OperationId}", operationId);
			return StatusCode(408);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error cancelling operation {OperationId}", operationId);
			return StatusCode(500, new { message = ex.Message });
		}
	}
}
