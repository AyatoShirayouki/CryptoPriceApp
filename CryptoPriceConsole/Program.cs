using Data;
using Data.Context;
using Domain.Interfaces;
using Domain.Services;
using Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Repository.Base;
using Repository.Base.Interfaces;
using Repository.Implementations;
using Repository.Implementations.Interfaces;
using System;
using System.Threading.Tasks;

class Program
{
	static async Task Main(string[] args)
	{
		var host = CreateHostBuilder(args).Build();

		await DatabaseInitializer.InitializeDatabaseAsync(host.Services);

		var runHostTask = host.RunAsync();

		using var scope = host.Services.CreateScope();
		var priceService = scope.ServiceProvider.GetRequiredService<IPriceService>();

		Console.WriteLine("Welcome to the Crypto Price Console!");
		Console.WriteLine("Available commands:");
		Console.WriteLine("  24h <symbol> - Get 24h average price for a symbol");
		Console.WriteLine("  sma <symbol> <n> <p> [<s>] - Get Simple Moving Average for a symbol");

		while (true)
		{
			Console.Write("\nEnter command: ");
			var input = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(input))
			{
				continue;
			}

			var inputArgs = input.Split(' ');
			if (inputArgs.Length < 2)
			{
				Console.WriteLine("Usage: <command> <arguments>");
				continue;
			}

			var command = inputArgs[0];
			var symbol = inputArgs[1];

			try
			{
				switch (command)
				{
					case "24h":
						var avgPrice = await priceService.Get24hAvgPriceAsync(symbol);
						Console.WriteLine($"24h Average Price for {symbol}: {avgPrice}");
						break;
					case "sma":
						if (inputArgs.Length < 4)
						{
							Console.WriteLine("Usage: sma <symbol> <n> <p> [<s>]");
							break;
						}
						int n = int.Parse(inputArgs[2]);
						string p = inputArgs[3];
						DateTime? s = inputArgs.Length == 5 ? DateTime.Parse(inputArgs[4]) : (DateTime?)null;
						var sma = await priceService.GetSimpleMovingAverageAsync(symbol, n, p, s);
						Console.WriteLine($"SMA for {symbol}: {sma}");
						break;
					default:
						Console.WriteLine("Unknown command");
						break;
				}
			}
			catch (ExceptionHandler ex)
			{
				Console.WriteLine($"Custom error: {ex.Message} (Code: {ex.ErrorCode})");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred: {ex.Message}");
			}
		}

		await runHostTask;
	}

	static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((context, config) =>
			{
				config.SetBasePath(AppContext.BaseDirectory);
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
				config.AddEnvironmentVariables();
			})
			.ConfigureServices((context, services) =>
			{
				var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

				services.AddScoped<IUnitOfWork, UnitOfWork>();
				services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
				services.AddScoped<IPriceRepository, PriceRepository>();
				services.AddScoped<IPriceService, PriceService>();
				services.AddHostedService<BinancePriceService>();

				services.AddDbContext<CryptoPriceDbContext>(options =>
					options.UseSqlServer(connectionString));

				services.AddMemoryCache();
			});
}
