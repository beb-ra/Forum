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
using System.ComponentModel.DataAnnotations;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ISoftDeleteRepository<User> _userRepository;

        public UsersController(ISoftDeleteRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        /*
        [HttpPost]
        public IActionResult Create([FromBody] User user)
        {
            if (!User.IsGlobalAdmin())
                return Forbid();
            if (user == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(user.Username?.Trim()) || string.IsNullOrEmpty(user.Email?.Trim()))
            {
                return BadRequest("Username and Email are required.");
            }

            var existingUser = _userRepository.Get()
                .FirstOrDefault(u => u.Email == user.Email.Trim());
            if (existingUser != null)
            {
                return Conflict("A user with this email already exists");
            }

            user.Username = user.Username.Trim();
            user.Email = user.Email.Trim();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash.Trim());

            _userRepository.Create(user);
            return CreatedAtRoute("GetUser", new { id = user.UserId }, user);
        }
        */

        [HttpGet(Name = "GetAllUsers")]
        public ActionResult<IEnumerable<User>> Get()
        {
            if (!User.IsGlobalAdmin()) return Forbid();
            var users = _userRepository.Get();
            return Ok(users);
        }

        [HttpGet("{id}", Name = "GetUser")]
        [AllowAnonymous]
        public IActionResult Get(int Id)
        {
            User? user = _userRepository.Get(Id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok( new { user.UserId, user.Username, user.RegistrationDate });
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            var user = _userRepository.Get(User.GetUserId());
            if (user == null || user.IsDeleted) return NotFound();

            return Ok(new { user.UserId, user.Username, user.Email,
                user.RegistrationDate });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int Id)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId != Id && !User.IsGlobalAdmin()) return Forbid();

            var deletedUser = _userRepository.Delete(Id);

            if (deletedUser == null)
            {
                return NotFound();
            }

            return Ok(deletedUser);
        }

        public class ChangeEmailRequest
        {
            [Required]
            public string CurrentPassword { get; set; } = "";

            [Required]
            [EmailAddress(ErrorMessage = "Uncorrect email")]
            public string NewEmail { get; set; } = "";
        }

        public class ChangePasswordRequest
        {
            [Required]
            public string CurrentPassword { get; set; } = "";

            [Required]
            public string NewPassword { get; set; } = "";
        }

        [HttpPut("me/email")]
        public IActionResult ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewEmail))
                return BadRequest("Email and current password are required");

            var userId = User.GetUserId();
            var user = _userRepository.Get(userId);
            if (user == null) return NotFound();

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
            if (!isPasswordValid)
                return BadRequest("Invalid password");

            var newEmail = request.NewEmail.Trim().ToLower();

            var existing = _userRepository.Get().FirstOrDefault(u => u.Email.ToLower() == newEmail);
            if (existing != null && existing.UserId != userId)
                return Conflict();

            user.Email = newEmail;
            _userRepository.Update(userId, user);

            return NoContent();
        }

        [HttpPut("me/password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Current and new passwords are required");

            var userId = User.GetUserId();
            var user = _userRepository.Get(userId);
            if (user == null) return NotFound();

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
            if (!isPasswordValid)
                return BadRequest("Invalid password");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _userRepository.Update(userId, user);

            return NoContent();
        }
    }
}
