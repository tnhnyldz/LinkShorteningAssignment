using LinkShorteningAssignment.WebApi.Entities;
using LinkShorteningAssignment.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LinkShorteningAssignment.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        //mongodan alınan fieldler
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoCollection<User> _userRepository;
        private readonly IMongoCollection<Link> _linkRepository;

        //Constructer
        public UsersController(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
            _userRepository = _mongoDatabase.GetCollection<User>("Users");
            _linkRepository = _mongoDatabase.GetCollection<Link>("Links");
        }

        [HttpGet("MostLinkShortenerUser")]
        public async Task<MostLinkShortenerUserResponse> MostLinkShortenerUser(CancellationToken cancellationToken)
        {
            var links = await _linkRepository.Find(_ => true).ToListAsync(cancellationToken);

            var maxData = links.GroupBy(q => q.CreatedBy)
                .Where(grp => grp.Count() > 0)
                .Select(grp => new { UserId = grp.Key, Count = grp.Count() })
                .OrderByDescending(e => e.Count)
                .FirstOrDefault();

            var user= await _userRepository.Find(x => x.Id == maxData.UserId).FirstOrDefaultAsync(cancellationToken);

            return new MostLinkShortenerUserResponse
            {
                FullName = user.FullName,
                LinkCount = maxData.Count
            };
        }

        [HttpGet]
        public async Task<List<User>> Get(CancellationToken cancellationToken) =>
            await _userRepository.Find(_ => true).ToListAsync(cancellationToken);

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<User>> Get(string id, CancellationToken cancellationToken) =>
            await _userRepository.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

        [HttpPost]
        public async Task<IActionResult> Post(User newUser, CancellationToken cancellationToken)
        {
            await _userRepository.InsertOneAsync(newUser, cancellationToken);

            return CreatedAtAction(nameof(Get), new { id = newUser.Id }, newUser);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, User updatedUser, CancellationToken cancellationToken)
        {
            var user = await _userRepository.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            updatedUser.Id = user.Id;

            await _userRepository.ReplaceOneAsync(x => x.Id == id, updatedUser);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var user = await _userRepository.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            await _userRepository.DeleteOneAsync(x => x.Id == id);

            return NoContent();
        }
    }
}
