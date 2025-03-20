using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public class OwnerRepository : GenericRepository<OwnerProfile>
    {
        public OwnerRepository(ApplicationDbContext context) : base(context) { }

        // ✅ Get All Owner Profiles
        public async Task<IEnumerable<OwnerProfile>> GetAllOwnerProfilesAsync()
        {
            return await _dbSet.ToListAsync();
        }
        public async Task<IEnumerable<OwnerProfile>> GetAllOwnersAsync()
        {
            return await _dbSet.ToListAsync();
        }

        // ✅ Get a Single Owner Profile by ID
        public async Task<OwnerProfile> GetOwnerProfileByIdAsync(int ownerId)
        {
            return await _dbSet.FirstOrDefaultAsync(o => o.Id == ownerId);
        }

        // ✅ Get First Owner Profile (Existing)
        public async Task<OwnerProfile> GetOwnerProfileAsync()
        {
            return await _dbSet.FirstOrDefaultAsync() ?? new OwnerProfile();
        }

        // ✅ Add a New Owner Profile
        public async Task AddOwnerProfileAsync(OwnerProfile owner)
        {
            try
            {
                await _dbSet.AddAsync(owner);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        // ✅ Update Owner Profile
        public async Task UpdateOwnerProfileAsync(OwnerProfile owner)
        {
            var existingOwner = await _dbSet.FirstOrDefaultAsync(o => o.Id == owner.Id);
            if (existingOwner != null)
            {
                existingOwner.OwnerName = owner.OwnerName;
                existingOwner.BillingEmail = owner.BillingEmail;
                existingOwner.PhoneNumber = owner.PhoneNumber;
                existingOwner.BillingAddress = owner.BillingAddress;

                _context.Update(existingOwner);
            }
            else
            {
                await _dbSet.AddAsync(owner);
            }

            await _context.SaveChangesAsync();
        }

        // ✅ Delete Owner Profile
        public async Task DeleteOwnerProfileAsync(int ownerId)
        {
            var owner = await _dbSet.FirstOrDefaultAsync(o => o.Id == ownerId);
            if (owner != null)
            {
                _dbSet.Remove(owner);
                await _context.SaveChangesAsync();
            }
        }
    }
}
