using Microsoft.Extensions.Caching.Memory;
using otherServices.Models;
using otherServices.Repositories;
using System.Diagnostics;
using otherServices.Services.Interfaces.Tenants;

namespace otherServices.Services.Tenants
{
    public class LikeService : ILikeService
    {
        private readonly ILikeRepository likeRepository;
        private readonly IUnitOfWork unitOfWork;
        private readonly IMemoryCache cache;
        private readonly ILogger<LikeService> logger;

        public LikeService(
            ILikeRepository likeRepository,
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            ILogger<LikeService> logger)
        {
            this.likeRepository = likeRepository;
            this.unitOfWork = unitOfWork;
            this.cache = cache;
            this.logger = logger;
        }

        public async Task LikePostAsync(int userId, int postId)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                logger.LogInformation("User {UserId} is attempting to like Post {PostId}", userId, postId);

                var user = await unitOfWork.Users.GetByIdAsync(userId)
                    ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

                var post = await unitOfWork.Posts.GetByIdAsync(postId)
                    ?? throw new KeyNotFoundException($"Post with ID {postId} not found.");

                bool alreadyLiked = await likeRepository.ExistsAsync(userId, postId);
                if (alreadyLiked)
                    throw new InvalidOperationException("User has already liked this post.");

                var like = new Like { UserId = userId, PostId = postId };
                await likeRepository.AddAsync(like);
                await unitOfWork.CompleteAsync();

                cache.Remove($"post_like_count_{postId}");

                logger.LogInformation("User {UserId} successfully liked Post {PostId}", userId, postId);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Like operation failed - Not Found");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Like operation failed - Conflict");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while liking post {PostId}", postId);
                throw;
            }
            finally
            {
                sw.Stop();
                logger.LogInformation("LikePostAsync executed in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            }
        }

        // Remove like
        public async Task RemoveLikeAsync(int userId, int postId)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                logger.LogInformation("User {UserId} is attempting to remove like from Post {PostId}", userId, postId);

                var like = await likeRepository.GetByIdAsync(userId, postId);
                if (like == null)
                    throw new KeyNotFoundException("Like not found.");

                likeRepository.Remove(like);
                await unitOfWork.CompleteAsync();

                cache.Remove($"post_like_count_{postId}");

                logger.LogInformation("Like removed successfully for Post {PostId} by User {UserId}", postId, userId);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Remove like failed - Not Found");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while removing like from post {PostId}", postId);
                throw;
            }
            finally
            {
                sw.Stop();
                logger.LogInformation("RemoveLikeAsync executed in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            }
        }

        // Get like count (with caching)
        public async Task<int> GetLikeCountByPostIdAsync(int postId)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                string cacheKey = $"post_like_count_{postId}";

                if (cache.TryGetValue(cacheKey, out int cachedCount))
                {
                    logger.LogInformation("Cache hit for Post {PostId} - LikeCount {Count}", postId, cachedCount);
                    return cachedCount;
                }

                logger.LogInformation("Cache miss for Post {PostId}. Fetching from database...", postId);
                var post = await unitOfWork.Posts.GetByIdAsync(postId)
                    ?? throw new KeyNotFoundException($"Post with ID {postId} not found.");

                int count = await likeRepository.CountAsync(l => l.PostId == postId);

                cache.Set(cacheKey, count, TimeSpan.FromMinutes(5));
                logger.LogInformation("Cached like count for Post {PostId} = {Count}", postId, count);

                return count;
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Post not found while getting like count");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while getting like count for post {PostId}", postId);
                throw;
            }
            finally
            {
                sw.Stop();
                logger.LogInformation("GetLikeCountByPostIdAsync executed in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            }
        }
    }

}
