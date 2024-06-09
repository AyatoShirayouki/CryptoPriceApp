using Domain.Interfaces;
using Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CryptoPriceAppAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Produces("application/json", "application/xml")]
	public class PriceController : ControllerBase
	{
		private readonly IPriceService _priceService;

		public PriceController(IPriceService priceService)
		{
			_priceService = priceService;
		}

		[HttpGet("{symbol}/24hAvgPrice")]
		public async Task<IActionResult> Get24hAvgPrice(string symbol)
		{
			try
			{
				var result = await _priceService.Get24hAvgPriceAsync(symbol);
				return Ok(result);
			}
			catch (ExceptionHandler ex)
			{
				return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = "An internal error occurred.", details = ex.Message });
			}
		}

		[HttpGet("{symbol}/SimpleMovingAverage")]
		public async Task<IActionResult> GetSimpleMovingAverage(string symbol, [FromQuery] int n, [FromQuery] string p, [FromQuery] DateTime? s)
		{
			try
			{
				var result = await _priceService.GetSimpleMovingAverageAsync(symbol, n, p, s);
				return Ok(result);
			}
			catch (ExceptionHandler ex)
			{
				return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = "An internal error occurred.", details = ex.Message });
			}
		}
	}
}
