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
using Azure;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly IHardDeleteRepository<Role> _roleRepository;

        public RolesController(IHardDeleteRepository<Role> roleRepository)
        {
            _roleRepository = roleRepository;
        }

        [HttpPost]
        public IActionResult Create([FromBody] Role role)
        {
            if (role == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(role.RoleName?.Trim()))
            {
                return BadRequest();
            }

            role.RoleName = role.RoleName.Trim();

            var existingRole = _roleRepository.Get()
                .FirstOrDefault(r => r.RoleName.ToLower() == role.RoleName.ToLower());
            if (existingRole != null)
            {
                return Conflict("A role with this name already exists.");
            }

            _roleRepository.Create(role);
            return CreatedAtRoute("GetRole", new { id = role.RoleId }, role);
        }

        [HttpGet(Name = "GetAllRoles")]
        public ActionResult<IEnumerable<Role>> Get()
        {
            var roles = _roleRepository.Get();
            return Ok(roles);
        }

        [HttpGet("{id}", Name = "GetRole")]
        public IActionResult Get(int Id)
        {
            Role? role = _roleRepository.Get(Id);

            if (role == null)
            {
                return NotFound();
            }

            return Ok(role);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int Id, [FromBody] Role updatedRole)
        {
            if (updatedRole == null)
            {
                return BadRequest();
            }

            var role = _roleRepository.Get(Id);
            if (role == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updatedRole.RoleName?.Trim())) { role.RoleName = updatedRole.RoleName.Trim(); }

            _roleRepository.Update(Id, role);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            var deletedRole = _roleRepository.Delete(Id);

            if (deletedRole == null)
            {
                return NotFound();
            }

            return Ok(deletedRole);
        }
    }
}
