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
    public class InvoiceRepository : GenericRepository<Invoice>
    {
        public InvoiceRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
            : base(contextFactory) { }

        public async Task<List<Invoice>> GetFilteredInvoicesAsync(DateTime? date, int? month, int? clientId)
        {
            using var context = _contextFactory.CreateDbContext();
            IQueryable<Invoice> query = context.Invoices.AsNoTracking();

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
            using var context = _contextFactory.CreateDbContext();
            return await context.Invoices
                               .Where(i => i.IsPaid)
                               .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        }

        public async Task<decimal> GetUnpaidInvoicesAmountAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Invoices
                               .Where(i => !i.IsPaid)
                               .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        }

        // Uncomment and adjust the following method if you need to track overdue invoices.
        // public async Task<int> GetOverdueInvoicesCountAsync()
        // {
        //     using var context = _contextFactory.CreateDbContext();
        //     return await context.Invoices
        //                        .CountAsync(i => !i.IsPaid && i.DueDate < DateTime.UtcNow);
        // }
    }
}
