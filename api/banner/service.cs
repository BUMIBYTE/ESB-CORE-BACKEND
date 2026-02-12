using MongoDB.Driver;
using Beres.Shared.Models;

namespace RepositoryPattern.Services.BannerService
{
    public class BannerService : IBannerService
    {
        private readonly IMongoCollection<Banner> dataUser;
        private readonly string key;

        public BannerService(IConfiguration configuration)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            dataUser = database.GetCollection<Banner>("Banner");
            this.key = configuration.GetSection("AppSettings")["JwtKey"];
        }
        public async Task<Object> Get()
        {
            try
            {
                var items = await dataUser.Find(_ => _.IsActive == true).ToListAsync();
                return new { code = 200, data = items, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<Object> GetById(string id)
        {
            try
            {
                var items = await dataUser.Find(_ => _.Id == id).FirstOrDefaultAsync();
                return new { code = 200, data = items, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }
        public async Task<object> Post(CreateBannersDto item)
        {
            try
            {
                var BannerData = new Banner()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = item.Name,
                    Image = item.Image,
                    IsActive = true,
                    IsVerification = false,
                    CreatedAt = DateTime.Now
                };
                await dataUser.InsertOneAsync(BannerData);
                return new { code = 200, id = BannerData.Id, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> Put(string id, CreateBannersDto item)
        {
            try
            {
                var BannerData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (BannerData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                BannerData.Name = item.Name;
                await dataUser.ReplaceOneAsync(x => x.Id == id, BannerData);
                return new { code = 200, id = BannerData.Id.ToString(), message = "Data Updated" };
            }
            catch (CustomException)
            {
                throw;
            }
        }
        public async Task<object> Delete(string id)
        {
            try
            {
                var BannerData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (BannerData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                BannerData.IsActive = false;
                await dataUser.ReplaceOneAsync(x => x.Id == id, BannerData);
                return new { code = 200, id = BannerData.Id.ToString(), message = "Data Deleted" };
            }
            catch (CustomException)
            {
                throw;
            }
        }
    }
}