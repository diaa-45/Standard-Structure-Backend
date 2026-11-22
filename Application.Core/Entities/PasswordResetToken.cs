using System;

namespace Application.Core.Entities
{
    public class PasswordResetToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Token { get; set; } = null!;
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public bool IsUsed { get; set; }
    }
}