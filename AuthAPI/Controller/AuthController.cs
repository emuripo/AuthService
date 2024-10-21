using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Core.Entidades;
using AuthService.Application.DTO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Security.Claims;

namespace AuthService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AuthDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/Register
        [HttpPost("Register")]
        public async Task<ActionResult<User>> Register([FromBody] RegisterUserDTO userDTO)
        {
            // Hash the password before saving it
            var hashedPassword = HashPassword(userDTO.PasswordHash);

            // Buscar los roles en la base de datos según los IDs
            var roles = await _context.Roles
                .Where(r => userDTO.RoleIds.Contains(r.Id))
                .Include(r => r.RolePermissions) // Incluye los permisos del rol
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            var user = new User
            {
                Username = userDTO.Username,
                Email = userDTO.Email,
                PasswordHash = hashedPassword, // Save the hashed password
                Roles = roles,  // Asignar roles existentes
                IsActive = userDTO.IsActive
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Devolver solo los campos esenciales en la respuesta
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive
            });
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Username == loginDTO.Username);

            if (user == null || !VerifyPasswordHash(loginDTO.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid username or password.");
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Add roles and permissions as claims
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.RoleName)));
            claims.AddRange(user.Roles
                .SelectMany(role => role.RolePermissions.Select(rp => new Claim("Permission", rp.Permission.PermissionName))));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // GET: api/Auth/Users
        [HttpGet("Users")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            var userDTOs = users.Select(user => new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles.Select(role => new RoleDTO
                {
                    Id = role.Id,
                    RoleName = role.RoleName,
                    Permissions = role.RolePermissions.Select(rp => new PermissionDTO
                    {
                        Id = rp.Permission.Id,
                        PermissionName = rp.Permission.PermissionName,
                        Description = rp.Permission.Description // Mapea también la descripción
                    }).ToList()
                }).ToList()
            }).ToList();

            return Ok(userDTOs);
        }

        // GET: api/Auth/Users/5
        [HttpGet("Users/{id}")]
        public async Task<ActionResult<UserDTO>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var userDTO = new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles.Select(role => new RoleDTO
                {
                    Id = role.Id,
                    RoleName = role.RoleName,
                    Permissions = role.RolePermissions.Select(rp => new PermissionDTO
                    {
                        Id = rp.Permission.Id,
                        PermissionName = rp.Permission.PermissionName,
                        Description = rp.Permission.Description
                    }).ToList()
                }).ToList()
            };

            return Ok(userDTO);
        }

        // PUT: api/Auth/Users/5
        [HttpPut("Users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] CreateUpdateUserDTO updateUserDTO)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Actualizar campos del usuario
            user.Username = updateUserDTO.Username;
            user.Email = updateUserDTO.Email;

            // Actualizar los roles del usuario
            var roles = await _context.Roles
                .Where(r => updateUserDTO.RoleIds.Contains(r.Id))
                .ToListAsync();

            user.Roles = roles;

            // Guardar los cambios en la base de datos
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            using (var sha256 = SHA256.Create())
            {
                var computedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(computedHash) == storedHash;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
