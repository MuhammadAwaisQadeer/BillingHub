using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(bool includeRelatedEntities = false);
        Task<T> GetByIdAsync(int id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id); 
    }
}
