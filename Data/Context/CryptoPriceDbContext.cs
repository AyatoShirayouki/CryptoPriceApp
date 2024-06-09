using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Context
{
	public class CryptoPriceDbContext : DbContext
	{
		public DbSet<PriceModel> Prices { get; set; }

		public CryptoPriceDbContext(DbContextOptions<CryptoPriceDbContext> options)
			: base(options)
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
}
