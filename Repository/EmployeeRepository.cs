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

        // Get employees by designation (excluding soft-deleted employees)
        public async Task<IEnumerable<Employee>> GetByDesignationAsync(Designation designation)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Employees
                .Where(e => e.Designation == designation && !e.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // Calculate total earnings for an employee based on consumed hours and hourly rate
        public async Task<decimal> CalculateTotalEarningsAsync(int employeeId)
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

        // Get all employees (excluding soft-deleted employees)
        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Employees
                .Where(e => !e.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // Get employee by ID (excluding soft-deleted employees)
        public override async Task<Employee?> GetByIdAsync(int employeeId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && !e.IsDeleted);
        }

        public override async Task AddAsync(Employee employee)
        {
            using var context = _contextFactory.CreateDbContext();
            await context.Employees.AddAsync(employee);
            await context.SaveChangesAsync();
        }

        public override async Task UpdateAsync(Employee employee)
        {
            using var context = _contextFactory.CreateDbContext();
            context.Employees.Update(employee);
            await context.SaveChangesAsync();
        }

        // Soft Delete: Mark employee as deleted instead of removing
        public override async Task DeleteAsync(int employeeId)
        {
            using var context = _contextFactory.CreateDbContext();
            var employee = await context.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                employee.IsDeleted = true;
                await context.SaveChangesAsync();
            }
        }
    }
}
