using otherServices.Models;

namespace otherServices.Repositories
{
    public interface ILikeRepository:IGenericRepository<Like>
    {
        Task<Like> GetByIdAsync(long userId, long postId);
        Task<bool> ExistsAsync(long userId, long postId);
    }
}
