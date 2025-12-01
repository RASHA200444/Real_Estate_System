using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommentAPI.DTOs;
using otherServices.Models;
using otherServices.Services;

namespace CommentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext2 _context;
        private readonly ICommentService _commentService;

        public CommentsController(AppDbContext2 context, ICommentService commentService)
        {
            _context = context;
            _commentService = commentService;
        }

        //[Authorize]
        [HttpGet("Post/get-comments/{postId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByPost(long postId)
        {
            var comments = await _commentService.GetCommentsByPostAsync(postId);

            if (comments == null || !comments.Any())
            {
                return NotFound("No comments found for this post");
            }

            return Ok(comments);
        }

        //[Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<CommentDto>> GetComment(long id)
        {
            var commentDto = await _commentService.GetCommentByIdAsync(id);

            if (commentDto == null)
            {
                return NotFound("Comment not found or has no associated user");
            }

            return Ok(commentDto);
        }


        // POST: api/Comments
        [HttpPost("{userId}/add-comment/{postId}")]
        public async Task<ActionResult<CommentDto>> CreateComment(CreateCommentDto createCommentDto, long userId, long postId)
        {
            var commentDto = await _commentService.CreateCommentAsync(createCommentDto, userId, postId);

            if (commentDto == null)
                return BadRequest("Unable to create comment (post or user not found)");

            return CreatedAtAction(nameof(GetComment), new { id = commentDto.Comment_Id }, commentDto);
        }



        [HttpPut("{userId}/edit-comment/{commentId}")]
        //[Authorize]
        public async Task<IActionResult> UpdateComment(long commentId, long userId, [FromBody] UpdateCommentDto updateCommentDto)
        {
            var updatedCommentDto = await _commentService.UpdateCommentAsync(commentId, updateCommentDto, userId);

            if (updatedCommentDto == null)
                return BadRequest("Unable to update comment (comment not found or user not authorized)");

            return Ok(updatedCommentDto);
        }


        [HttpDelete("{UserId}/delete-comment/{commentId}")]
        //[Authorize]
        public async Task<IActionResult> DeleteComment(long UserId, long commentId)
        {
            var result = await _commentService.DeleteCommentAsync(commentId, UserId);

            if (!result)
                return BadRequest("Unable to delete comment (comment not found or user not authorized)");

            return Ok("Comment deleted successfully");
        }


    }
}