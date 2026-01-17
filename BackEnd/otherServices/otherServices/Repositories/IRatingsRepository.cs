using otherServices.Models;

namespace otherServices.Repositories
{
    public interface IRatingsRepository: IGenericRepository<Rating>
    {
        Task<decimal> GetUserAverageRatingAsync(long ownerId);

    }
}
