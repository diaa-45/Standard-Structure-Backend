using Application.Core.DTOs;
using Application.Core.Entities;
using Application.Core.Interfaces;
using Application.Infrastructure.Persistence;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Application.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly AuthDbContext _db;
        private readonly IConfiguration _config;
        private readonly IClientIpProvider _ipProvider;

        public TokenService(AuthDbContext db, IConfiguration config, IClientIpProvider ipProvider)
        {
            _db = db;
            _config = config;
            _ipProvider = ipProvider;
        }

        public async Task<AuthResponse> CreateTokensAsync(User user)
        {
            // load role names for user
            var roleNames = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            var jwt = CreateJwtToken(user, roleNames);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
            // return expiry in local time for callers that expect local time
            var accessExpires = jwt.ValidTo.ToLocalTime();

            var refresh = CreateRefreshToken();
            refresh.UserId = user.Id;
            refresh.CreatedByIp = _ipProvider.GetClientIp();
            await _db.RefreshTokens.AddAsync(refresh);
            await _db.SaveChangesAsync();

            return new AuthResponse(accessToken, refresh.Token, accessExpires);
        }

        public async Task<AuthResponse?> RefreshAsync(string refreshToken)
        {
            var stored = await _db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (stored == null || !stored.IsActive || stored.User == null)
                return null;

            // revoke current refresh token
            stored.Revoked = DateTime.UtcNow;
            stored.RevokedByIp = _ipProvider.GetClientIp();
            _db.RefreshTokens.Update(stored);

            // issue new tokens
            var user = stored.User;

            var roleNames = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            var jwt = CreateJwtToken(user, roleNames);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
            var accessExpires = jwt.ValidTo.ToLocalTime();

            var newRefresh = CreateRefreshToken();
            newRefresh.UserId = user.Id;
            newRefresh.CreatedByIp = _ipProvider.GetClientIp();
            await _db.RefreshTokens.AddAsync(newRefresh);

            await _db.SaveChangesAsync();

            return new AuthResponse(accessToken, newRefresh.Token, accessExpires);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (stored == null) return;
            stored.Revoked = DateTime.UtcNow;
            stored.RevokedByIp = _ipProvider.GetClientIp();
            _db.RefreshTokens.Update(stored);
            await _db.SaveChangesAsync();
        }

        private JwtSecurityToken CreateJwtToken(User user, System.Collections.Generic.IEnumerable<string> roles)
        {
            var jwtCfg = _config.GetSection("Jwt");
            var keyString = jwtCfg["Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresMinutes = int.Parse(jwtCfg["AccessTokenExpirationMinutes"] ?? "60");

            var claims = new System.Collections.Generic.List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            // add role claims
            foreach (var r in roles.Distinct())
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }

            var now = DateTime.UtcNow;
            return new JwtSecurityToken(
                issuer: jwtCfg["Issuer"],
                audience: jwtCfg["Audience"],
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(expiresMinutes),
                signingCredentials: creds
            );
        }

        private static RefreshToken CreateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7) // configurable in future
            };
        }

        public static string HashPassword(string password) => BCrypt.Net.BCrypt.EnhancedHashPassword(password);
        public static bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
    }
}
