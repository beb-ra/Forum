using System.Xml.Linq;
using ForumWebAPI.Data.Repositories;
using ForumWebAPI.Extensions;
using ForumWebAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using static ForumWebAPI.Controllers.CommentsController;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ModerationController : ControllerBase
    {
        private readonly ISoftDeleteRepository<Post> _postRepository;
        private readonly ISoftDeleteRepository<Comment> _commentRepository;
        private readonly IRepository<User> _userRepository;

        public ModerationController(ISoftDeleteRepository<Post> postRepository, 
            ISoftDeleteRepository<Comment> commentRepository,
            IRepository<User> userRepository)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _userRepository = userRepository;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var claims = User.FindAll("SubforumRole").Select(c => c.Value).ToList();
            bool isAdmin = claims.Contains("global:Admin");

            var moderatedSubforums = claims
                .Where(r => r.EndsWith(":Moderator"))
                .Select(r => r.Split(':')[0])
                .Where(idStr => int.TryParse(idStr, out _))
                .Select(int.Parse)
                .ToList();

            if (!isAdmin && moderatedSubforums.Count == 0)
            {
                return Ok(new { Posts = new List<Post>(), Comments = new List<CommentDto>() });
            }

            var postsQuery = _postRepository.Get()
                .Include(p => p.Images)
                .Where(p => p.ModerationStatus == ModerationStatus.Pending && !p.IsDeleted);

            var commentsQuery = _commentRepository.Get()
                .Where(c => c.ModerationStatus == ModerationStatus.Pending && !c.IsDeleted);

            if (!isAdmin)
            {
                postsQuery = postsQuery.Where(p => moderatedSubforums.Contains(p.SubforumId));
                commentsQuery = commentsQuery.Where(c => moderatedSubforums.Contains(c.Post.SubforumId));
            }

            var posts = await postsQuery.ToListAsync();

            var comments = await commentsQuery
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    PostId = c.PostId,
                    PostTitle = c.Post.Title,
                    AuthorId = c.AuthorId,
                    Content = c.Content,
                    CreationDate = c.CreationDate,
                    Rating = c.Rating,
                    IsDeleted = c.IsDeleted,
                    IsPinned = c.IsPinned,
                    ImageUrl = c.ImageUrl,
                    HasReplies = false
                })
                .ToListAsync();

            return Ok(new { Posts = posts, Comments = comments });
        }

        [HttpPut("posts/{id}/approve")]
        public IActionResult ApprovePost(int id)
        {
            var post = _postRepository.Get(id);
            if (post == null) return NotFound();

            if (!User.IsModeratorOf(post.SubforumId)) return Forbid();

            post.ModerationStatus = ModerationStatus.Approved;
            _postRepository.Update(id, post);
            return NoContent();
        }

        [HttpPut("posts/{id}/reject")]
        public IActionResult RejectPost(int id)
        {
            var post = _postRepository.Get(id);
            if (post == null) return NotFound();

            if (!User.IsModeratorOf(post.SubforumId)) return Forbid();

            post.ModerationStatus = ModerationStatus.Rejected;
            post.IsDeleted = true;
            _postRepository.Update(id, post);
            return NoContent();
        }

        [HttpPut("comments/{id}/approve")]
        public IActionResult ApproveComment(int id)
        {
            var comment = _commentRepository.Get(id);
            if (comment == null) return NotFound();
            var post = _postRepository.Get(comment.PostId);

            if (!User.IsModeratorOf(post.SubforumId)) return Forbid();

            comment.ModerationStatus = ModerationStatus.Approved;
            _commentRepository.Update(id, comment);
            return NoContent();
        }

        [HttpPut("comments/{id}/reject")]
        public IActionResult RejectComment(int id)
        {
            var comment = _commentRepository.Get(id);
            if (comment == null) return NotFound();
            var post = _postRepository.Get(comment.PostId);

            if (!User.IsModeratorOf(post.SubforumId)) return Forbid();

            comment.ModerationStatus = ModerationStatus.Rejected;
            comment.IsDeleted = true;
            _commentRepository.Update(id, comment);
            return NoContent();
        }

        [HttpGet("users")]
        public IActionResult GetModeratedUsers()
        {
            var claims = User.FindAll("SubforumRole").Select(c => c.Value).ToList();
            bool isAdmin = claims.Contains("global:Admin");

            var moderatedSubforums = claims
                .Where(r => r.EndsWith(":Moderator"))
                .Select(r => r.Split(':')[0])
                .Where(idStr => int.TryParse(idStr, out _))
                .Select(int.Parse)
                .ToList();

            if (!isAdmin && moderatedSubforums.Count == 0)
                return Ok(new List<object>());

            var postUserIds = _postRepository.Get()
                .Where(p => (isAdmin || moderatedSubforums.Contains(p.SubforumId)) && !p.IsDeleted)
                .Select(p => p.AuthorId);

            var commentUserIds = _commentRepository.Get()
                .Where(c => (isAdmin || moderatedSubforums.Contains(c.Post.SubforumId)) && !c.IsDeleted)
                .Select(c => c.AuthorId);

            var allUserIds = postUserIds.Union(commentUserIds).Distinct().ToList();
            
            var users = _userRepository.Get()
                .Where(u => allUserIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Username })
                .ToList();

            return Ok(users);
        }
    }
}
