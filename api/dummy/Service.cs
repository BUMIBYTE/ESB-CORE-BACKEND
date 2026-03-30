using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;

namespace RepositoryPattern.Services.DummyService
{
    public class DummyService : IDummyService
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly IMongoCollection<BsonDocument> _collectionEsb;
        private readonly IMongoCollection<BsonDocument> _collectionSAP;
        private readonly IMongoCollection<Asset> _collectionSAPPush;



        public DummyService(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            var database = mongoClient.GetDatabase("itacha");
            _collection = database.GetCollection<BsonDocument>("Sharepoint");
            _collectionSAPPush = database.GetCollection<Asset>("ithacaDB");
            _collectionSAP = database.GetCollection<BsonDocument>("ithacaDB");

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

        public async Task<object> PatchDummyWA(PushAssetModel pushAssetModel)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("__metadata.type", pushAssetModel.__metadata.type);
                var update = Builders<BsonDocument>.Update.Set("Asset_x0020_Number", pushAssetModel.__metadata.type);
                var result = await _collection.UpdateManyAsync(filter, update);

                return new
                {
                    code = 200,
                    message = "Berhasil",
                    modifiedCount = result.ModifiedCount
                };
            }
            catch (Exception ex)
            {
                throw new CustomException(400, "Message", ex.Message);
            }
        }

        public async Task<object> GetDataECC()
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Empty;

                var data = await _collectionSAP.Find(filter).ToListAsync();

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

        public async Task<object> PostData(AssetModel request)
        {
            try
            {
                Random rnd = new Random();
                long number = rnd.NextInt64(1000000000, 9999999999);
                
                var document = new Asset
                {
                    COMPANYCODE = request.COMPANYCODE,
                    ASSETCLASS = request.ASSETCLASS,
                    DESCRIPT = request.DESCRIPT,
                    COSTCENTER = request.COSTCENTER,
                    PLANT = request.PLANT,
                    ULIFE_BOOK = request.ULIFE_BOOK,
                    ULIFE_TAX = request.ULIFE_TAX,
                    ULIFE_IFRS = request.ULIFE_IFRS,
                    ASSET_x0020_Number = number
                };
                await _collectionSAPPush.InsertOneAsync(document);

                return new
                {
                    code = 200,
                    message = "Berhasil",
                    Asset_x0020_Number = number,
                    data = document
                };
            }
            catch (Exception ex)
            {
                throw new CustomException(400, "Message", ex.Message);
            }
        }
    }
}