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
            IQueryable<Invoice> query = context.Invoices
                .Include(i => i.Client)
                .Include(i => i.InvoiceItems)
                .AsNoTracking();

            if (date.HasValue)
                query = query.Where(i => i.InvoiceDate.Date == date.Value.Date);

            if (month.HasValue)
                query = query.Where(i => i.InvoiceDate.Month == month.Value);

            if (clientId.HasValue && clientId > 0)
                query = query.Where(i => i.ClientId == clientId.Value);

            return await query.ToListAsync();
        }

        public async Task<Invoice> GetInvoiceWithItemsAsync(int invoiceId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Invoices.SumAsync(i => (decimal?)i.PaidAmount) ?? 0;
        }

        public async Task<decimal> GetUnpaidInvoicesAmountAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Invoices.SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        }

        public async Task UpdateInvoiceAmountsAsync(int invoiceId)
        {
            using var context = _contextFactory.CreateDbContext();
            var invoice = await context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice != null)
            {
                invoice.TotalAmount = invoice.InvoiceItems.Sum(item => item.TotalAmount);
                invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
                await context.SaveChangesAsync();
            }
        }
    }
}
