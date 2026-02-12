using MongoDB.Driver;
using Beres.Shared.Models;

namespace RepositoryPattern.Services.SettingService
{
    public class SettingService : ISettingService
    {
        private readonly IMongoCollection<Setting> dataUser;
        private readonly string key;

        public SettingService(IConfiguration configuration)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            dataUser = database.GetCollection<Setting>("Setting");
            this.key = configuration.GetSection("AppSettings")["JwtKey"];
        }
        public async Task<Object> Get()
        {
            try
            {
                var items = await dataUser.Find(_ => true).ToListAsync();
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
        public async Task<object> Post(CreateSettingsDto item)
        {
            try
            {
                var SettingData = new Setting()
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = item.Key,
                    Value = item.Value,
                    IsActive = true,
                    IsVerification = false,
                    CreatedAt = DateTime.Now
                };
                await dataUser.InsertOneAsync(SettingData);
                return new { code = 200, id = SettingData.Id, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> Put(string id, CreateSettingsDto item)
        {
            try
            {
                var SettingData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (SettingData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                SettingData.Key = item.Key;
                SettingData.Value = item.Value;
                await dataUser.ReplaceOneAsync(x => x.Id == id, SettingData);
                return new { code = 200, id = SettingData.Id.ToString(), message = "Data Updated" };
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
                var SettingData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (SettingData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                SettingData.IsActive = false;
                await dataUser.ReplaceOneAsync(x => x.Id == id, SettingData);
                return new { code = 200, id = SettingData.Id.ToString(), message = "Data Deleted" };
            }
            catch (CustomException)
            {
                throw;
            }
        }
    }
}