using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
	public static class DatabaseInitializer
	{
		public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
		{
			var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("DatabaseInitializer");
			try
			{
				using (var scope = serviceProvider.CreateScope())
				{
					var context = scope.ServiceProvider.GetRequiredService<CryptoPriceDbContext>();
					logger.LogInformation("Ensuring database exists and applying migrations...");

					if (!await context.Database.CanConnectAsync())
					{
						logger.LogInformation("Database does not exist. Creating database...");
						await context.Database.EnsureCreatedAsync();
					}
					else
					{
						logger.LogInformation("Database exists. Applying migrations...");
						await context.Database.MigrateAsync();
					}

					logger.LogInformation("Database check and migrations completed.");
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while initializing the database.");
				throw;
			}
		}
	}
}
