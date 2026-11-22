using Application.Core.Entities;

namespace Application.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAll();
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUserNameAsync(string userName);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUserNameOrEmailAsync(string userNameOrEmail);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task SaveChangesAsync();
        Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken);

        // Password reset token support
        Task AddPasswordResetTokenAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token);
        Task UpdatePasswordResetTokenAsync(PasswordResetToken token);

        // Roles & permissions
        Task<Role?> GetRoleByNameAsync(string roleName);
        // get roles for user
        Task<List<string>> GetRolesForUserAsync(Guid userId);
        Task AddRoleAsync(Role role);
        Task UpdateRoleAsync(Role role);
        Task<List<string>> GetAllRolesAsync();

        Task<Permission?> GetPermissionByNameAsync(string permissionName);
        // get permissions for role
        Task<List<string>> GetPermissionsForRoleAsync(Guid roleId);
        Task AddPermissionAsync(Permission permission);
        Task UpdatePermissionAsync(Permission permission);
        Task<List<string>> GetAllPermissionsAsync();
    }
}
