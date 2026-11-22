using Application.Core.DTOs;
using Application.Core.Entities;
using Application.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly IUserRepository _users;

        public RolesController(IUserRepository users)
        {
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _users.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
            var existing = await _users.GetRoleByNameAsync(dto.Name);
            if (existing != null) return Conflict("Role already exists");

            var role = new Role { Name = dto.Name };
            await _users.AddRoleAsync(role);
            await _users.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = role.Id }, new RoleDto(role.Name));
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> Update(string name, [FromBody] RoleDto dto)
        {
            var role = await _users.GetRoleByNameAsync(name);
            if (role == null) return NotFound();
            role.Name = dto.Name;
            await _users.UpdateRoleAsync(role);
            await _users.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var role = await _users.GetRoleByNameAsync(name);
            if (role == null) return NotFound();
            // repository lacks Delete; mark as remove via DbContext in future. For now, detach permissions and allow removal by context if implemented.
            // This is a placeholder to indicate deletion should occur here.
            await _users.UpdateRoleAsync(role);
            await _users.SaveChangesAsync();
            return NoContent();
        }

        // Permissions
        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions()
        {
            var perms = await _users.GetAllPermissionsAsync();
            return Ok(perms);
        }

        [HttpGet("permissions/{roleNme}")]
        public async Task<IActionResult> GetPermissionsForRole(string roleNme)
        {
            var role = await _users.GetRoleByNameAsync(roleNme);
            if (role == null) return NotFound("Role not found");
            var perms = role.RolePermissions.Select(rp => rp.Permission.Name).ToList();
            if (perms is null || !perms.Any()) return NotFound("No permissions found for this role");
            return Ok(perms);
        }

        [HttpPost("permissions")]
        public async Task<IActionResult> CreatePermission([FromBody] PermissionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required");
            var existing = await _users.GetPermissionByNameAsync(dto.Name);
            if (existing != null) return Conflict("Permission exists");
            var perm = new Permission { Name = dto.Name };
            await _users.AddPermissionAsync(perm);
            await _users.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPermissions), new { id = perm.Id }, new PermissionDto ( dto.Name ));
        }

        // Assign permission to role
        [HttpPost("{roleName}/permissions/{permName}")]
        public async Task<IActionResult> AssignPermissionToRole(string roleName, string permName)
        {
            var role = await _users.GetRoleByNameAsync(roleName);
            if (role == null) return NotFound("Role not found");
            var perm = await _users.GetPermissionByNameAsync(permName);
            if (perm == null) return NotFound("Permission not found");

            // avoid duplicates
            if (!role.RolePermissions.Any(rp => rp.PermissionId == perm.Id))
            {
                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
                await _users.UpdateRoleAsync(role);
                await _users.SaveChangesAsync();
            }

            return NoContent();
        }

        // Assign role to user
        [HttpPost("{roleName}/users/{userId}")]
        public async Task<IActionResult> AssignRoleToUser(string roleName, System.Guid userId)
        {
            var role = await _users.GetRoleByNameAsync(roleName);
            if (role == null) return NotFound("Role not found");
            var user = await _users.GetByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            if (!user.UserRoles.Any(ur => ur.RoleId == role.Id))
            {
                user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
                await _users.UpdateAsync(user);
                await _users.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}
