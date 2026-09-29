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

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly IHardDeleteRepository<Tag> _tagRepository;

        public TagsController(IHardDeleteRepository<Tag> tagRepository)
        {
            _tagRepository = tagRepository;
        }

        [HttpPost]
        public IActionResult Create([FromBody] Tag tag)
        {
            if (!User.IsGlobalAdmin()) return Forbid();

            if (tag == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(tag.Name?.Trim()))
            {
                return BadRequest();
            }

            tag.Name = tag.Name.Trim();

            var existingTag = _tagRepository.Get()
                .FirstOrDefault(t => t.Name.ToLower() == tag.Name.ToLower());
            if (existingTag != null)
            {
                return Conflict("A tag with this name already exists.");
            }

            _tagRepository.Create(tag);
            return CreatedAtRoute("GetTag", new { id = tag.TagId }, tag);
        }

        [HttpGet(Name = "GetAllTags")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<Tag>> Get()
        {
            var tags = _tagRepository.Get();
            return Ok(tags);
        }

        [HttpGet("{id}", Name = "GetTag")]
        [AllowAnonymous]
        public IActionResult Get(int Id)
        {
            Tag? tag = _tagRepository.Get(Id);

            if (tag == null)
            {
                return NotFound();
            }

            return Ok(tag);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int Id, [FromBody] Tag updatedTag)
        {
            if (!User.IsGlobalAdmin()) return Forbid();

            if (updatedTag == null)
            {
                return BadRequest();
            }

            var tag = _tagRepository.Get(Id);
            if (tag == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updatedTag.Name?.Trim())) { tag.Name = updatedTag.Name.Trim(); }

            _tagRepository.Update(Id, tag);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            if (!User.IsGlobalAdmin()) return Forbid();

            var deletedTag = _tagRepository.Delete(Id);

            if (deletedTag == null)
            {
                return NotFound();
            }

            return Ok(deletedTag);
        }
    }
}
