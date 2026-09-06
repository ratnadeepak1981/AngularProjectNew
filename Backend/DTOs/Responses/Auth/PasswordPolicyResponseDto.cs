namespace CampusServicesPortal.DTOs.Responses.Auth
{
    public class PasswordPolicyResponseDto
    {
        public int MinLength { get; set; } = 8;
        public string ComplexityTier { get; set; } = "strong"; // "basic", "medium", "strong", "strict"
        public int ExpiryDays { get; set; } = 90;
        public int ReuseHistoryLimit { get; set; } = 5;
        public int OtpValidityMinutes { get; set; } = 3;
        public int MaxFailedLogins { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 15;
    }
}
