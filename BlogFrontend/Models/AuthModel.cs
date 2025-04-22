
namespace BlogFrontend.Models
{
    // Response model for login
    public class LoginResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();

        public string Token { get; set; }
    }



    // Response model for auth check
    public class AuthCheckResponse
    {
        public bool Success { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string UserId { get; set; }

        public string? Name { get; set; }

        public bool IsAuthenticated { get; set; } 


    }

    // Request model for reset password
    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    // Additional model for login request (optional, for form binding)
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Additional model for register request (optional, for form binding)
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // Additional model for forgot password request (optional, for form binding)
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}