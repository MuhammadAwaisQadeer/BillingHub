using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repositories
{
    public class GenericRepository<T> : IRepository<T> where T : class
    {
        public readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public GenericRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        //protected readonly DbSet<T> _dbSet;

        //public GenericRepository(ApplicationDbContext context)
        //{
        //    _context = context;
        //    _dbSet = _context.Set<T>();
        //}

        public async Task<IEnumerable<T>> GetAllAsync(bool includeRelatedEntities = false)
        {
            using var context = _contextFactory.CreateDbContext();
            IQueryable<T> query = context.Set<T>();

            if (includeRelatedEntities && typeof(T) == typeof(Client))
            {
                query = query.Include("CountryCurrency");
            }

            return await query.AsNoTracking().ToListAsync();
        }



        public virtual async Task<T> GetByIdAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public virtual async Task AddAsync(T entity)
        {
            using var context = _contextFactory.CreateDbContext();
            await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(T entity)
        {
            using var context = _contextFactory.CreateDbContext();
            context.Set<T>().Update(entity);
            await context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = await context.Set<T>().FindAsync(id);
            if (entity != null)
            {
                context.Set<T>().Remove(entity);
                await context.SaveChangesAsync();
            }
        }
    }
}
