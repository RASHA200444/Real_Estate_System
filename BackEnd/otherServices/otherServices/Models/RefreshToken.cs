namespace otherServices.Models
{
    public class RefreshToken
    {
        public long RefreshTokenId { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }

        // نخزن HASH مش التوكن raw
        public string TokenHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        // rotation
        public string? ReplacedByTokenHash { get; set; }

        // metadata optional
        public string? CreatedByIp { get; set; }
        public string? RevokedByIp { get; set; }
        public string? UserAgent { get; set; }

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}

