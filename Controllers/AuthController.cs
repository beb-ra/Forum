using ForumWebAPI.Data.Repositories;
using ForumWebAPI.Model;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ForumWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISoftDeleteRepository<User> _userRepository;
        private readonly IConfiguration _config;
        private readonly IRepository<SubforumRole> _subforumRoleRepository;
        private readonly IRepository<Role> _roleRepository;

        public AuthController(
            ISoftDeleteRepository<User> userRepository,
            IRepository<SubforumRole> subforumRoleRepository,
            IRepository<Role> roleRepository,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _subforumRoleRepository = subforumRoleRepository;
            _roleRepository = roleRepository;
            _config = config;
        }

        public class RegisterRequest
        {
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public class LoginRequest
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Username?.Trim()) ||
                string.IsNullOrEmpty(request.Email?.Trim()) ||
                string.IsNullOrEmpty(request.Password?.Trim()))
            {
                return BadRequest("Username, Email and Password are required");
            }

            var email = request.Email.Trim().ToLower();

            var exists = _userRepository.Get().Any(u => u.Email.ToLower() == email);
            if (exists)
            {
                return Conflict("Email is already in use");
            }

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RegistrationDate = DateTime.UtcNow,
                IsDeleted = false
            };

            _userRepository.Create(user);

            return Ok(new { user.UserId, user.Username, user.Email, Token = GenerateToken(user) });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email?.Trim()) || string.IsNullOrEmpty(request.Password?.Trim()))
            {
                return BadRequest("Email and Password are required");
            }

            var email = request.Email.Trim().ToLower();

            var user = _userRepository.Get()
                .FirstOrDefault(u => u.Email.ToLower() == email && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password");
            }

            return Ok(new {user.UserId, user.Username, user.Email, Token = GenerateToken(user)});
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(claims);
        }

        private string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var subforumRoles = _subforumRoleRepository.Get()
                .Where(sr => sr.UserId == user.UserId).ToList();

            var roleIds = subforumRoles.Select(sr => sr.RoleId).Distinct().ToList();

            var roles = _roleRepository.Get()
                .Where(r => roleIds.Contains(r.RoleId)).ToList();

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }

            var roleNameById = roles.ToDictionary(r => r.RoleId, r => r.RoleName);

            foreach (var sr in subforumRoles)
            {
                var roleName = roleNameById[sr.RoleId];

                var value = sr.SubforumId == null
                    ? $"global:{roleName}"
                    : $"{sr.SubforumId}:{roleName}";

                claims.Add(new Claim("SubforumRole", value));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
