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
        public ActiveClientRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<ActiveClient>> GetAllActiveClientsAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<ActiveClient?> GetActiveClientByIdAsync(int clientId)
        {
            return await _dbSet.FirstOrDefaultAsync(ac => ac.ClientId == clientId);
        }

        public async Task UpdateClientStatusAsync(int clientId, bool status)
        {
            var activeClient = await _dbSet.FirstOrDefaultAsync(ac => ac.ClientId == clientId);
            if (activeClient != null)
            {
                activeClient.Status = status;
                await _context.SaveChangesAsync();
            }
        }
    }
}
