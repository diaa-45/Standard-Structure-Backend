using System.Threading.Tasks;
using Application.Core.Entities;
using Application.Core.DTOs;

namespace Application.Core.Interfaces
{
 public interface ITokenService
 {
 Task<AuthResponse> CreateTokensAsync(User user);
 Task<AuthResponse?> RefreshAsync(string refreshToken);
 Task RevokeRefreshTokenAsync(string refreshToken);
 }
}
