using MongoDB.Driver;
using Beres.Shared.Models;

namespace RepositoryPattern.Services.EventService
{
    public class EventService : IEventService
    {
        private readonly IMongoCollection<Event> dataUser;
        private readonly IMongoCollection<Transaksi> dataTransaksi;
        private readonly IMongoCollection<User> Users;


        private readonly string key;

        public EventService(IConfiguration configuration)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            dataUser = database.GetCollection<Event>("Event");
            Users = database.GetCollection<User>("User");
            dataTransaksi = database.GetCollection<Transaksi>("Transaksi");

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

        public async Task<Object> GetById(string id, string idUser)
        {
            try
            {
                var dataEvents = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (dataEvents == null)
                    throw new CustomException(400, "Error", "Data Event Not Found");

                var items = await dataTransaksi.Find(_ => _.IdTransaksi == id && _.IdUser == idUser).FirstOrDefaultAsync();
                return new { code = 200, data = items, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> GetListById(string id)
        {
            try
            {
                // Cek apakah data event dengan ID yang diberikan ada
                var dataEvent = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (dataEvent == null)
                    throw new CustomException(400, "Error", "Data Event Not Found");

                // Ambil semua transaksi yang terkait dengan ID tersebut
                var items = await dataTransaksi.Find(x => x.IdTransaksi == id).ToListAsync();

                var listUser = new List<object>();
                foreach (var item in items)
                {
                    var user = await Users.Find(x => x.Phone == item.IdUser).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        var userData = new 
                        {
                            Id = user.Id,
                            FullName = user.FullName,
                            Phone = user.Phone,
                        };
                        listUser.Add(userData);
                    }
                }

                return new
                {
                    code = 200,
                    data = listUser,
                    message = "Data Fetch Complete"
                };
            }
            catch (CustomException)
            {
                throw; // biarkan diteruskan ke middleware atau handler global
            }
            catch (Exception ex)
            {
                // Tangani exception umum yang tidak terduga
                throw new CustomException(500, "Server Error", ex.Message);
            }
        }

        public async Task<object> Post(CreateEventDto item)
        {
            try
            {
                var EventData = new Event()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = item.Name,
                    Image = item.Image,
                    DueDate = item.DueDate,
                    Price = item.Price,
                    Desc = item.Desc,
                    Location = item.Location,
                    IsActive = true,
                    IsVerification = false,
                    CreatedAt = DateTime.Now
                };
                await dataUser.InsertOneAsync(EventData);
                return new { code = 200, id = EventData.Id, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> Put(string id, CreateEventDto item)
        {
            try
            {
                var EventData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (EventData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                EventData.Name = item.Name;
                EventData.Image = item.Image;
                EventData.DueDate = item.DueDate;
                EventData.Price = item.Price;
                EventData.Desc = item.Desc;
                EventData.Location = item.Location;
                await dataUser.ReplaceOneAsync(x => x.Id == id, EventData);
                return new { code = 200, id = EventData.Id.ToString(), message = "Data Updated" };
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
                var EventData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (EventData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                EventData.IsActive = false;
                await dataUser.ReplaceOneAsync(x => x.Id == id, EventData);
                return new { code = 200, id = EventData.Id.ToString(), message = "Data Deleted" };
            }
            catch (CustomException)
            {
                throw;
            }
        }
    }
}