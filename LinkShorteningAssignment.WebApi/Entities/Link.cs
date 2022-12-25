using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LinkShorteningAssignment.WebApi.Entities
{
    public class Link
    {
        private const string Characters = "23456789bcdfghjkmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ";
        private static readonly int CharactersLength = Characters.Length;

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string OriginalUrl { get; set; }
        public string ShortenedUrl { get; set; }
        public string CreatedBy { get; set; }
        public int ClickCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiredAt { get; set; }

        [NotMapped]
        public virtual string? CreatedTime => CreatedAt.ToString("dd.MM.yyyy HH:mm");
        [NotMapped]
        public virtual string? ExpiredTime => ExpiredAt.ToString("dd.MM.yyyy HH:mm");
        [NotMapped]
        public virtual string? CreatedUser { get; set; }

        public virtual string GenerateKey()
        {
            int num = int.Parse(DateTime.Now.ToString("MMddHHmmss"));
            var sb = new StringBuilder();
            while (num > 0)
            {
                sb.Insert(0, Characters.ElementAt(num % CharactersLength));
                num = num / CharactersLength;
            }

            

            return sb.ToString();
        }
    }
}
