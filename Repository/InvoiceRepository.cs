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
            try
            {
                using var context = _contextFactory.CreateDbContext();
                IQueryable<Invoice> query = context.Invoices
                    .Include(i => i.Client)
                    .Include(i => i.InvoiceItems)
                    .Where(i => !i.IsDeleted)
                    .AsNoTracking();

                if (date.HasValue)
                    query = query.Where(i => i.InvoiceDate.Date == date.Value.Date);

                if (month.HasValue)
                    query = query.Where(i => i.InvoiceDate.Month == month.Value);

                if (clientId.HasValue && clientId > 0)
                    query = query.Where(i => i.ClientId == clientId.Value);

                return await query.ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public async Task<Invoice> GetInvoiceWithItemsAsync(int invoiceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Invoices
                    .Include(i => i.InvoiceItems)
                    .Where(i => !i.IsDeleted) // Exclude soft-deleted invoices
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Invoices
                    .Where(i => !i.IsDeleted) 
                    .SumAsync(i => (decimal?)i.PaidAmount) ?? 0;
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public async Task<decimal> GetUnpaidInvoicesAmountAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Invoices
                    .Where(i => !i.IsDeleted) 
                    .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public async Task UpdateInvoiceAmountsAsync(int invoiceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var invoice = await context.Invoices
                    .Include(i => i.InvoiceItems)
                    .Where(i => !i.IsDeleted) 
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice != null)
                {
                    invoice.TotalAmount = invoice.InvoiceItems.Sum(item => item.TotalAmount);
                    invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public override async Task DeleteAsync(int invoiceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var invoice = await context.Invoices.FindAsync(invoiceId);
                if (invoice != null)
                {
                    invoice.IsDeleted = true;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
            
        }
    }
}
