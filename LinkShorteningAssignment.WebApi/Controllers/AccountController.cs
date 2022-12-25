using LinkShorteningAssignment.WebApi.Entities;
using LinkShorteningAssignment.WebApi.Exceptions;
using LinkShorteningAssignment.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LinkShorteningAssignment.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly JwtSetting _jwtSetting;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoCollection<User> _userRepository;
        public AccountController(IOptions<JwtSetting> jwtSetting, IMongoDatabase mongoDatabase)
        {
            _jwtSetting = jwtSetting.Value;
            _mongoDatabase = mongoDatabase;
            _userRepository = _mongoDatabase.GetCollection<User>("Users");
        }

        [HttpPost("Authenticate")]
        public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                throw new AppException("Hatalı istek.");

            if (string.IsNullOrEmpty(request.Username))
                throw new AppException("Kullanıcı adı boş olamaz.");

            if (string.IsNullOrEmpty(request.Password))
                throw new AppException("Parola boş olamaz.");

            User user = await _userRepository.Find(q => q.Username == request.Username).FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                throw new AppException("Kullanıcı bulunamadı.");

            if (user.Password != request.Password)
                throw new AppException("Kullanıcı veya parola hatalı.");

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Username),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.GivenName,user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSetting.Secret);
            var expDate = DateTime.Now.AddYears(1);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expDate,
                Issuer = _jwtSetting.Issuer,
                Audience = _jwtSetting.Audience,
                NotBefore = DateTime.Now,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);
            
            user.Password = "*********";
            
            return new AuthenticateResponse(user, jwtToken);
        }
    }
}
