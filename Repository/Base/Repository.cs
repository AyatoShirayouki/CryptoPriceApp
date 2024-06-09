using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Base.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Base
{
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
			return filter == null
				? await _entities.ToListAsync()
				: await _entities.Where(filter).ToListAsync();
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
}
