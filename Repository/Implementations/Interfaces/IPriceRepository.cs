using Data.Models;
using Repository.Base.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Implementations.Interfaces
{
	public interface IPriceRepository : IRepository<PriceModel>
	{
		Task<decimal> Get24hAvgPriceAsync(string symbol);
		Task<decimal> GetSimpleMovingAverageAsync(string symbol, int n, TimeSpan timePeriod, DateTime? startDate);
	}
}
