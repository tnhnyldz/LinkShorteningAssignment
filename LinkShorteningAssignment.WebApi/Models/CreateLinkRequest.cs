using System.ComponentModel.DataAnnotations;

namespace LinkShorteningAssignment.WebApi.Models
{
    public class CreateLinkRequest
    {
        public string OriginalUrl { get; set; }
        public DateTime ExpiredAt { get; set; }
        public string? SpecialKey { get; set; }
    }
}
