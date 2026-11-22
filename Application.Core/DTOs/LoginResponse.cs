namespace Application.Core.DTOs
{
 public record Response(string AccessToken, int ExpiresInSeconds, UserDto User,List<string> roles);
}