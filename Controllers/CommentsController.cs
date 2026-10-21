using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Forum.Data;
using ForumWebAPI.Model;
using ForumWebAPI.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using ForumWebAPI.Extensions;
using ForumWebAPI.Services;
using Microsoft.Extensions.Hosting;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ISoftDeleteRepository<Comment> _commentRepository;
        private readonly IRepository<Post> _postRepository;
        private readonly IBanService _banService;

        public CommentsController(
            ISoftDeleteRepository<Comment> commentRepository,
            IRepository<Post> postRepository,
            IBanService banService)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _banService = banService;
        }

        [HttpPost]
        public IActionResult Create([FromBody] Comment comment)
        {
            if (comment == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(comment.Content?.Trim()))
            {
                return BadRequest("Content is required");
            }

            var post = _postRepository.Get(comment.PostId);
            if (post == null) return NotFound();

            if (_banService.IsBanned(User.GetUserId(), post.SubforumId))
            {
                return StatusCode(403, "You are banned in this subforum");
            }

            comment.AuthorId = User.GetUserId();
            comment.Content = comment.Content.Trim();

            comment.CreationDate = DateTime.UtcNow;
            comment.Rating = 0;
            comment.IsDeleted = false;
            comment.IsPinned = false;
            comment.ImageUrl = comment.ImageUrl?.Trim();

            try
            {
                _commentRepository.Create(comment);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Invalid PostId, AuthorId or ParentCommentId");
            }

            return CreatedAtRoute("GetComment", new { id = comment.CommentId }, comment);
        }

        [HttpGet(Name = "GetAllComments")]
        [AllowAnonymous]
        public ActionResult<PageResult<CommentDto>> Get(int postId, int? parentCommentId = null, int page = 1, int pageSize = 10)
        {
            var allCommentsForPost = _commentRepository.Get()
                .Where(c => c.PostId == postId)
                .ToList();

            var repliesStats = allCommentsForPost
                .Where(c => c.ParentCommentId != null && !c.IsDeleted)
                .GroupBy(c => c.ParentCommentId!.Value)
                .ToDictionary(g => g.Key, g => new { Count = g.Count(), TotalRating = g.Sum(c => c.Rating) });

            double GetScore(Comment c)
            {
                var stats = repliesStats.GetValueOrDefault(c.CommentId);
                return Comment.CalculateScore(
                    c.Rating,
                    stats?.Count ?? 0,
                    stats?.TotalRating ?? 0,
                    c.CreationDate, c.IsDeleted, c.IsPinned);
            }

            var result = allCommentsForPost
                .Where(c => c.ParentCommentId == parentCommentId)
                .Where(c => !c.IsDeleted || repliesStats.ContainsKey(c.CommentId))
                .OrderByDescending(GetScore)
                .ToList();

            var pageItems = parentCommentId.HasValue
                ? result
                : result.Skip((page - 1) * pageSize).Take(pageSize);

            CommentDto Map(Comment c)
            {
                return new CommentDto
                {
                    CommentId = c.CommentId,
                    PostId = c.PostId,
                    AuthorId = c.AuthorId,
                    Content = c.Content,
                    CreationDate = c.CreationDate,
                    Rating = c.Rating,
                    IsDeleted = c.IsDeleted,
                    IsPinned = c.IsPinned,
                    HasReplies = repliesStats.ContainsKey(c.CommentId),
                    ImageUrl = c.ImageUrl
                };
            }

            return Ok(new PageResult<CommentDto>
            {
                Items = pageItems.Select(Map).ToList(),
                TotalCount = result.Count
            });
        }

        [HttpGet("{id}", Name = "GetComment")]
        [AllowAnonymous]
        public IActionResult Get(int Id)
        {
            Comment? comment = _commentRepository.Get(Id);

            if (comment == null)
            {
                return NotFound();
            }

            return Ok(comment);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int Id, [FromBody] Comment updatedComment)
        {
            if (updatedComment == null)
            {
                return BadRequest();
            }

            var comment = _commentRepository.Get(Id);
            if (comment == null)
            {
                return NotFound();
            }

            var post = _postRepository.Get(comment.PostId);
            if (post == null) return NotFound();

            if (_banService.IsBanned(User.GetUserId(), post.SubforumId))
            {
                return StatusCode(403, "You are banned in this subforum");
            }

            var isAuthor = comment.AuthorId == User.GetUserId();
            var isModerator = User.IsModeratorOf(post.SubforumId);

            if (!isAuthor && !isModerator) return Forbid();

            if (!string.IsNullOrEmpty(updatedComment.Content?.Trim())) { comment.Content = updatedComment.Content.Trim(); }
            comment.IsPinned = updatedComment.IsPinned;

            _commentRepository.Update(Id, comment);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            var comment = _commentRepository.Get(Id);
            if (comment == null) return NotFound();

            var post = _postRepository.Get(comment.PostId);
            if (post == null) return NotFound();

            if (_banService.IsBanned(User.GetUserId(), post.SubforumId))
            {
                return StatusCode(403, "You are banned in this subforum");
            }

            var currentUserId = User.GetUserId();
            if (comment.AuthorId != currentUserId && !User.IsModeratorOf(post.SubforumId))
                return Forbid();

            _commentRepository.Delete(Id);
            return NoContent();
        }

        public class CommentDto
        {
            public int CommentId { get; set; }
            public int PostId { get; set; }
            public int AuthorId { get; set; }
            public string Content { get; set; } = "";
            public DateTime CreationDate { get; set; }
            public int Rating { get; set; }
            public bool IsDeleted { get; set; }
            public bool IsPinned { get; set; }
            public bool HasReplies { get; set; }
            public string? ImageUrl { get; set; }
        }
    }
}
