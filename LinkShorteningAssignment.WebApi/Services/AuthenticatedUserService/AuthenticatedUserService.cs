using System.Security.Claims;

namespace LinkShorteningAssignment.WebApi.Services.AuthenticatedUserService
{
    /*
     *Bu kod, bir kullanıcının kimliğini ve kullanıcı adını almak için bir hizmet sağlar. İlgili bilgiler,
     *IHttpContextAccessor aracılığıyla HTTP isteğinin kimlik bilgilerini kullanarak elde edilir.
     *UserId ve Username özellikleri, kullanıcının kimliğini ve kullanıcı adını saklar ve bu bilgileri dışarıya açık hale getirir. 
     *Bu hizmet, bir ASP.NET Core uygulamasında kullanıcı bilgilerini erişmek için kullanılabilir.
     */
    public class AuthenticatedUserService : IAuthenticatedUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticatedUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            UserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            Username = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.GivenName);
        }

        public string UserId { get; }

        public string Username { get; }
    }
}
