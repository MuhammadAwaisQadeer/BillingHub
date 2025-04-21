using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                    .Where(r => r.ClientId == clientId && !r.IsDeleted) // Exclude soft-deleted resources
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving resources for client {clientId}");
                return new List<Resource>();
            }
        }

        public async Task<int> GetTotalHoursConsumedAsync(int resourceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resource = await context.Resources
                    .Where(r => r.ResourceId == resourceId && !r.IsDeleted)
                    .FirstOrDefaultAsync();

                return resource?.ConsumedTotalHours ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving total hours for resource {resourceId}");
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
                    .Where(r => r.ClientId == clientId && !r.IsDeleted)
                    .ToListAsync();

                return resources.Sum(r => r.ConsumedTotalHours * (r.Employee?.HourlyRate ?? 0));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating billing for client {clientId}");
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
                    .Where(r => !r.IsDeleted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all resources with details");
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
                    .Where(r => r.ResourceId == resourceId && !r.IsDeleted)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving resource details for {resourceId}");
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
                    .Include(r => r.OwnerProfile)
                    .Where(r => !r.IsDeleted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all resources");
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
                    .Where(r => r.ResourceId == resourceId && !r.IsDeleted)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving resource by ID {resourceId}");
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
                _logger.LogError(ex, "Error adding resource");
            }
        }

        public async Task UpdateAsync(Resource resource)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var existingResource = await context.Resources
                    .Where(r => r.ResourceId == resource.ResourceId && !r.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingResource != null)
                {
                    existingResource.ClientId = resource.ClientId;
                    existingResource.ResourceName = resource.ResourceName;
                    existingResource.EmployeeId = resource.EmployeeId;
                    existingResource.ConsumedTotalHours = resource.ConsumedTotalHours;
                    existingResource.IsActive = resource.IsActive;
                    existingResource.OwnerProfileId = resource.OwnerProfileId;

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating resource {resource.ResourceId}");
            }
        }

        public async Task DeleteAsync(int resourceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resource = await context.Resources
                    .Where(r => r.ResourceId == resourceId && !r.IsDeleted)
                    .FirstOrDefaultAsync();

                if (resource != null)
                {
                    resource.IsDeleted = true;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting resource {resourceId}");
            }
        }
    }
}
