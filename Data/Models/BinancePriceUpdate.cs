using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Utils;

namespace Data.Models
{
	public class BinancePriceUpdate
	{
		[JsonPropertyName("s")]
		public string? Symbol { get; set; }

		[JsonPropertyName("c")]
		[JsonConverter(typeof(StringToDecimalConverter))]
		public decimal LastPrice { get; set; }
	}
}
