using LiteDB;

namespace Siren.Components.Http.Models
{
    public class BinaryAttachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FileName { get; set; } = "";

        public string ContentType { get; set; } = "application/octet-stream";

        [BsonIgnore]
        public byte[]? Data { get; set; }

        public string? DataBase64 { get; set; }

        public long Size { get; set; }

        public BinaryAttachment Copy()
        {
            return new BinaryAttachment
            {
                Id = Guid.NewGuid(),
                FileName = FileName,
                ContentType = ContentType,
                Data = Data?.ToArray(),
                DataBase64 = DataBase64,
                Size = Size
            };
        }
    }
}

