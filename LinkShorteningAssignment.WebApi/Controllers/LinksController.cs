using LinkShorteningAssignment.WebApi.Entities;
using LinkShorteningAssignment.WebApi.Exceptions;
using LinkShorteningAssignment.WebApi.Models;
using LinkShorteningAssignment.WebApi.Services.AuthenticatedUserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LinkShorteningAssignment.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LinksController : ControllerBase
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoCollection<Link> _linkRepository;
        private readonly IMongoCollection<User> _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public LinksController(IMongoDatabase mongoDatabase, IHttpContextAccessor httpContextAccessor, IAuthenticatedUserService authenticatedUserService)
        {
            _mongoDatabase = mongoDatabase;
            _linkRepository = _mongoDatabase.GetCollection<Link>("Links");
            _userRepository = _mongoDatabase.GetCollection<User>("Users");
            _httpContextAccessor = httpContextAccessor;
            _authenticatedUserService = authenticatedUserService;
        }

        [AllowAnonymous]
        [HttpGet("/{key}")]
        public async Task<IActionResult> Navigate(string key, CancellationToken cancellationToken)
        {
            var url = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{key}";
            var link = await _linkRepository.Find(x => x.ShortenedUrl == url).FirstOrDefaultAsync(cancellationToken);
            if (link is not null)
            {
                if (link.ExpiredAt < DateTime.Now)
                    throw new AppException("Linkin süresi dolmuş.");

                link.ClickCount++;
                await _linkRepository.ReplaceOneAsync(x => x.Id == link.Id, link);
                return Redirect(link.OriginalUrl);
            }
            else
                throw new AppException("Link bulunamadı");
        }

        [AllowAnonymous]
        [HttpGet("GeyByKey")]
        public async Task<ActionResult<Link>> GeyByKey(string key, CancellationToken cancellationToken)
        {
            var url = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{key}";
            var link = await _linkRepository.Find(x => x.ShortenedUrl == url).FirstOrDefaultAsync(cancellationToken);
            if (link is not null)
            {
                if (link.ExpiredAt < DateTime.Now)
                    throw new AppException("Linkin süresi dolmuş.");

                link.ClickCount++;
                await _linkRepository.ReplaceOneAsync(x => x.Id == link.Id, link);
                return link;
            }
            else
                throw new AppException("Link bulunamadı");
        }

        [HttpGet("MostClickedLinks")]
        public async Task<List<Link>> MostClickedLinks(CancellationToken cancellationToken)
        {
            var links= await _linkRepository.Find(_ => true).SortByDescending(w => w.ClickCount).ToListAsync(cancellationToken);
            var users = await _userRepository.Find(_ => true).ToListAsync(cancellationToken);
            //mongo db ilişkisel bir veritabanı olmadığı için burada farklı bir yöntem ile dönen listeye kullanıcı isimlerini dahil ediyorum.
            foreach (var link in links)
            {
                link.CreatedUser = users.FirstOrDefault(q => q.Id == link.CreatedBy).FullName;
            }
            return links;
        }

        [HttpGet]
        public async Task<List<Link>> Get(CancellationToken cancellationToken) =>
            await _linkRepository.Find(_ => true).ToListAsync(cancellationToken);

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Link>> Get(string id, CancellationToken cancellationToken) =>
            await _linkRepository.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

        [HttpPost]
        public async Task<IActionResult> Post(CreateLinkRequest request, CancellationToken cancellationToken)
        {
            Link newLink = new Link
            {
                ClickCount = 0,
                CreatedAt = DateTime.Now,
                CreatedBy = _authenticatedUserService.UserId,
                ExpiredAt = request.ExpiredAt,
                OriginalUrl = request.OriginalUrl,
                ShortenedUrl = ""
            };

            if (!string.IsNullOrEmpty(request.SpecialKey))
            {
                if (request.SpecialKey.Length > 10)
                    throw new AppException("Özel adres için maksimum karakter sayısı 10 aşıldı.");

                var url = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{request.SpecialKey}";

                var link = await _linkRepository.Find(x => x.ShortenedUrl == url).FirstOrDefaultAsync(cancellationToken);
                if (link is not null)
                    throw new AppException("Bu özel adres kullanılıyor. Lütfen farklı bir adres seçiniz.");

                newLink.ShortenedUrl = url;
            }
            else
            {
                var url = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{newLink.GenerateKey()}";
                newLink.ShortenedUrl = url;
            }

            await _linkRepository.InsertOneAsync(newLink, cancellationToken);

            return CreatedAtAction(nameof(Get), new { id = newLink.Id }, newLink);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Link updatedLink, CancellationToken cancellationToken)
        {
            var link = await _linkRepository.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

            if (link is null)
            {
                return NotFound();
            }

            updatedLink.Id = link.Id;

            await _linkRepository.ReplaceOneAsync(x => x.Id == id, updatedLink);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var link = await _linkRepository.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

            if (link is null)
            {
                return NotFound();
            }

            await _linkRepository.DeleteOneAsync(x => x.Id == id);

            return NoContent();
        }
    }
}
