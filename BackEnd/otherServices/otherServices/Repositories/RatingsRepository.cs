using Microsoft.EntityFrameworkCore;

using otherServices.Models;

namespace otherServices.Repositories
{
    public class RatingsRepository : GenericRepository<Rating>, IRatingsRepository
    {
        private readonly AppDbContext2 _context;

        public RatingsRepository(AppDbContext2 context) : base(context)
        {
            _context = context;
        }

        public async Task<decimal> GetUserAverageRatingAsync(long landlordId)
        {
            var avg = await _context.Ratings
                .Where(r => r.LandlordId == landlordId)
                .AverageAsync(r => (decimal?)r.Score);

            return avg ?? 0;   // لو مفيش تقييم يرجع 0
        }

        //public async Task<IEnumerable<UserAverageRatingDto>> GetAllUsersAverageRatingAsync()
        //{
        //    var query = await _context.Ratings
        //        .GroupBy(r => r.OwnerId)
        //        .Select(g => new UserAverageRatingDto
        //        {
        //            OwnerId = g.Key,
        //            AverageRating = g.Average(r => r.Score),
        //            RatingsCount = g.Count()
        //        })
        //        .ToListAsync();

        //    return query;
        //}

    }

}
