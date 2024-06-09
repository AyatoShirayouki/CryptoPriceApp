using Data;
using Data.Context;
using Domain.Interfaces;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Repository.Base;
using Repository.Base.Interfaces;
using Repository.Implementations;
using Repository.Implementations.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
	.AddXmlSerializerFormatters();

builder.Services.AddDbContext<CryptoPriceDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
		sqlOptions => sqlOptions.CommandTimeout(300)));

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPriceRepository, PriceRepository>();
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddHostedService<BinancePriceService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await DatabaseInitializer.InitializeDatabaseAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
