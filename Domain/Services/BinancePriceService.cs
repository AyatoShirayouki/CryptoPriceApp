using Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository.Base.Interfaces;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace Domain.Services
{
	public class BinancePriceService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<BinancePriceService> _logger;

		public BinancePriceService(IServiceProvider serviceProvider, ILogger<BinancePriceService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var url = new Uri("wss://stream.binance.com:9443/ws/btcusdt@ticker/adausdt@ticker/ethusdt@ticker");

			using (var client = new WebsocketClient(url))
			{
				client.MessageReceived.Subscribe(msg => HandleMessage(msg.Text));

				await client.Start();

				while (!stoppingToken.IsCancellationRequested)
				{
					await Task.Delay(1000);
				}
			}
		}

		private void HandleMessage(string message)
		{
			BinancePriceUpdate data = null;
			try
			{
				data = JsonSerializer.Deserialize<BinancePriceUpdate>(message);
			}
			catch (JsonException ex)
			{
				_logger.LogError(ex, "Error deserializing WebSocket message: {Message}", message);
				return;
			}

			if (data == null || string.IsNullOrEmpty(data.Symbol))
			{
				_logger.LogWarning("Received invalid data: {Message}", message);
				return;
			}

			using (var scope = _serviceProvider.CreateScope())
			{
				var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				var priceRepository = unitOfWork.Repository<PriceModel>();

				var price = new PriceModel
				{
					Symbol = data.Symbol.ToUpper(),
					Price = data.LastPrice,
					Timestamp = DateTime.UtcNow
				};

				priceRepository.AddAsync(price).Wait();
				unitOfWork.SaveChangesAsync().Wait();
			}
		}
	}
}
