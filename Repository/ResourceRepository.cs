using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public class ResourceRepository(ApplicationDbContext context) : GenericRepository<Resource>(context)
    {
        public async Task<IEnumerable<Resource>> GetResourcesByClientAsync(int clientId)
        {
            try
            {
                return await _dbSet
                    .Include(r => r.Employee)
                    .Where(r => r.ClientId == clientId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching resources for Client {clientId}: {ex.Message}");
                return Enumerable.Empty<Resource>();
            }
        }

        public async Task<int> GetTotalHoursConsumedAsync(int resourceId)
        {
            try
            {
                var resource = await _dbSet.FindAsync(resourceId);
                return resource?.ConsumedTotalHours ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching total hours for Resource {resourceId}: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> CalculateClientBillingAsync(int clientId)
        {
            try
            {
                var resources = await _dbSet
                    .Include(r => r.Employee)
                    .Where(r => r.ClientId == clientId)
                    .ToListAsync();

                return resources.Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calculating billing for Client {clientId}: {ex.Message}");
                return 0m;
            }
        }

        public async Task<IEnumerable<Resource>> GetAllResourcesWithDetailsAsync()
        {
            try
            {
                return await _dbSet
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching all resources: {ex.Message}");
                return Enumerable.Empty<Resource>();
            }
        }

        public async Task<Resource?> GetResourceDetailsAsync(int resourceId)
        {
            try
            {
                return await _dbSet
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .FirstOrDefaultAsync(r => r.ResourceId == resourceId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching details for Resource {resourceId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Resource>> GetAllAsync()
        {
            try
            {
                return await _context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching all resources: {ex.Message}");
                return new List<Resource>();
            }
        }

        public async Task<Resource?> GetByIdAsync(int resourceId)
        {
            try
            {
                return await _context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .FirstOrDefaultAsync(r => r.ResourceId == resourceId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching resource ID {resourceId}: {ex.Message}");
                return null;
            }
        }


        public async Task AddAsync(Resource resource)
        {
            try
            {
                await _context.Resources.AddAsync(resource);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Resource {resource.ResourceId} added successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding resource: {ex.Message}");
            }
        }

        public async Task UpdateAsync(Resource resource)
        {
            try
            {
                var existingResource = await _context.Resources.FindAsync(resource.ResourceId);
                if (existingResource != null)
                {
                    existingResource.ClientId = resource.ClientId;
                    existingResource.ResourceName = resource.ResourceName;
                    existingResource.EmployeeId = resource.EmployeeId;
                    existingResource.ConsumedTotalHours = resource.ConsumedTotalHours;
                    existingResource.IsActive = resource.IsActive;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ Resource {resource.ResourceId} updated successfully.");
                }
                else
                {
                    Console.WriteLine($"⚠️ Resource {resource.ResourceId} not found for update.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating Resource {resource.ResourceId}: {ex.Message}");
            }
        }

        public async Task DeleteAsync(int resourceId)
        {
            try
            {
                var resource = await _context.Resources
                    .Include(r => r.Client) // Ensure Client is included
                    .FirstOrDefaultAsync(r => r.ResourceId == resourceId);

                if (resource == null)
                {
                    Console.WriteLine($"⚠️ Resource with ID {resourceId} not found.");
                    return;
                }

                _context.Resources.Remove(resource);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Resource ID {resourceId} deleted successfully.");
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"❌ SQL Error while deleting resource: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ General Error while deleting resource: {ex.Message}");
            }
        }

    }
}
