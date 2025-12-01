using CommentAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using otherServices.Repositories;
using otherServices.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;
    private IPostRepository _postRepository;
    public CommentService(ICommentRepository commentRepository, IUserRepository userRepository, IPostRepository postRepository)
    {
        _commentRepository = commentRepository;
        _userRepository = userRepository;
        _postRepository = postRepository;

    }

    public async Task<List<CommentDto>> GetCommentsByPostAsync(long postId)
    {
        var comments = await _commentRepository.NestedFind(
             c => c.PostId == postId,
             c => c.User
         );

        return comments
        .OrderByDescending(c => c.Description)
        .Select(c => new CommentDto
        {
            Comment_Id = c.CommentId,
            Comment_description = c.Description,
            date_comment = c.CreatedAt,
            User_name = c.User?.UserName,
            Post_Id = c.PostId
        })
        .ToList();
    }

    public async Task<CommentDto?> GetCommentByIdAsync(long id)
    {
        var comment = (await _commentRepository.NestedFind(
            c => c.CommentId == id,
            c => c.User
        )).FirstOrDefault();

        if (comment == null || comment.User == null)
        {
            return null;
        }

        return new CommentDto
        {
            Comment_Id = comment.CommentId,
            Comment_description = comment.Description,
            date_comment = comment.CreatedAt,
            User_name = comment.User.UserName,
            Post_Id = comment.PostId
        };
    }

    public async Task<CommentDto?> CreateCommentAsync(CreateCommentDto createCommentDto, long userId, long postId)
    {

        var user = await _userRepository.GetByIdAsync(userId);
        var post = await _postRepository.GetByIdAsync(postId);

        if (user == null || post == null)
            return null;

        var comment = new Comment
        {
            Description = createCommentDto.Comment_description,
            CreatedAt = DateTime.UtcNow,
            PostId = postId,
            UserId = user.UserId
        };

        await _commentRepository.AddAsync(comment);
        await _commentRepository.SaveChangesAsync();

        return new CommentDto
        {
            Comment_Id = comment.CommentId,
            Comment_description = comment.Description,
            date_comment = comment.CreatedAt,
            User_name = user.UserName,
            Post_Id = comment.PostId
        };
    }



    public async Task<CommentDto?> UpdateCommentAsync(long commentId, UpdateCommentDto updateCommentDto, long userId)
    {
        var comment = (await _commentRepository.NestedFind(
            c => c.CommentId == commentId,
            c => c.User
        )).FirstOrDefault();

        if (comment == null || comment.UserId != userId)
            return null;

        comment.Description = updateCommentDto.Comment_description;

        _commentRepository.Update(comment);

        try
        {
            await _commentRepository.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        return new CommentDto
        {
            Comment_Id = comment.CommentId,
            Comment_description = comment.Description,
            date_comment = comment.CreatedAt,
            User_name = comment.User?.UserName,
            Post_Id = comment.PostId
        };
    }


    public async Task<bool> DeleteCommentAsync(long commentId, long userId)
    {
        var comment = (await _commentRepository.FindAsync(c => c.CommentId == commentId && c.UserId == userId)).FirstOrDefault();

        if (comment == null)
            return false;

        _commentRepository.Remove(comment);
        await _commentRepository.SaveChangesAsync();
        return true;
    }

}
