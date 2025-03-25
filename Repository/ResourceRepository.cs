using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public class ResourceRepository : GenericRepository<Resource>
    {
        public ResourceRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
            : base(contextFactory) { }

        public async Task<IEnumerable<Resource>> GetResourcesByClientAsync(int clientId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Resources
                .Include(r => r.Employee)
                .Where(r => r.ClientId == clientId)
                .ToListAsync();
        }

        public async Task<int> GetTotalHoursConsumedAsync(int resourceId)
        {
            using var context = _contextFactory.CreateDbContext();
            var resource = await context.Resources.FindAsync(resourceId);
            return resource?.ConsumedTotalHours ?? 0;
        }

        public async Task<decimal> CalculateClientBillingAsync(int clientId)
        {
            using var context = _contextFactory.CreateDbContext();
            var resources = await context.Resources
                .Include(r => r.Employee)
                .Where(r => r.ClientId == clientId)
                .ToListAsync();

            return resources.Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);
        }

        public async Task<IEnumerable<Resource>> GetAllResourcesWithDetailsAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Resources
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .ToListAsync();
        }

        public async Task<Resource?> GetResourceDetailsAsync(int resourceId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Resources
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId);
        }

        public async Task<List<Resource>> GetAllAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Resources
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .ToListAsync();
        }

        public async Task<Resource?> GetByIdAsync(int resourceId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Resources
                .Include(r => r.Client)
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId);
        }

        public async Task AddAsync(Resource resource)
        {
            using var context = _contextFactory.CreateDbContext();
            await context.Resources.AddAsync(resource);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Resource resource)
        {
            using var context = _contextFactory.CreateDbContext();
            var existingResource = await context.Resources.FindAsync(resource.ResourceId);
            if (existingResource != null)
            {
                existingResource.ClientId = resource.ClientId;
                existingResource.ResourceName = resource.ResourceName;
                existingResource.EmployeeId = resource.EmployeeId;
                existingResource.ConsumedTotalHours = resource.ConsumedTotalHours;
                existingResource.IsActive = resource.IsActive;
                // Optionally update other properties if needed

                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int resourceId)
        {
            using var context = _contextFactory.CreateDbContext();
            var resource = await context.Resources
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId);
            if (resource != null)
            {
                context.Resources.Remove(resource);
                await context.SaveChangesAsync();
            }
        }
    }
}
