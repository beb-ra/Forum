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
using System.Data;
using ForumWebAPI.Extensions;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BansController : ControllerBase
    {
        private readonly IHardDeleteRepository<Ban> _banRepository;

        public BansController(IHardDeleteRepository<Ban> banRepository)
        {
            _banRepository = banRepository;
        }

        [HttpPost]
        public IActionResult Create([FromBody] Ban ban)
        {
            if (ban == null)
            {
                return BadRequest();
            }

            if (ban.SubforumId == null)
            {
                if (!User.IsGlobalAdmin()) return Forbid();
            }
            else
            {
                if (!User.IsModeratorOf(ban.SubforumId.Value)) return Forbid();
            }

            ban.BannedDate = DateTime.UtcNow;
            ban.BannedBy = User.GetUserId();

            if (ban.ExpiresDate.HasValue && ban.ExpiresDate.Value <= DateTime.UtcNow)
            {
                return BadRequest("ExpiresDate is uncorrect");
            }

            try
            {
                _banRepository.Create(ban);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Invalid UserId, SubforumId or BannedBy");
            }

            return CreatedAtRoute("GetBan", new { id = ban.BanId }, ban);
        }

        [HttpGet(Name = "GetAllBans")]
        public ActionResult<IEnumerable<Ban>> Get()
        {
            if (!User.IsGlobalAdmin()) return Forbid();
            var bans = _banRepository.Get();
            return Ok(bans);
        }

        [HttpGet("{id}", Name = "GetBan")]
        public IActionResult Get(int Id)
        {
            Ban? ban = _banRepository.Get(Id);

            if (ban == null)
            {
                return NotFound();
            }

            if (ban.SubforumId == null && !User.IsGlobalAdmin()) return Forbid();
            if (ban.SubforumId != null && !User.IsModeratorOf(ban.SubforumId.Value)) return Forbid();

            return Ok(ban);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int Id, [FromBody] Ban updatedBan)
        {
            if (updatedBan == null)
            {
                return BadRequest();
            }

            var ban = _banRepository.Get(Id);
            if (ban == null)
            {
                return NotFound();
            }

            if (ban.SubforumId == null)
            {
                if (!User.IsGlobalAdmin()) return Forbid();
            }
            else
            {
                if (!User.IsModeratorOf(ban.SubforumId.Value)) return Forbid();
            }

            ban.BanReason = updatedBan.BanReason?.Trim();
            ban.ExpiresDate = updatedBan.ExpiresDate;

            _banRepository.Update(Id, ban);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            var deletedBan = _banRepository.Delete(Id);

            if (deletedBan == null)
            {
                return NotFound();
            }

            if (deletedBan.SubforumId == null)
            {
                if (!User.IsGlobalAdmin()) return Forbid();
            }
            else
            {
                if (!User.IsModeratorOf(deletedBan.SubforumId.Value)) return Forbid();
            }


            return Ok(deletedBan);
        }
    }
}
