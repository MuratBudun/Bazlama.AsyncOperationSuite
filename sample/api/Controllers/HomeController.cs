using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("/ttapi/[controller]")]
public class ValuesController : ControllerBase
{
	[HttpGet]
	[Authorize]
	public IActionResult GetValues()
	{
		return Ok(new string[] { "value1", "value2" });
	}
}