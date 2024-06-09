using Data.Context;
using Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Repository.Base.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Base
{
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
}
