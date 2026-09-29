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

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubforumsController : ControllerBase
    {
        private readonly ISoftDeleteRepository<Subforum> _subforumRepository;

        public SubforumsController(ISoftDeleteRepository<Subforum> subforumRepository)
        {
            _subforumRepository = subforumRepository;
        }

        [HttpPost]
        public IActionResult Create([FromBody] Subforum subforum)
        {
            if (subforum == null)
            {
                return BadRequest();
            }
            if (!User.IsGlobalAdmin()) return Forbid();

            if (string.IsNullOrEmpty(subforum.Name?.Trim()))
            {
                return BadRequest("Name is required");
            }

            var existingSubforum = _subforumRepository.Get()
                .FirstOrDefault(s => s.Name.ToLower() == subforum.Name.Trim().ToLower());

            if (existingSubforum != null)
            {
                return Conflict("A subforum with this name already exists");
            }

            subforum.Name = subforum.Name.Trim();
            subforum.IsDeleted = false;

            _subforumRepository.Create(subforum);
            return CreatedAtRoute("GetSubforum", new { id = subforum.SubforumId }, subforum);
        }


        [HttpGet(Name = "GetAllSubforums")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<Subforum>> Get()
        {
            var subforum = _subforumRepository.Get();
            return Ok(subforum);
        }

        [HttpGet("{id}", Name = "GetSubforum")]
        [AllowAnonymous]
        public IActionResult Get(int Id)
        {
            Subforum? subforums = _subforumRepository.Get(Id);

            if (subforums == null)
            {
                return NotFound();
            }

            return Ok(subforums);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int Id, [FromBody] Subforum updatedSubforum)
        {
            if (updatedSubforum == null)
            {
                return BadRequest();
            }

            var subforum = _subforumRepository.Get(Id);
            if (subforum == null)
            {
                return NotFound();
            }

            if (!User.IsModeratorOf(Id)) return Forbid();

            if (!string.IsNullOrEmpty(updatedSubforum.Name?.Trim())) { subforum.Name = updatedSubforum.Name.Trim(); }
            subforum.Description = updatedSubforum.Description;

            _subforumRepository.Update(Id, subforum);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            if (!User.IsGlobalAdmin()) return Forbid();

            var deletedSubforum = _subforumRepository.Delete(Id);

            if (deletedSubforum == null)
            {
                return NotFound();
            }

            return Ok(deletedSubforum);
        }
    }
}
