using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using otherServices.Repositories;
using otherServices.Services.Interfaces;
using otherServices.Models.Enums;

namespace otherServices.Services.Admins
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private const string CacheKey = "dashboard_user_stats";

        public DashboardService(IUnitOfWork uow, IMemoryCache cache, ILogger<DashboardService> logger)
        {
            _uow = uow;
            _cache = cache;
            _logger = logger;
        }

        public async Task<object> GetUserStatisticsAsync()
        {
            if (_cache.TryGetValue(CacheKey, out object cachedStats))
            {
                _logger.LogInformation("📦 Dashboard stats retrieved from cache at {Time}", DateTime.UtcNow);
                return cachedStats;
            }

            var sw = Stopwatch.StartNew();
            _logger.LogInformation("⏳ Calculating dashboard statistics...");

            var totalUsers = await _uow.Users.CountAsync();
            var owners = await _uow.Users.CountAsync(u => u.RoleName == UserRole.Landlord);
            var tenants = await _uow.Users.CountAsync(u => u.RoleName == UserRole.Tenant);
            var admins = await _uow.Admins.CountAsync(); // جدول منفصل

            var stats = new
            {
                TotalUsers = totalUsers,
                Owners = owners,
                Tenants = tenants,
                Admins = admins
            };

            _cache.Set(CacheKey, stats, TimeSpan.FromMinutes(2)); 

            sw.Stop();
            _logger.LogInformation("✅ Dashboard stats calculated in {Elapsed} ms", sw.ElapsedMilliseconds);

            return stats;
        }
        public void InvalidateDashboardCache()
        {
            _cache.Remove(CacheKey);
            _logger.LogInformation("🧹 Dashboard cache invalidated at {Time}", DateTime.UtcNow);
        }
    }
}
