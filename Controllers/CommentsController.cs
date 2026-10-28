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
        public async Task<ActionResult<PageResult<CommentDto>>> Get(int postId, int? parentCommentId = null, int page = 1, int pageSize = 10)
        {
            var epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var query = _commentRepository.Get()
                .Where(c => c.PostId == postId && c.ParentCommentId == parentCommentId);

            var projected = query.Select(c => new
            {
                c.CommentId, c.PostId, c.AuthorId,
                c.Content, c.CreationDate, c.Rating,
                c.IsDeleted, c.IsPinned, c.ImageUrl,
                ReplyCount = c.Replies.Count(r => !r.IsDeleted),
                TotalRepliesRating = c.Replies.Where(r => !r.IsDeleted).Sum(r => (int?)r.Rating) ?? 0
            })
            .Where(x => !x.IsDeleted || x.ReplyCount > 0)
            .OrderByDescending(x =>
                Math.Log10(Math.Max(Math.Abs(x.Rating), 1)) +
                ((x.CreationDate - epoch).TotalSeconds / 45000.0) +
                (Math.Log10(x.ReplyCount + 1) * 1.25) +
                (Math.Log10(Math.Max(Math.Abs(x.TotalRepliesRating), 1)) * 2) +
                (x.IsDeleted ? -5 : 0) +
                (x.IsPinned ? 100000 : 0)
            );

            var total = await projected.CountAsync();

            var pageItems = parentCommentId.HasValue
                ? await projected.ToListAsync()
                : await projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();

            var dtos = pageItems.Select(x => new CommentDto
            {
                CommentId = x.CommentId,
                PostId = x.PostId,
                AuthorId = x.AuthorId,
                Content = x.Content,
                CreationDate = x.CreationDate,
                Rating = x.Rating,
                IsDeleted = x.IsDeleted,
                IsPinned = x.IsPinned,
                HasReplies = x.ReplyCount > 0,
                ImageUrl = x.ImageUrl
            }).ToList();

            return Ok(new PageResult<CommentDto>
            {
                Items = dtos,
                TotalCount = total
            });
        }

        [HttpGet("author/{authorId}")]
        [AllowAnonymous]
        public async Task<ActionResult<PageResult<CommentDto>>> GetByAuthor(int authorId, int page = 1, int pageSize = 5)
        {
            var query = _commentRepository.Get()
                .Include(c => c.Post)
                .Where(c => !c.IsDeleted && !c.Post.IsDeleted && c.AuthorId == authorId);

            var total = await query.CountAsync();

            var comments = await query
                .OrderByDescending(c => c.CreationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
            }).ToListAsync();

            return Ok(new PageResult<CommentDto> { Items = comments, TotalCount = total });
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
            public string? PostTitle { get; set; }
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
