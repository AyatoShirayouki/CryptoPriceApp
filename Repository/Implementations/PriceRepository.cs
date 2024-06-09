using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Base;
using Repository.Implementations.Interfaces;
using Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Repository.Implementations
{
	public class PriceRepository : Repository<PriceModel>, IPriceRepository
	{
		public PriceRepository(CryptoPriceDbContext context) : base(context) { }

		public async Task<decimal> Get24hAvgPriceAsync(string symbol)
		{
			try
			{
				var now = DateTime.UtcNow;
				var start = now.AddDays(-1);

				var prices = await _entities
					.AsNoTracking()
					.Where(p => p.Symbol == symbol && p.Timestamp >= start && p.Timestamp <= now)
					.Select(p => p.Price)
					.ToListAsync();

				if (!prices.Any())
					throw new ExceptionHandler("No price data available.", "PRICE_DATA_NOT_FOUND");

				return prices.Average();
			}
			catch (ExceptionHandler ex) when (ex.ErrorCode == "PRICE_DATA_NOT_FOUND")
			{
				throw;
			}
			catch (DbUpdateException dbEx)
			{
				throw new ExceptionHandler("Database update error occurred.", "DB_UPDATE_ERROR", dbEx);
			}
			catch (Exception ex)
			{
				throw new ExceptionHandler("An error occurred while retrieving 24h average price.", "REPOSITORY_ERROR", ex);
			}
		}

		public async Task<decimal> GetSimpleMovingAverageAsync(string symbol, int n, TimeSpan timePeriod, DateTime? startDate)
		{
			try
			{
				var start = startDate ?? DateTime.UtcNow;

				var dataPoints = await _entities
					.AsNoTracking()
					.Where(p => p.Symbol == symbol && p.Timestamp >= start.Subtract(timePeriod.Multiply(n)))
					.OrderBy(p => p.Timestamp)
					.Select(p => p.Price)
					.ToListAsync();

				if (dataPoints.Count < n)
					throw new ExceptionHandler("Not enough data points.", "INSUFFICIENT_DATA_POINTS");

				return dataPoints.TakeLast(n).Average();
			}
			catch (ExceptionHandler ex) when (ex.ErrorCode == "INSUFFICIENT_DATA_POINTS")
			{
				throw;
			}
			catch (DbUpdateException dbEx)
			{
				throw new ExceptionHandler("Database update error occurred.", "DB_UPDATE_ERROR", dbEx);
			}
			catch (Exception ex)
			{
				throw new ExceptionHandler("An error occurred while calculating SMA.", "REPOSITORY_ERROR", ex);
			}
		}
	}
}
