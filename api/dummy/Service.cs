using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;

namespace RepositoryPattern.Services.RedPayService
{
    public class RedPayService : IRedPayService
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly IMongoCollection<BsonDocument> _collectionEsb;
        private readonly IMongoCollection<BsonDocument> _collectionSAP;


        public RedPayService(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            var database = mongoClient.GetDatabase("primakom");
            _collection = database.GetCollection<BsonDocument>("primakomSH");
            _collectionEsb = database.GetCollection<BsonDocument>("primakomESB");
            _collectionSAP = database.GetCollection<BsonDocument>("primakomSAP");


        }

        public async Task<object> GetData()
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Empty;

                var data = await _collection.Find(filter).ToListAsync();

                if (data.Count == 0)
                    throw new CustomException(400, "Data", "Data not found");

                var result = data.Select(x => BsonTypeMapper.MapToDotNetValue(x));

                return new
                {
                    code = 200,
                    message = "Berhasil",
                    data = result
                };
            }
            catch (Exception ex)
            {
                throw new CustomException(400, "Message", ex.Message);
            }
        }

        public async Task<object> GetDataESB()
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Empty;

                var data = await _collectionEsb.Find(filter).ToListAsync();

                if (data.Count == 0)
                    throw new CustomException(400, "Data", "Data not found");

                var result = data.Select(x => BsonTypeMapper.MapToDotNetValue(x));

                return new
                {
                    code = 200,
                    message = "Berhasil",
                    data = result
                };
            }
            catch (Exception ex)
            {
                throw new CustomException(400, "Message", ex.Message);
            }
        }

        public async Task<object> PostData(JsonElement request)
        {
            try
            {
                var json = request.GetRawText();
                var document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);

                await _collectionSAP.InsertOneAsync(document);
                Random rnd = new Random();
                long number = rnd.NextInt64(1000000000, 9999999999);

                return new
                {
                    code = 200,
                    message = "Berhasil",
                    Asset_x0020_Number = number,
                    data = BsonTypeMapper.MapToDotNetValue(document)
                };
            }
            catch (Exception ex)
            {
                throw new CustomException(400, "Message", ex.Message);
            }
        }
    }
}