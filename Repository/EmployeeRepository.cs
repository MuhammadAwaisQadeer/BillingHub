using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public class EmployeeRepository : GenericRepository<Employee>
    {
        public EmployeeRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
            : base(contextFactory) { }

        public async Task<IEnumerable<Employee>> GetByDesignationAsync(Designation designation)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Employees
                    .Where(e => e.Designation == designation && !e.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public async Task<decimal> CalculateTotalEarningsAsync(int employeeId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var employee = await context.Employees
                    .Include(e => e.Resources)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && !e.IsDeleted);

                if (employee == null || employee.Resources == null)
                    return 0;

                return employee.Resources.Sum(r => r.ConsumedTotalHours * employee.HourlyRate);
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Employees
                    .Where(e => !e.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        public override async Task<Employee?> GetByIdAsync(int employeeId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && !e.IsDeleted);
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        public override async Task AddAsync(Employee employee)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                await context.Employees.AddAsync(employee);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        public override async Task UpdateAsync(Employee employee)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                context.Employees.Update(employee);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
          
        }

        public override async Task DeleteAsync(int employeeId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var employee = await context.Employees.FindAsync(employeeId);
                if (employee != null)
                {
                    employee.IsDeleted = true;
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
