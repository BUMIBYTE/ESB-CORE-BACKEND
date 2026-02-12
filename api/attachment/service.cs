using MongoDB.Driver;
using Beres.Shared.Models;

namespace RepositoryPattern.Services.AttachmentService
{
    public class AttachmentService : IAttachmentService
    {
        private readonly string bucketName = "trasgo";
        private readonly IMongoCollection<Attachments> AttachmentLink;
        private readonly IMongoCollection<User> users;

        private readonly string key;

        public AttachmentService(IConfiguration configuration)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            AttachmentLink = database.GetCollection<Attachments>("Attachment");
            this.key = configuration.GetSection("AppSettings")["JwtKey"];
        }
        public async Task<Object> Get(string Username)
        {
            try
            {
                var items = await AttachmentLink.Find(_ => _.UserId == Username).ToListAsync();
                return new { code = 200, data = items, message = "Complete" };
            }
            catch (CustomException ex)
            {

                throw new CustomException(400, "Error", ex.Message); ;
            }
        }
    }
}