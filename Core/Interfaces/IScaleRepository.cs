using System.Collections.Generic;
using System.Threading.Tasks;
using AppsielPrintManager.Core.Models;

namespace AppsielPrintManager.Core.Interfaces
{
    public interface IScaleRepository
    {
        Task<List<Scale>> GetAllAsync();
        Task<Scale?> GetByIdAsync(string id);
        Task AddAsync(Scale scale);
        Task UpdateAsync(Scale scale);
        Task DeleteAsync(string id);
    }
}
