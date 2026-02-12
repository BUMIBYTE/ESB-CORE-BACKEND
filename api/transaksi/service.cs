using MongoDB.Driver;
using Beres.Shared.Models;

namespace RepositoryPattern.Services.TransaksiService
{
    public class TransaksiService : ITransaksiService
    {
        private readonly IMongoCollection<Transaksi> dataUser;
        private readonly IMongoCollection<User> Users;
        private readonly IMongoCollection<Setting3> Setting;
        private readonly IMongoCollection<Event> dataEvent;

        private readonly string key;

        public TransaksiService(IConfiguration configuration)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            dataUser = database.GetCollection<Transaksi>("Transaksi");
            Users = database.GetCollection<User>("User");
            dataEvent = database.GetCollection<Event>("Event");

            Setting = database.GetCollection<Setting3>("Setting");


            this.key = configuration.GetSection("AppSettings")["JwtKey"];
        }
        public async Task<object> Get(string idUser)
        {
            try
            {
                Console.WriteLine("Fetching transactions for user: " + idUser);
                var items = await dataUser
                    .Find(_ => _.IdUser == idUser)
                    .SortByDescending(_ => _.CreatedAt) // Urutkan dari terbaru ke terlama
                    .ToListAsync();

                return new { code = 200, data = items, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> GetSedekah()
        {
            try
            {
                var items = await dataUser
                    .Find(_ => _.Type == "Sedekah" && _.IdUser == "Sedekah")
                    .SortByDescending(_ => _.CreatedAt)
                    .ToListAsync();

                // Hitung total Sedekah berdasarkan status
                var totalSedekah = items.Sum(item =>
                    item.Status == "Income" ? item.Nominal :
                    item.Status == "Expense" ? -item.Nominal : 0
                );

                return new
                {
                    code = 200,
                    data = items,
                    message = "Data Add Complete",
                    totalSedekah
                };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> GetKoperasi()
        {
            try
            {
                var items = await dataUser
                    .Find(_ => _.Type == "KoperasiBulanan" || _.Type == "KoperasiTahunan")
                    .SortByDescending(_ => _.CreatedAt)
                    .ToListAsync();

                // Hitung total Koperasi berdasarkan status
                var totalKoperasi = items.Sum(item =>
                    item.Status == "Income" ? item.Nominal :
                    item.Status == "Expense" ? -item.Nominal : 0
                );
                    

                return new
                {
                    code = 200,
                    data = items,
                    message = "Data Add Complete",
                    totalKoperasi
                };
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

        public async Task<object> PayBulananKoperasi(string idUser)
        {
            try
            {
                var setting = await Setting.Find(d => d.Key == "IuranBulanan").FirstOrDefaultAsync()
                              ?? throw new CustomException(400, "Data", "Data not found");

                var user = await Users.Find(x => x.Id == idUser).FirstOrDefaultAsync();
                if (user == null)
                    throw new CustomException(400, "Error", "Data User Not Found");

                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var filter = Builders<Transaksi>.Filter.And(
                    Builders<Transaksi>.Filter.Eq(_ => _.Type, "KoperasiBulanan"),
                    Builders<Transaksi>.Filter.Eq(_ => _.IdUser, user.Id),
                    Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonth),
                    Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonth)
                );

                var existingTransaction = await dataUser.Find(filter).FirstOrDefaultAsync();
                if (existingTransaction != null)
                    throw new CustomException(400, "Error", "Transaksi koperasi bulan ini sudah ada.");

                var nominal = setting.Value ?? 0;
                if (user.Balance < nominal)
                    throw new CustomException(400, "Error", "Saldo tidak mencukupi");

                user.Balance -= nominal;
                await Users.ReplaceOneAsync(x => x.Id == idUser, user);

                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = user.Id,
                    IdTransaksi = Guid.NewGuid().ToString(),
                    Type = "KoperasiBulanan",
                    Nominal = nominal,
                    Ket = "Iuran Bulanan Koperasi",
                    Status = "Expense",
                    CreatedAt = DateTime.UtcNow
                };
                await dataUser.InsertOneAsync(transaksi);

                return new { code = 200, id = transaksi.Id, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> Sedekah(string idUser, CreateTransaksiDto items)
        {
            try
            {
                var user = await Users.Find(x => x.Id == idUser).FirstOrDefaultAsync();
                if (user == null)
                    throw new CustomException(400, "Error", "Data User Not Found");

                var nominal = items.Nominal ?? 0;
                if (user.Balance < nominal)
                    throw new CustomException(400, "Error", "Saldo tidak mencukupi");

                user.Balance -= nominal;
                await Users.ReplaceOneAsync(x => x.Id == idUser, user);

                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = user.Id,
                    IdTransaksi = Guid.NewGuid().ToString(),
                    Type = "Sedekah",
                    Nominal = nominal,
                    Ket = items.Keterangan ?? "Sedekah",
                    Status = "Expense",
                    CreatedAt = DateTime.UtcNow
                };
                await dataUser.InsertOneAsync(transaksi);

                var transaksi2 = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = "Sedekah",
                    IdTransaksi = Guid.NewGuid().ToString(),
                    Type = "Sedekah",
                    Nominal = nominal,
                    Ket = items.Keterangan ?? "Sedekah",
                    Status = "Income",
                    CreatedAt = DateTime.UtcNow
                };
                await dataUser.InsertOneAsync(transaksi2);

                return new { code = 200, id = transaksi.Id, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

         public async Task<object> Event(string idUser, CreateEventPayDto items)
        {
            try
            {
                var user = await Users.Find(x => x.Phone == idUser).FirstOrDefaultAsync();
                if (user == null)
                    throw new CustomException(400, "Error", "Data User Not Found");

                var dataEvents = await dataEvent.Find(x => x.Id == items.IdEvent).FirstOrDefaultAsync();
                if (dataEvents == null)
                    throw new CustomException(400, "Error", "Data Event Not Found");

                var dataPayed = await dataUser.Find(x => x.IdTransaksi == items.IdEvent && x.IdUser == idUser).FirstOrDefaultAsync();
                if (dataPayed != null)
                    throw new CustomException(400, "Error", "Sudah Membayar Event ini");    

                var nominal = dataEvents.Price ?? 0;
                if (user.Balance < nominal)
                    throw new CustomException(400, "Error", "Saldo tidak mencukupi");

                user.Balance -= nominal;
                await Users.ReplaceOneAsync(x => x.Phone == idUser, user);

                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = user.Phone,
                    IdTransaksi = items.IdEvent,
                    Type = "Event",
                    Nominal = nominal,
                    Ket = dataEvents.Name ?? "Event",
                    Status = "Expense",
                    CreatedAt = DateTime.UtcNow
                };
                await dataUser.InsertOneAsync(transaksi);

                return new { code = 200, id = transaksi.Id, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }


        public async Task<object> Put(string id, CreateTransaksiDto item)
        {
            try
            {
                var TransaksiData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (TransaksiData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                // TransaksiData.Name = item.Name;
                await dataUser.ReplaceOneAsync(x => x.Id == id, TransaksiData);
                return new { code = 200, id = TransaksiData.Id.ToString(), message = "Data Updated" };
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
                var TransaksiData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (TransaksiData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                TransaksiData.IsActive = false;
                await dataUser.ReplaceOneAsync(x => x.Id == id, TransaksiData);
                return new { code = 200, id = TransaksiData.Id.ToString(), message = "Data Deleted" };
            }
            catch (CustomException)
            {
                throw;
            }
        }
    }
}