using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Core.Entidades;
using AuthService.Application.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthService.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public AuthController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: api/Auth/Users
        [HttpGet("Users")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
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
                    Permissions = role.Permissions.Select(permission => new PermissionDTO
                    {
                        Id = permission.Id,
                        PermissionName = permission.PermissionName
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
                .ThenInclude(r => r.Permissions)
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
                    Permissions = role.Permissions.Select(permission => new PermissionDTO
                    {
                        Id = permission.Id,
                        PermissionName = permission.PermissionName
                    }).ToList()
                }).ToList()
            };

            return Ok(userDTO);
        }

        // POST: api/Auth/Users
        [HttpPost("Users")]
        public async Task<ActionResult<User>> PostUser([FromBody] UserDTO userDTO)
        {
            // Hash the password before saving it
            var hashedPassword = HashPassword(userDTO.PasswordHash);

            var user = new User
            {
                Username = userDTO.Username,
                Email = userDTO.Email,
                PasswordHash = hashedPassword, // Save the hashed password
                Roles = userDTO.Roles.Select(roleDTO => new Role
                {
                    RoleName = roleDTO.RoleName
                }).ToList()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // DELETE: api/Auth/Users/5
        [HttpDelete("Users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Auth/Users/5
        [HttpPut("Users/{id}")]
        public async Task<IActionResult> PutUser(int id, [FromBody] UserDTO userDTO)
        {
            if (id != userDTO.Id)
            {
                return BadRequest();
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            user.Username = userDTO.Username;
            user.Email = userDTO.Email;

            user.Roles.Clear(); // Clear existing roles
            user.Roles = userDTO.Roles.Select(roleDTO => new Role
            {
                Id = roleDTO.Id,
                RoleName = roleDTO.RoleName
            }).ToList();

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(u => u.Id == id);
        }

        // Utility function to hash passwords
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
