using Bazlama.AsyncOperationSuite.Exceptions;
using Bazlama.AsyncOperationSuite.Services;
using Example.Web.Api.AsyncOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("/welcome")]
public class HomeController : ControllerBase
{
	[HttpGet]
	public IActionResult Welcome()
	{
		return Ok(new { message = "Welcome to the Async Operation Suite example API!" });
	}

	[HttpPost("test-delay-operations")]
	[AllowAnonymous]
	public async Task<IActionResult> PublishDelayOperations(
		AsyncOperationService asyncOperationService)
	{
		try
		{
			for (int i = 0; i < 5; i++)
			{
				var payload = new DelayOperationPayload
				{
					Name = $"Test Delay Operation #{i + 1}",
					Description = $"This is test delay operation number {i + 1}",
					DelaySeconds = 1,
					StepCount = 10
				};

				await asyncOperationService.PublishPayloadAsync(
					payload: payload,
					waitForQueueSpace: false,
					waitForPayloadSlotSpace: false,
					cancellationToken: default);
			}

			return Ok();
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
}