using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Client_Invoice_System.Repository
{
    public class ResourceRepository : GenericRepository<Resource>
    {
        private readonly ILogger<ResourceRepository> _logger;

        public ResourceRepository(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ResourceRepository> logger)
            : base(contextFactory)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<Resource>> GetResourcesByClientAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Resources
                    .Include(r => r.Employee)
                    .Where(r => r.ClientId == clientId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving resources for client {clientId}: {ex.Message}");
                return new List<Resource>();
            }
        }

        public async Task<int> GetTotalHoursConsumedAsync(int resourceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resource = await context.Resources.FindAsync(resourceId);
                return resource?.ConsumedTotalHours ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving total hours for resource {resourceId}: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> CalculateClientBillingAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resources = await context.Resources
                    .Include(r => r.Employee)
                    .Where(r => r.ClientId == clientId)
                    .ToListAsync();

                return resources.Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating billing for client {clientId}: {ex.Message}");
                return 0;
            }
        }

        public async Task<IEnumerable<Resource>> GetAllResourcesWithDetailsAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving all resources with details: {ex.Message}");
                return new List<Resource>();
            }
        }

        public async Task<Resource?> GetResourceDetailsAsync(int resourceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .FirstOrDefaultAsync(r => r.ResourceId == resourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving resource details for {resourceId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Resource>> GetAllAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving all resources: {ex.Message}");
                return new List<Resource>();
            }
        }

        public async Task<Resource?> GetByIdAsync(int resourceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .FirstOrDefaultAsync(r => r.ResourceId == resourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving resource by ID {resourceId}: {ex.Message}");
                return null;
            }
        }

        public async Task AddAsync(Resource resource)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                await context.Resources.AddAsync(resource);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding resource: {ex.Message}");
            }
        }

        public async Task UpdateAsync(Resource resource)
        {
            try
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

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating resource {resource.ResourceId}: {ex.Message}");
            }
        }

        public async Task DeleteAsync(int resourceId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting resource {resourceId}: {ex.Message}");
            }
        }
    }
}
