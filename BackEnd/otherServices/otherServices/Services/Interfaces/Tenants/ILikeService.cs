namespace otherServices.Services.Interfaces.Tenants
{
    public interface ILikeService
    {
        Task LikePostAsync(int userId, int postId);
        Task RemoveLikeAsync(int userId, int postId);
        Task<int> GetLikeCountByPostIdAsync(int postId);
    }
}
