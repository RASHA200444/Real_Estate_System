// ===============================
// File: Services/Interfaces/IDashboardService.cs
// ===============================
using System.Threading.Tasks;

namespace otherServices.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<object> GetDashboardAsync();
        void InvalidateDashboardCache();
    }
}