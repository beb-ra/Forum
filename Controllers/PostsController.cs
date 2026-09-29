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

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly ISoftDeleteRepository<Post> _postRepository;
        private readonly IBanService _banService;

        public PostsController(ISoftDeleteRepository<Post> postRepository, IBanService banService)
        {
            _postRepository = postRepository;
            _banService = banService;
        }

        [HttpPost]
        public IActionResult Create([FromBody] Post post)
        {
            if (post == null)
            {
                return BadRequest();
            }

            if (_banService.IsBanned(User.GetUserId(), post.SubforumId))
            {
                return StatusCode(403, "You are banned in this subforum.");
            }

            if (string.IsNullOrEmpty(post.Title?.Trim()) || string.IsNullOrEmpty(post.Content?.Trim()))
            {
                return BadRequest("Title and Content are required");
            }

            post.AuthorId = User.GetUserId();

            post.Title = post.Title.Trim();
            post.Content = post.Content.Trim();

            post.CreationDate = DateTime.UtcNow;
            post.Rating = 0;
            post.IsPinned = false;
            post.IsClosed = false;
            post.IsDeleted = false;

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
        public ActionResult<IEnumerable<Post>> Get()
        {
            var posts = _postRepository.Get();
            return Ok(posts);
        }

        [HttpGet("{id}", Name = "GetPost")]
        [AllowAnonymous]
        public IActionResult Get(int Id)
        {
            Post? post = _postRepository.Get(Id);

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
