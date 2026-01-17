using otherServices.Models;

namespace otherServices.Repositories
{
    public interface IPostRepository: IGenericRepository<Post>
    {
        Task<Post> AcceptPostAsync(long postId);
        Task<Post> RejectPostAsync(long postId);
    }
}
