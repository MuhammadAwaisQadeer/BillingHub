using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Client_Invoice_System.Models;
using Client_Invoice_System.Data;

namespace Client_Invoice_System.Repository
{
    public class CountryCurrencyRepository
    {
        private readonly ApplicationDbContext _context;

        public CountryCurrencyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CountryCurrency>> GetAllAsync()
        {
            try
            {
                return await _context.CountryCurrencies
               .Where(cc => !cc.IsDeleted)
               .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
           
        }
    }
}
