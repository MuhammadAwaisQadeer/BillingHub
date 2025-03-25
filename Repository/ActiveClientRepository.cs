using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public class ActiveClientRepository : GenericRepository<ActiveClient>
    {
        public ActiveClientRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
           : base(contextFactory) { }

        public async Task<IEnumerable<ActiveClient>> GetAllActiveClientsAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<ActiveClient>().AsNoTracking().ToListAsync();
        }

        public async Task<ActiveClient?> GetActiveClientByIdAsync(int clientId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<ActiveClient>().AsNoTracking()
                .FirstOrDefaultAsync(ac => ac.ClientId == clientId);
        }

        public async Task UpdateClientStatusAsync(int clientId, bool status)
        {
            using var context = _contextFactory.CreateDbContext();
            var activeClient = await context.Set<ActiveClient>()
                .FirstOrDefaultAsync(ac => ac.ClientId == clientId);

            if (activeClient != null)
            {
                activeClient.Status = status;
                await context.SaveChangesAsync();
            }
        }
    }
}
