using CommentAPI.DTOs;

namespace otherServices.Services
{
    public interface ICommentService
    {
        Task<List<CommentDto>> GetCommentsByPostAsync(long postId);
        Task<CommentDto?> GetCommentByIdAsync(long id);
        Task<CommentDto?> CreateCommentAsync(CreateCommentDto createCommentDto, long userId, long postId);
        Task<CommentDto?> UpdateCommentAsync(long commentId, UpdateCommentDto updateCommentDto, long userId);
        Task<bool> DeleteCommentAsync(long commentId, long userId); 

    }
}
