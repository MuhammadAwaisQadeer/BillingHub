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
        public EmployeeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Employee>> GetByDesignationAsync(Designation designation)
        {
            return await _dbSet.Where(e => e.Designation == designation).ToListAsync();
        }


        public async Task<decimal> CalculateTotalEarningsAsync(int employeeId)
        {
            var employee = await _dbSet
                .Include(e => e.Resources)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null || employee.Resources == null)
                return 0;

            return employee.Resources.Sum(r => r.ConsumedTotalHours * employee.HourlyRate);
        }


        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<Employee> GetByIdAsync(int employeeId)
        {
            return await _dbSet.FindAsync(employeeId);
        }

        public async Task AddAsync(Employee employee)
        {
            await _dbSet.AddAsync(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Employee employee)
        {
            _dbSet.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int employeeId)
        {
            var employee = await _dbSet.FindAsync(employeeId);
            if (employee != null)
            {
                _dbSet.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }
    }
}
