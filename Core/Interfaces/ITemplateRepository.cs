using AppsielPrintManager.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    public interface ITemplateRepository
    {
        Task<PrintTemplate> GetTemplateByTypeAsync(string documentType);
        Task SaveTemplateAsync(PrintTemplate template);
        Task<List<PrintTemplate>> GetAllTemplatesAsync();
        Task DeleteTemplateAsync(string templateId);
        Task EnsureDefaultTemplatesAsync();
    }
}
