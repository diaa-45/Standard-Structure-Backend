namespace Application.Core.DTOs
{
 public record PasswordResetConfirmRequest(string Token, string NewPassword);
}
