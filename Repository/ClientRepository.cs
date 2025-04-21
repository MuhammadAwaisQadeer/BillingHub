using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public class ClientRepository : GenericRepository<Client>
    {
        public ClientRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
           : base(contextFactory) { }

        public async Task<List<Client>> GetAllClientsWithDetailsAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Clients
                .Where(c => !c.IsDeleted)  
                .Include(c => c.CountryCurrency)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetActiveClientsCountAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Clients.CountAsync(c => !c.IsDeleted);  
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching active clients count: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetTotalEmployeesAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Employees.CountAsync(e => !e.IsDeleted);  
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching total employees: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetActiveContractsCountAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Resources.CountAsync(r => r.IsActive && !r.IsDeleted);  
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching active contracts count: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Clients.AnyAsync(c => c.Email == email && !c.IsDeleted);  
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking email existence: {ex.Message}");
                return false;
            }
        }

        public async Task<Client> GetClientWithResourcesAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Clients
                    .Where(c => !c.IsDeleted)  
                    .Include(c => c.Resources)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving client with resources: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Employee>> GetEmployeesByClientIdAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Resources
                    .Where(r => r.ClientId == clientId && !r.IsDeleted)  
                    .Select(r => r.Employee)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching employees for client {clientId}: {ex.Message}");
                return new List<Employee>();
            }
        }

        /// <summary>
        /// Soft delete a client by setting IsDeleted = true instead of removing from the database.
        /// </summary>
        public override async Task DeleteAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var client = await context.Clients
                    .Include(c => c.Resources)  
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client == null)
                {
                    Console.WriteLine($"Client with ID {clientId} not found.");
                    return;
                }

                client.IsDeleted = true;

                if (client.Resources != null)
                {
                    foreach (var resource in client.Resources)
                    {
                        resource.IsDeleted = true;
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error soft deleting client: {ex.Message}");
            }
        }

        public async Task<Client?> GetByIdAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Clients
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving client by ID: {ex.Message}");
                return null;
            }
        }

        public async Task AddAsync(Client client)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                await context.Clients.AddAsync(client);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding client: {ex.Message}");
            }
        }

        public async Task UpdateAsync(Client client)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                context.Clients.Update(client);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating client: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore a soft-deleted client.
        /// </summary>
        public async Task RestoreClientAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var client = await context.Clients
                    .Include(c => c.Resources)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsDeleted);

                if (client == null)
                {
                    Console.WriteLine($"Client with ID {clientId} is not found or not deleted.");
                    return;
                }

                client.IsDeleted = false;

                if (client.Resources != null)
                {
                    foreach (var resource in client.Resources)
                    {
                        resource.IsDeleted = false;
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring client: {ex.Message}");
            }
        }
    }
}
