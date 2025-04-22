
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlogFrontend.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthService _authService;
        private readonly IJSRuntime _js;

        public CustomAuthStateProvider(AuthService authService, IJSRuntime js)
        {
            _authService = authService;
            _js = js;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                string token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var authResponse = await _authService.CheckAuth(token);
                if (authResponse != null)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, authResponse.Id),
                        new Claim(ClaimTypes.Email, authResponse.Email)
                    }, "jwt");
                    return new AuthenticationState(new ClaimsPrincipal(identity));
                }
                else
                {
                    await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }
            catch (Exception)
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        // Corrected NotifyAuthenticationStateChanged method
        public void NotifyUserAuthenticationChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        // Optional: Method to manually trigger logout
        public async Task LogoutAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            NotifyUserAuthenticationChanged();
        }
    }
}