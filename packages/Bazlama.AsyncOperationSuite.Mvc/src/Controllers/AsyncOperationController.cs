using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Dto;
using Bazlama.AsyncOperationSuite.Services;
using Bazlama.AsyncOperationSuite.Exceptions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bazlama.Mvc.AsyncOperationSuite.Controllers;

[Controller]
[Route("api/aos", Name = "Async Operation Suite")]
public class AsyncOperationController: ControllerBase
{
    private readonly ILogger _logger;
    private readonly AsyncOperationService _asyncOperationService;

    public AsyncOperationController(
        AsyncOperationService asyncOperationService,
        ILogger<AsyncOperationController> logger)
    {
        ArgumentNullException.ThrowIfNull(asyncOperationService);

        _logger = logger;
        _asyncOperationService = asyncOperationService;
    }

    [HttpGet("registered-types")]
    [ProducesResponseType<RegisteredTypeDto>(StatusCodes.Status200OK)]
    public ActionResult<RegisteredTypeDto> GetRegisteredTypes()
    {
        var result = _asyncOperationService.GetRegisteredTypeDtos();
        return Ok(result);
    }

    [HttpGet("active-processes")]
    [ProducesResponseType<ActiveProcessDto>(StatusCodes.Status200OK)]
    public ActionResult<ActiveProcessDto> GetActiveProcesses()
    {
        var result = _asyncOperationService.GetActiveProcesses();
        return Ok(result);
    }

    [HttpGet("operation/{operationId}")]
    [ProducesResponseType<AsyncOperation>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsyncOperation>> GetOperation(string operationId,
        CancellationToken cancellationToken)
    {
        var result = await _asyncOperationService.GetOperation(operationId, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [HttpGet("operation/{operationId}/payload")]
    [ProducesResponseType<AsyncOperationPayloadBase>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsyncOperationPayloadBase>> GetOperationPayload(string operationId,
        CancellationToken cancellationToken)
    {
        var result = await _asyncOperationService.GetOperationPayload(operationId, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [HttpGet("payload/{payloadId}")]
    [ProducesResponseType<AsyncOperationPayloadBase>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsyncOperationPayloadBase>> GetPayload(string payloadId,
        CancellationToken cancellationToken)
    {
        var result = await _asyncOperationService.GetOperationPayloadById(payloadId, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [HttpGet("operation/{operationId}/progress")]
    [ProducesResponseType<AsyncOperationProgress>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsyncOperationProgress>> GetOperationProgress(string operationId,
        CancellationToken cancellationToken)
    {
        var result = await _asyncOperationService.GetOperationProgress(operationId, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [HttpPost("publish")]
    [ProducesResponseType<AsyncOperationPayloadBase>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AsyncOperationPayloadBase>> PublishPayload(
        [FromBody] JsonDocument payloadJson,
        [FromQuery] string payloadType,
        [FromQuery] bool waitForQueueSpace = false,
        [FromQuery] bool waitForPayloadSlotSpace = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var types = _asyncOperationService.GetTypesFromPayload(payloadType);
           
            if (types.OperationType == null || types.PayloadType == null) 
                return BadRequest(new { message = "Invalid payload type" });

            try
            {
                AsyncOperationPayloadBase? payload = JsonSerializer.Deserialize(
                    payloadJson.RootElement.GetRawText(), types.PayloadType) as AsyncOperationPayloadBase;

                if (payload == null)
                    return BadRequest(new { message = "Invalid payload json dcoument" });

                var result = await _asyncOperationService.PublishPayloadAsync(
                    payload: payload,
                    waitForQueueSpace: waitForQueueSpace,
                    waitForPayloadSlotSpace: waitForPayloadSlotSpace,
                    cancellationToken: cancellationToken);

                return Ok(result);

            }
            catch (Exception ex) {

                return BadRequest(new { message = $"Invalid payload json dcoument. {ex.Message}" });
            }
        }

        catch (QueueFullException ex)
        {
            return StatusCode(429, new { message = ex.Message });
        }
        catch (PayloadTypeLimitExceededException ex)
        {
            return StatusCode(429, new { message = ex.Message });
        }
        catch (Exception ex)
        {
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
            _asyncOperationService.CancelProcess(
                operationId: operationId,
                useThrowIfCancellationRequested: useThrowIfCancellationRequested,
                waitForCompletion: waitForCompletion,
                timeoutMs: timeoutMs);
            return Ok();
        }
        catch(AsyncOperationNotFoundException) {
            return NotFound();
        }
        catch (TimeoutException) {
            return StatusCode(408); 
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
