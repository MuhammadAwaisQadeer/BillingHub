using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repository;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<T>> GetAllAsync(bool includeRelatedEntities = false)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                IQueryable<T> query = context.Set<T>();

                if (includeRelatedEntities && typeof(T) == typeof(Client))
                {
                    query = query.Include("CountryCurrency");
                }

                if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
                {
                    query = query.Where(e => !EF.Property<bool>(e, "IsDeleted"));
                }

                return await query.AsNoTracking().ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
        
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                IQueryable<T> query = context.Set<T>();

                if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
                {
                    query = query.Where(e => !EF.Property<bool>(e, "IsDeleted"));
                }

                return await query.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
            }
            catch (Exception)
            {

                throw;
            }
          
        }

        public virtual async Task AddAsync(T entity)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                await context.Set<T>().AddAsync(entity);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public virtual async Task UpdateAsync(T entity)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                context.Set<T>().Update(entity);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        // Soft Delte: Mark as deleted instead of removing
        public virtual async Task DeleteAsync(int id)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var entity = await context.Set<T>().FindAsync(id);
                if (entity != null)
                {
                    if (entity is ISoftDeletable deletableEntity)
                    {
                        deletableEntity.IsDeleted = true;
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
           
        }
    }
}
