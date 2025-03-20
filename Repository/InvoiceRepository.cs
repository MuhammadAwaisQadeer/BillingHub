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
    public class InvoiceRepository : GenericRepository<Invoice>
    {
        public InvoiceRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<Invoice>> GetFilteredInvoicesAsync(DateTime? date, int? month, int? clientId)
        {
            IQueryable<Invoice> query = _context.Invoices.AsNoTracking();

            if (date.HasValue)
            {
                query = query.Where(i => i.InvoiceDate.Date == date.Value.Date);
            }

            if (month.HasValue)
            {
                query = query.Where(i => i.InvoiceDate.Month == month.Value);
            }

            if (clientId.HasValue && clientId > 0)
            {
                query = query.Where(i => i.ClientId == clientId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Invoices.Where(i => i.IsPaid).SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        }

        public async Task<decimal> GetUnpaidInvoicesAmountAsync()
        {
            return await _context.Invoices.Where(i => !i.IsPaid).SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        }

        //public async Task<int> GetOverdueInvoicesCountAsync()
        //{
        //    return await _context.Invoices.CountAsync(i => !i.IsPaid && i.DueDate < DateTime.UtcNow);
        //}
    }
}
