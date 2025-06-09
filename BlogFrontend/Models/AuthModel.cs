namespace BlogFrontend.Models
{
  

    public class LoginResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string Token { get; set; }
    }

    public class AuthCheckResponse
    {
        public bool Success { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; }
        public string? Name { get; set; }
        public bool IsAuthenticated { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class OtpVerificationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ProfilePictureResponse
    {
        public string Message { get; set; } = string.Empty;
        public string ImageBase64 { get; set; } = string.Empty;
    }

    public class UserProfile
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Bio { get; set; }
        public string ProfilePicture { get; set; }
    }
}