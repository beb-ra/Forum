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
using System.Data;
using Microsoft.AspNetCore.Authorization;
using ForumWebAPI.Extensions;
using ForumWebAPI.Services;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VotesController : ControllerBase
    {
        private readonly IHardDeleteRepository<Vote> _voteRepository;
        private readonly IRepository<Post> _postRepository;
        private readonly IRepository<Comment> _commentRepository;
        private readonly IBanService _banService;

        public VotesController(
            IHardDeleteRepository<Vote> voteRepository,
            IRepository<Post> postRepository,
            IRepository<Comment> commentRepository,
            IBanService banService)
        {
            _voteRepository = voteRepository;
            _postRepository = postRepository;
            _commentRepository = commentRepository;
            _banService = banService;
        }

        [HttpPost]
        public IActionResult Create([FromBody] Vote vote)
        {
            if (vote == null)
            {
                return BadRequest();
            }

            if (vote.VoteType != 1 && vote.VoteType != -1)
                return BadRequest("VoteType must be 1 or -1");

            if (vote.PostId == null && vote.CommentId == null)
                return BadRequest("Vote must have only one target (post or comment)");
            if (vote.PostId != null && vote.CommentId != null)
                return BadRequest("Vote must have only one target (post or comment)");

            int? subforumId = null;

            if (vote.PostId != null)
            {
                var post = _postRepository.Get(vote.PostId.Value);
                if (post == null || post.IsDeleted)
                    return NotFound($"Post with id {vote.PostId} not found");
                subforumId = post.SubforumId;
            }
            else if (vote.CommentId != null)
            {
                var comment = _commentRepository.Get(vote.CommentId.Value);
                if (comment == null || comment.IsDeleted)
                    return NotFound($"Comment with id {vote.CommentId} not found");

                var post = _postRepository.Get(comment.PostId);
                if (post == null) return NotFound("Post for comment not found");
                subforumId = post.SubforumId;
            }

            if (_banService.IsBanned(User.GetUserId(), subforumId))
            {
                return StatusCode(403, "You are banned in this subforum");
            }

            vote.UserId = User.GetUserId();
            vote.VoteDate = DateTime.UtcNow;

            var existingVote = _voteRepository.Get().FirstOrDefault(v =>
                    v.UserId == vote.UserId &&
                    v.PostId == vote.PostId &&
                    v.CommentId == vote.CommentId);

            if (existingVote != null)
            {
                existingVote.VoteType = vote.VoteType;
                _voteRepository.Update(existingVote.VoteId, existingVote);
                RecountRating(vote);
                return Ok(existingVote);
            }

            try
            {
                _voteRepository.Create(vote);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Failed to create vote: target no longer exists.");
            }

            RecountRating(vote);
            return CreatedAtRoute("GetVote", new { id = vote.VoteId }, vote);
        }

        [HttpGet(Name = "GetAllVotes")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<Vote>> Get()
        {
            var vote = _voteRepository.Get();
            return Ok(vote);
        }

        [HttpGet("{id}", Name = "GetVote")]
        [AllowAnonymous]
        public IActionResult Get(int Id)
        {
            Vote? vote = _voteRepository.Get(Id);

            if (vote == null)
            {
                return NotFound();
            }

            return Ok(vote);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int Id, [FromBody] Vote updatedVote)
        {
            if (updatedVote == null)
            {
                return BadRequest();
            }

            var vote = _voteRepository.Get(Id);
            if (vote == null)
            {
                return NotFound();
            }

            if (updatedVote.VoteType != 1 && updatedVote.VoteType != -1)
            {
                return BadRequest("VoteType must be 1 or -1");
            }

            if (vote.UserId != User.GetUserId()) return Forbid();

            int? subforumId = null;

            if (vote.PostId != null)
            {
                var post = _postRepository.Get(vote.PostId.Value);
                if (post == null || post.IsDeleted)
                    return NotFound($"Post with id {vote.PostId} not found");
                subforumId = post.SubforumId;
            }
            else if (vote.CommentId != null)
            {
                var comment = _commentRepository.Get(vote.CommentId.Value);
                if (comment == null || comment.IsDeleted)
                    return NotFound($"Comment with id {vote.CommentId} not found");

                var post = _postRepository.Get(comment.PostId);
                if (post == null) return NotFound("Post for comment not found");
                subforumId = post.SubforumId;
            }

            if (_banService.IsBanned(User.GetUserId(), subforumId))
            {
                return StatusCode(403, "You are banned in this subforum");
            }

            vote.VoteType = updatedVote.VoteType;
            vote.VoteDate = DateTime.UtcNow;

            try
            {
                _voteRepository.Update(Id, vote);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Failed to update vote: target no longer exists.");
            }
      
            RecountRating(vote);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            var vote = _voteRepository.Get(Id);
            if (vote == null) return NotFound();

            if (vote.UserId != User.GetUserId()) return Forbid();

            int? subforumId = null;

            if (vote.PostId != null)
            {
                var post = _postRepository.Get(vote.PostId.Value);
                if (post == null || post.IsDeleted)
                    return NotFound($"Post with id {vote.PostId} not found");
                subforumId = post.SubforumId;
            }
            else if (vote.CommentId != null)
            {
                var comment = _commentRepository.Get(vote.CommentId.Value);
                if (comment == null || comment.IsDeleted)
                    return NotFound($"Comment with id {vote.CommentId} not found");

                var post = _postRepository.Get(comment.PostId);
                if (post == null) return NotFound("Post for comment not found");
                subforumId = post.SubforumId;
            }

            if (_banService.IsBanned(User.GetUserId(), subforumId))
            {
                return StatusCode(403, "You are banned in this subforum");
            }

            var deleted = _voteRepository.Delete(Id);
            if (deleted == null) return NotFound();

            RecountRating(deleted);
            return Ok(deleted);
        }

        private void RecountRating(Vote vote)
        {
            if (vote.PostId != null)
            {
                var post = _postRepository.Get(vote.PostId.Value);
                if (post == null) return;

                post.Rating = _voteRepository.Get()
                    .Where(v => v.PostId == vote.PostId)
                    .Sum(v => v.VoteType);

                _postRepository.Update(post.PostId, post);
            }
            else if (vote.CommentId != null)
            {
                var comment = _commentRepository.Get(vote.CommentId.Value);
                if (comment == null) return;

                comment.Rating = _voteRepository.Get()
                    .Where(v => v.CommentId == vote.CommentId)
                    .Sum(v => v.VoteType);

                _commentRepository.Update(comment.CommentId, comment);
            }
        }
    }
}
