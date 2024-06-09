using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
	public interface IPriceService
	{
		Task<decimal> Get24hAvgPriceAsync(string symbol);
		Task<decimal> GetSimpleMovingAverageAsync(string symbol, int n, string p, DateTime? s);
	}
}
