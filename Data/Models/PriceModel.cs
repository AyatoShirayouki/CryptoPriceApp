using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
	public class PriceModel : BaseEntity
	{
		public string? Symbol { get; set; }
		public decimal Price { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
