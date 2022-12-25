using LinkShorteningAssignment.WebApi.Entities;

namespace LinkShorteningAssignment.WebApi.Models
{
    public class AuthenticateResponse
    {
        public User User { get; set; }
        public string accessToken { get; set; }

        public AuthenticateResponse(User user, string jwtToken)
        {
            User = user;
            accessToken = jwtToken;
        }
    }
}
