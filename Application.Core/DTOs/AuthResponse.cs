using System;

namespace Application.Core.DTOs
{
 public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpires);
}
