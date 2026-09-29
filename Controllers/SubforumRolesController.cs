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
using Microsoft.IdentityModel.Tokens;
using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using ForumWebAPI.Extensions;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubforumRolesController : ControllerBase
    {
        private readonly IHardDeleteRepository<SubforumRole> _subforumroleRepository;
        private readonly IRepository<Role> _roleRepository;

        public SubforumRolesController(IHardDeleteRepository<SubforumRole> subforumroleRepository,
            IRepository<Role> roleRepository)
        {
            _subforumroleRepository = subforumroleRepository;
            _roleRepository = roleRepository;
        }

        [HttpPost]
        public IActionResult Create([FromBody] SubforumRole role)
        {
            if (!User.IsGlobalAdmin()) return Forbid();
            if (role == null)
            {
                return BadRequest();
            }

            var existing = _subforumroleRepository.Get()
                .FirstOrDefault(r =>
                r.UserId == role.UserId &&
                r.SubforumId == role.SubforumId &&
                r.RoleId == role.RoleId);

            if (existing != null)
            {
                return Conflict("This user already has this role in this subforum");
            }

            var roleEntity = _roleRepository.Get(role.RoleId);
            if (roleEntity == null) return NotFound("Role not found.");

            if (roleEntity.RoleName == "Admin" && role.SubforumId != null)
                return BadRequest("Admin role must be global (subforumId = null)");

            if (roleEntity.RoleName == "Moderator" && role.SubforumId == null)
                return BadRequest("Moderator role must be assigned to a specific subforum");

            role.AssignedAt = DateTime.UtcNow;

            try
            {
                _subforumroleRepository.Create(role);
            }
            catch (DbUpdateException)
            {
                return BadRequest("The role is uncorrect");
            }
            return CreatedAtRoute("GetSubforumRole", new { id = role.SubfRoleId }, role);
        }

        [HttpGet(Name = "GetAllSubforumRoles")]
        public ActionResult<IEnumerable<SubforumRole>> Get()
        {
            if (!User.IsGlobalAdmin()) return Forbid();
            var roles = _subforumroleRepository.Get();
            return Ok(roles);
        }

        [HttpGet("{id}", Name = "GetSubforumRole")]
        public IActionResult Get(int Id)
        {
            if (!User.IsGlobalAdmin()) return Forbid();
            SubforumRole? role = _subforumroleRepository.Get(Id);

            if (role == null)
            {
                return NotFound();
            }

            return Ok(role);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int Id, [FromBody] SubforumRole updatedRole)
        {
            if (!User.IsGlobalAdmin()) return Forbid();

            if (updatedRole == null)
            {
                return BadRequest();
            }

            var role = _subforumroleRepository.Get(Id);
            if (role == null)
            {
                return NotFound();
            }

            role.RoleId = updatedRole.RoleId;
            role.SubforumId = updatedRole.SubforumId;
            role.AssignedAt = DateTime.UtcNow;

            try
            {
                _subforumroleRepository.Update(Id, role);
            }
            catch (DbUpdateException)
            {
                return BadRequest("The role is uncorrect");
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            if (!User.IsGlobalAdmin()) return Forbid();

            var deletedRole = _subforumroleRepository.Delete(Id);

            if (deletedRole == null)
            {
                return NotFound();
            }

            return Ok(deletedRole);
        }
    }
}
