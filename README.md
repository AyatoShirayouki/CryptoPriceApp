# CryptoPriceApp

CryptoPriceApp is a robust and extensible solution designed to provide real-time and historical cryptocurrency price data. It comprises a web API (`CryptoPriceAppAPI`) for serving price data and a console application (`CryptoPriceConsole`) for command-line interactions. The application also includes several class libraries for managing data, business logic, and utility functions.

## Table of Contents

- [Project Structure](#project-structure)
  - [CryptoPriceAppAPI](#cryptopriceappapi)
  - [CryptoPriceConsole](#cryptopriceconsole)
  - [Data](#data)
  - [Domain](#domain)
  - [Exceptions](#exceptions)
  - [Repository](#repository)
  - [Utils](#utils)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Running the API](#running-the-api)
  - [Using the Console App](#using-the-console-app)
- [Technical Details](#technical-details)
  - [Database Initialization](#database-initialization)
  - [Error Handling](#error-handling)
  - [Real-time Price Updates](#real-time-price-updates)
- [License](#license)

## Project Structure

### 1. CryptoPriceAppAPI

The `CryptoPriceAppAPI` project provides a web-based interface for accessing cryptocurrency price data. This project is built with ASP.NET Core and offers several endpoints to fetch price information.

#### **Controllers**

- **PriceController.cs**: The core controller handling API requests for cryptocurrency prices. It provides two main endpoints:
  - `GET /api/Price/{symbol}/24hAvgPrice`: Retrieves the 24-hour average price for the specified cryptocurrency.
  - `GET /api/Price/{symbol}/SimpleMovingAverage`: Calculates the Simple Moving Average (SMA) for the specified cryptocurrency over a given period.

  ```csharp
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
  ```

#### **Configuration**

- **appsettings.json**: Configures logging and database connection settings. Make sure to adjust the `ConnectionStrings` section to your environment.

  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "ConnectionStrings": {
      "DefaultConnection": "Data Source=YOUR_SERVER;TrustServerCertificate=True;Initial Catalog=BinancePriceAppDB;User ID=YOUR_USER;Password=YOUR_PASSWORD"
    },
    "AllowedHosts": "*"
  }
  ```

- **Program.cs**: The entry point of the application that sets up dependency injection, configures services, and runs the web host.

  ```csharp
  var builder = WebApplication.CreateBuilder(args);
  builder.Services.AddControllers().AddXmlSerializerFormatters();
  builder.Services.AddDbContext<CryptoPriceDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions => sqlOptions.CommandTimeout(300)));
  builder.Services.AddMemoryCache();
  builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
  builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
  builder.Services.AddScoped<IPriceRepository, PriceRepository>();
  builder.Services.AddScoped<IPriceService, PriceService>();
  builder.Services.AddHostedService<BinancePriceService>();
  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddSwaggerGen();
  var app = builder.Build();
  await DatabaseInitializer.InitializeDatabaseAsync(app.Services);
  if (app.Environment.IsDevelopment())
  {
      app.UseSwagger();
      app.UseSwaggerUI();
  }
  app.UseHttpsRedirection();
  app.UseAuthorization();
  app.MapControllers();
  app.Run();
  ```

### 2. CryptoPriceConsole

The `CryptoPriceConsole` is a command-line application for interacting with the cryptocurrency data. It allows users to request price data and calculate metrics directly from the terminal.

#### **Program.cs**

The main entry point for the console application. It sets up the host, initializes the database, and handles user commands.

```csharp
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
                services.AddDbContext<CryptoPriceDbContext>(options => options.UseSqlServer(connectionString));
                services.AddMemoryCache();
            });
}
```

### 3. Data

The `Data` project contains the database context and entity models. It is responsible for mapping data to the database and initializing it.

#### **Context**

- **CryptoPriceDbContext.cs**: The primary database context for the application. It manages `PriceModel` entities and configures indexes for improved query performance.

  ```csharp
  public class CryptoPriceDbContext : DbContext
  {
      public DbSet<PriceModel> Prices { get; set; }

      public CryptoPriceDbContext(DbContextOptions<Crypto

PriceDbContext> options) : base(options)
      {
          Prices = this.Set<PriceModel>();
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          base.OnModelCreating(modelBuilder);

          modelBuilder.Entity<PriceModel>(entity =>
          {
              entity.Property(p => p.Price).HasPrecision(18, 8);
              entity.HasIndex(p => p.Symbol);
              entity.HasIndex(p => p.Timestamp);
              entity.HasIndex(p => new { p.Symbol, p.Timestamp });
          });
      }
  }
  ```

#### **Models**

- **BaseEntity.cs**: Provides a base entity with a unique identifier for other models to inherit from.

  ```csharp
  public class BaseEntity
  {
      [Key]
      public int Id { get; set; }
  }
  ```

- **BinancePriceUpdate.cs**: Represents the structure of price updates received from Binance WebSocket. 

  ```csharp
  public class BinancePriceUpdate
  {
      [JsonPropertyName("s")]
      public string? Symbol { get; set; }

      [JsonPropertyName("c")]
      [JsonConverter(typeof(StringToDecimalConverter))]
      public decimal LastPrice { get; set; }
  }
  ```

- **PriceModel.cs**: Defines the structure of price records stored in the database.

  ```csharp
  public class PriceModel : BaseEntity
  {
      public string? Symbol { get; set; }
      public decimal Price { get; set; }
      public DateTime Timestamp { get; set; }
  }
  ```

#### **Database Initialization**

- **DatabaseInitializer.cs**: Contains methods for checking and initializing the database, applying migrations if necessary.

  ```csharp
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
  ```

### 4. Domain

The `Domain` project encapsulates business logic and background services for processing cryptocurrency data.

#### **Interfaces**

- **IPriceService.cs**: Defines the contract for price services, including methods to fetch 24-hour average prices and calculate Simple Moving Averages (SMA).

  ```csharp
  public interface IPriceService
  {
      Task<decimal> Get24hAvgPriceAsync(string symbol);
      Task<decimal> GetSimpleMovingAverageAsync(string symbol, int n, string p, DateTime? s);
  }
  ```

#### **Services**

- **BinancePriceService.cs**: A background service that connects to the Binance WebSocket API to receive real-time price updates. It processes these updates and stores them in the database.

  ```csharp
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
  ```

- **PriceService.cs**: Implements `IPriceService` to provide methods for fetching and calculating price metrics, such as the 24-hour average price and SMA.

  ```csharp
  public class PriceService : IPriceService
  {
      private readonly IUnitOfWork _unitOfWork;
      private readonly IPriceRepository _priceRepository;
      private readonly IMemoryCache _cache;

      public PriceService(IUnitOfWork unitOfWork, IPriceRepository priceRepository, IMemoryCache cache)
      {
          _unitOfWork = unitOfWork;
          _priceRepository = priceRepository;
          _cache = cache;
      }

      public async Task<decimal> Get24hAvgPriceAsync(string symbol)
      {
          var cacheKey = $"{symbol}_24hAvgPrice";
          if (_cache.TryGetValue(cacheKey, out decimal cachedPrice))
          {
              return cachedPrice;
          }

          try
          {
              var avgPrice = await _priceRepository.Get24hAvgPriceAsync(symbol);
              _cache.Set(cacheKey, avgPrice, TimeSpan.FromMinutes(10));
              return avgPrice;
          }
          catch (ExceptionHandler ex)
          {
              throw new ExceptionHandler($"Error in service: {ex.Message}", ex.ErrorCode, ex);
          }
      }

      public async Task<decimal> GetSimpleMovingAverageAsync(string symbol, int n, string p, DateTime? s)
      {
          TimeSpan timePeriod = p switch
          {
              "1w" => TimeSpan.FromDays(7),
              "1d" => TimeSpan.FromDays(1),
              "30m" => TimeSpan.FromMinutes(30),
              "5m" => TimeSpan.FromMinutes(5),
              "1m" => TimeSpan.FromMinutes(1),
              _ => throw new ArgumentException("Invalid time period."),
          };

          try
          {
              var sma = await _priceRepository.GetSimpleMovingAverageAsync(symbol, n, timePeriod, s);
              return sma;
          }
          catch (ExceptionHandler ex)
          {
              throw new ExceptionHandler($"Error in service: {ex.Message}", ex.ErrorCode, ex);
          }
      }
  }
  ```

### 5. Exceptions

The `Exceptions` project provides custom exception handling mechanisms for the application.

#### **ExceptionHandler.cs**

Defines a custom exception class that includes an error code for more detailed error reporting.

```csharp
public class ExceptionHandler : Exception
{
    public string ErrorCode { get; }

    public ExceptionHandler(string message, string errorCode, Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
```

### 6. Repository

The `Repository` project implements data access and repository patterns for interacting with the database.

#### **Base**

- **IRepository.cs**: Defines the generic repository interface for CRUD operations.

  ```csharp
  public interface IRepository<T> where T : BaseEntity
  {
      Task<T> GetByIdAsync(int id);
      Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null);
      Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> where);
      Task AddAsync(T entity);
      Task UpdateAsync(T entity);
      Task DeleteAsync(T entity);
  }
  ```

- **IUnitOfWork.cs**: Defines the Unit of Work interface to manage transactions.

  ```csharp
  public interface IUnitOfWork : IDisposable
  {
      IRepository<T> Repository<T>() where T : BaseEntity;
      Task SaveChangesAsync();
  }
  ```

- **Repository.cs**: Implements the generic repository interface for CRUD operations.

  ```csharp
  public class Repository<T> : IRepository<T> where T : BaseEntity
  {
      protected readonly DbSet<T> _entities;
      private readonly CryptoPriceDbContext _context;

      public Repository(CryptoPriceDbContext context)
      {
          _context = context;
          _entities = context.Set<T>();
      }

      public async Task<T> GetByIdAsync(int id)
      {
          return await _entities.FindAsync(id);
      }

      public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null)
      {
          return filter == null ? await _entities.ToListAsync() : await _entities.Where(filter).ToListAsync();
      }

     

 public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> where)
      {
          return await _entities.FirstOrDefaultAsync(where);
      }

      public async Task AddAsync(T entity)
      {
          await _entities.AddAsync(entity);
          await _context.SaveChangesAsync();
      }

      public async Task UpdateAsync(T entity)
      {
          _entities.Update(entity);
          await _context.SaveChangesAsync();
      }

      public async Task DeleteAsync(T entity)
      {
          _entities.Remove(entity);
          await _context.SaveChangesAsync();
      }
  }
  ```

- **UnitOfWork.cs**: Manages database transactions and provides access to repositories.

  ```csharp
  public class UnitOfWork : IUnitOfWork
  {
      private readonly CryptoPriceDbContext _context;
      private readonly IServiceProvider _serviceProvider;

      public UnitOfWork(CryptoPriceDbContext context, IServiceProvider serviceProvider)
      {
          _context = context;
          _serviceProvider = serviceProvider;
      }

      public IRepository<T> Repository<T>() where T : BaseEntity
      {
          return _serviceProvider.GetService<IRepository<T>>();
      }

      public async Task SaveChangesAsync()
      {
          await _context.SaveChangesAsync();
      }

      public void Dispose()
      {
          _context.Dispose();
      }
  }
  ```

#### **Implementations**

- **IPriceRepository.cs**: Extends the generic repository interface for `PriceModel` entities.

  ```csharp
  public interface IPriceRepository : IRepository<PriceModel>
  {
      Task<decimal> Get24hAvgPriceAsync(string symbol);
      Task<decimal> GetSimpleMovingAverageAsync(string symbol, int n, TimeSpan timePeriod, DateTime? startDate);
  }
  ```

- **PriceRepository.cs**: Implements the `IPriceRepository` interface to provide methods for fetching and calculating cryptocurrency price data.

  ```csharp
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
  ```

### 7. Utils

The `Utils` project provides utility classes used across the application.

- **StringToDecimalConverter.cs**: A custom JSON converter for handling decimal values represented as strings in JSON payloads.

  ```csharp
  public class StringToDecimalConverter : JsonConverter<decimal>
  {
      public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
          if (reader.TokenType == JsonTokenType.String)
          {
              var stringValue = reader.GetString();
              if (decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
              {
                  return value;
              }
              throw new JsonException($"Unable to convert \"{stringValue}\" to {typeToConvert}.");
          }

          return reader.GetDecimal();
      }

      public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
      {
          writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
      }
  }
  ```

## Getting Started

### Prerequisites

- .NET SDK 8.0 or higher
- SQL Server
- A compatible code editor like Visual Studio or VS Code

### Installation

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/AyatoShirayouki/CryptoPriceApp
   ```

### Running the API

1. **Navigate to the API Project**:

  Open appsettings.json and place the connection string for your MsSql database.

2. **Run the API**:

### Using the Console App

1. **Navigate to the Console App Project**:

   - Go to \bin\Debug\net8.0
   - open appsettings.json and place the connection string for your MsSql database.

2. **Run the Console App**:

3. **Enter Commands**:

   Follow the prompts in the console to request cryptocurrency data.

## Technical Details

### Database Initialization

The database is initialized using the `DatabaseInitializer` class in the `Data` project. It checks if the database exists and applies migrations if necessary. This process is invoked at the start of both the API and console applications to ensure the database is ready before any operations are performed.

### Error Handling

Custom error handling is implemented through the `ExceptionHandler` class in the `Exceptions` project. This class provides meaningful error messages and codes that propagate through the service and repository layers, ensuring consistent error reporting.

### Real-time Price Updates

The `BinancePriceService` in the `Domain` project connects to the Binance WebSocket API to receive real-time cryptocurrency price updates. These updates are processed and stored in the database asynchronously, allowing the application to provide up-to-date price information.

## License

This project is licensed under the MIT License.
