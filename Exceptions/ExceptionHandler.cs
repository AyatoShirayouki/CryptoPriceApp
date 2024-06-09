using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exceptions
{
	public class ExceptionHandler : Exception
	{
		public string ErrorCode { get; }

		public ExceptionHandler(string message, string errorCode, Exception innerException = null)
			: base(message, innerException)
		{
			ErrorCode = errorCode;
		}
	}
}
