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
using Azure.Core;
using System.ComponentModel.DataAnnotations;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly ISoftDeleteRepository<Post> _postRepository;
        private readonly IBanService _banService;
        private readonly IRepository<Comment> _commentRepository;
        private readonly IRepository<Subforum> _subforumRepository;

        public PostsController(ISoftDeleteRepository<Post> postRepository, 
            IBanService banService, IRepository<Comment> commentRepository, 
            IRepository<Subforum> subforumRepository)
        {
            _postRepository = postRepository;
            _banService = banService;
            _commentRepository = commentRepository;
            _subforumRepository = subforumRepository;
        }

        public class CreatePostRequest
        {
            [Required]
            public int SubforumId { get; set; }
            [Required]
            public string Title { get; set; } = string.Empty;
            [Required]
            public string Content { get; set; } = string.Empty;
            public List<string> ImageUrls { get; set; } = new();
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreatePostRequest request)
        {
            if (request == null)
            {
                return BadRequest();
            }

            if (_banService.IsBanned(User.GetUserId(), request.SubforumId))
            {
                return StatusCode(403, "You are banned in this subforum");
            }

            var subforum = _subforumRepository.Get(request.SubforumId);
            if (subforum == null) return NotFound("Subforum not found");

            var post = new Post
            {
                SubforumId = request.SubforumId,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                AuthorId = User.GetUserId(),
                CreationDate = DateTime.UtcNow,
                Rating = 0,
                IsPinned = false,
                IsClosed = false,
                IsDeleted = false
            };

            if (request.ImageUrls != null)
            {
                foreach (var url in request.ImageUrls)
                {
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        post.Images.Add(new PostImage { ImageUrl = url.Trim() });
                    }
                }
            }

            try
            {
                _postRepository.Create(post);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Invalid SubforumId, AuthorId or TagId.");
            }
           
            return CreatedAtRoute("GetPost", new { id = post.PostId }, post);
        }

        [HttpGet(Name = "GetAllPosts")]
        [AllowAnonymous]
        public async Task<ActionResult<PageResult<Post>>> Get(int page = 1, int pageSize = 10, int? subforumId = null)
        {
            var epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var query = _postRepository.Get()
                .Include(p => p.Images)
                .Where(p => !p.IsDeleted);

            if (subforumId.HasValue)
            {
                query = query.Where(p => p.SubforumId == subforumId.Value);
            }

            var total = await query.CountAsync();

            var result = await query
                .OrderByDescending(p =>
                    Math.Log10(Math.Max(Math.Abs(p.Rating), 1)) +
                    ((p.CreationDate - epoch).TotalSeconds / 45000.0) +
                    (Math.Log10(p.Comments.Count(c => !c.IsDeleted) + 1) * 1.2) +
                    (p.IsClosed ? -10 : 0) +
                    (p.IsPinned ? 100000 : 0)
                )
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PageResult<Post> { Items = result, TotalCount = total });
        }

        [HttpGet("author/{authorId}")]
        [AllowAnonymous]
        public async Task<ActionResult<PageResult<Post>>> GetByAuthor(int authorId, int page = 1, int pageSize = 5)
        {
            var query = _postRepository.Get()
                .Include(p => p.Images)
                .Where(p => !p.IsDeleted && p.AuthorId == authorId);

            var total = await query.CountAsync();

            var result = await query
                .OrderByDescending(p => p.CreationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PageResult<Post> { Items = result, TotalCount = total });
        }

        [HttpGet("{id}", Name = "GetPost")]
        [AllowAnonymous]
        public IActionResult Get(int Id)
        {
            Post? post = _postRepository.Get()
                .Include(p => p.Images)
                .FirstOrDefault(p => p.PostId == Id);

            if (post == null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int Id, [FromBody] Post updatedPost)
        {
            if (updatedPost == null)
            {
                return BadRequest();
            }

            var post = _postRepository.Get(Id);
            if (post == null)
            {
                return NotFound();
            }

            if (_banService.IsBanned(User.GetUserId(), post.SubforumId))
            {
                return StatusCode(403, "You are banned in this subforum.");
            }

            var currentUserId = User.GetUserId();
            var isAuthor = post.AuthorId == currentUserId;
            var isModerator = User.IsModeratorOf(post.SubforumId);

            if (!isAuthor && !isModerator)
                return Forbid();

            if (!string.IsNullOrEmpty(updatedPost.Title?.Trim())) { post.Title = updatedPost.Title.Trim(); }
            if (!string.IsNullOrEmpty(updatedPost.Content?.Trim())) { post.Content = updatedPost.Content.Trim(); }
            if (updatedPost.TagId != null) { post.TagId = updatedPost.TagId; }
            post.IsPinned = updatedPost.IsPinned;
            post.IsClosed = updatedPost.IsClosed;

            _postRepository.Update(Id, post);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            var post = _postRepository.Get(Id);
            if (post == null) return NotFound();

            if (_banService.IsBanned(User.GetUserId(), post.SubforumId))
            {
                return StatusCode(403, "You are banned in this subforum.");
            }

            var currentUserId = User.GetUserId();
            if (post.AuthorId != currentUserId && !User.IsModeratorOf(post.SubforumId))
                return Forbid();

            _postRepository.Delete(Id);
            return NoContent();
        }
    }
}
