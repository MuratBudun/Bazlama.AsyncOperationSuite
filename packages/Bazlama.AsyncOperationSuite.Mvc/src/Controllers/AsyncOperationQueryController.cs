using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Dto;
using Bazlama.AsyncOperationSuite.Services;
using Bazlama.AsyncOperationSuite.Exceptions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bazlama.AsyncOperationSuite.Mvc.Controllers;

[Controller]
[Route("query", Name = "Async Operation Suite Query")]
internal class AsyncOperationQueryController: ControllerBase
{
    private readonly ILogger _logger;
    private readonly AsyncOperationService _asyncOperationService;

    public AsyncOperationQueryController(
        AsyncOperationService asyncOperationService,
        ILogger<AsyncOperationQueryController> logger)
    {
        ArgumentNullException.ThrowIfNull(asyncOperationService);

        _logger = logger;
        _asyncOperationService = asyncOperationService;
    }

	[HttpGet("engine")]
	[ProducesResponseType<EngineInfoDto>(StatusCodes.Status200OK)]
	public ActionResult<EngineInfoDto> GetEngineInfo()
	{
		var result = _asyncOperationService.GetEngineInfo();
		return Ok(result);
	}

	[HttpGet("active")]
    [ProducesResponseType<ActiveProcessDto>(StatusCodes.Status200OK)]
    public ActionResult<ActiveProcessDto> GetActiveProcesses()
    {
        var result = _asyncOperationService.GetActiveProcesses();
        return Ok(result);
    }

    [HttpGet("operations")]
	public async Task<ActionResult<List<AsyncOperation>>> GetOperations(
        [FromQuery] DateTime? startDate,
		[FromQuery] DateTime? endDate,
		[FromQuery] List<AsyncOperationStatus>? status,
		[FromQuery] string? ownerId,
		[FromQuery] string? search,
		[FromQuery] bool isDesc = true,
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 10,
		CancellationToken cancellationToken = default)
	{
		var defaultStartDate = DateTime.UtcNow.AddDays(-7);
		var defaultEndDate = DateTime.UtcNow.AddDays(1);
		var defaultStatus = new List<AsyncOperationStatus>
		{
			AsyncOperationStatus.Pending,
			AsyncOperationStatus.Running,
			AsyncOperationStatus.Completed,
			AsyncOperationStatus.Failed,
			AsyncOperationStatus.Canceled
		};

		var result = await _asyncOperationService.GetOperations(
			startDate?? defaultStartDate, 
			endDate?? defaultEndDate, 
			status?? defaultStatus, ownerId, search,
			isDesc, pageNumber, pageSize,
			cancellationToken);
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

	[HttpGet("payload/{payloadId}/progress")]
	[ProducesResponseType<AsyncOperationProgress>(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<AsyncOperationProgress>> GetPayloadProgress(string payloadId,
		CancellationToken cancellationToken)
	{
		var payload = await _asyncOperationService.GetOperationPayloadById(payloadId, cancellationToken);
		if (payload == null) return NotFound();

		var result = await _asyncOperationService.GetOperationProgress(payload.OperationId, cancellationToken);
		if (result == null) return NotFound();

		return Ok(result);
	}
}
