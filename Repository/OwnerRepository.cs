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
    public class OwnerRepository : GenericRepository<OwnerProfile>
    {
        public OwnerRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
            : base(contextFactory) { }

        public async Task<IEnumerable<OwnerProfile>> GetAllOwnerProfilesAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            var owners = await context.Set<OwnerProfile>().Include(o => o.CountryCurrency)
                                .AsNoTracking()
                                .ToListAsync();
            return owners;
        }

        public async Task<IEnumerable<OwnerProfile>> GetAllOwnersAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<OwnerProfile>()
                                .AsNoTracking()
                                .ToListAsync();
        }

        public async Task<OwnerProfile> GetOwnerProfileByIdAsync(int ownerId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<OwnerProfile>()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(o => o.Id == ownerId);
        }

        public async Task<OwnerProfile> GetOwnerProfileAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<OwnerProfile>().AsNoTracking().FirstOrDefaultAsync() ?? new OwnerProfile();
        }

        public async Task AddOwnerProfileAsync(OwnerProfile owner)
        {
            using var context = _contextFactory.CreateDbContext();
            try
            {
                await context.Set<OwnerProfile>().AddAsync(owner);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateOwnerProfileAsync(OwnerProfile owner)
        {
            using var context = _contextFactory.CreateDbContext();
            var existingOwner = await context.Set<OwnerProfile>().FirstOrDefaultAsync(o => o.Id == owner.Id);
            if (existingOwner != null)
            {
                existingOwner.OwnerName = owner.OwnerName;
                existingOwner.BillingEmail = owner.BillingEmail;
                existingOwner.PhoneNumber = owner.PhoneNumber;
                existingOwner.BillingAddress = owner.BillingAddress;
                existingOwner.IBANNumber = owner.IBANNumber;
                existingOwner.AccountNumber = owner.AccountNumber;
                existingOwner.CustomCurrency = owner.CustomCurrency;
                existingOwner.BranchAddress = owner.BranchAddress;
                existingOwner.BeneficeryAddress = owner.BeneficeryAddress;

                context.Update(existingOwner);
            }
            else
            {
                await context.Set<OwnerProfile>().AddAsync(owner);
            }
            await context.SaveChangesAsync();
        }

        public async Task DeleteOwnerProfileAsync(int ownerId)
        {
            using var context = _contextFactory.CreateDbContext();
            var owner = await context.Set<OwnerProfile>().FindAsync(ownerId);
            if (owner != null)
            {
                context.Set<OwnerProfile>().Remove(owner);
                await context.SaveChangesAsync();
            }
        }
    }
}
