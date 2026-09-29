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
        public ActionResult<IEnumerable<Comment>> Get()
        {
            var comments = _commentRepository.Get();
            return Ok(comments);
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
    }
}
