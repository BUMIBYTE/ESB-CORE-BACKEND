using MongoDB.Driver;
using Beres.Shared.Models;

namespace RepositoryPattern.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<User> dataUser;
        private readonly IMongoCollection<Transaksi> dataTransaksi;
        private readonly string key;

        public UserService(IConfiguration configuration)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            dataUser = database.GetCollection<User>("User");
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

        public async Task<object> GetMember()
        {
            try
            {
                var items = await dataUser.Find(_ => _.IsActive == true).ToListAsync();
                var memberList = new List<ModelViewUser>();

                foreach (var item in items)
                {
                    var aktifasiResult = await Aktifasi(item.Phone);
                    memberList.Add((ModelViewUser)aktifasiResult);
                }

                // Filter hanya member aktif
                var filteredMembers = memberList.Where(x => x.IsMember == true).ToList();

                return new { code = 200, data = filteredMembers, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }


        public async Task<object> Aktifasi(string id)
        {
            try
            {
                var roleData = await dataUser.Find(x => x.Phone == id).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data not found");
                // Cek apakah transaksi koperasi tahunan tahun ini sudah ada
                var now = DateTime.Now;
                var startOfYear = new DateTime(now.Year, 1, 1);
                var endOfYear = new DateTime(now.Year, 12, 31, 23, 59, 59);

                var filter = Builders<Transaksi>.Filter.And(
                    Builders<Transaksi>.Filter.Eq(_ => _.Type, "KoperasiTahunan"),
                    Builders<Transaksi>.Filter.Eq(_ => _.IdUser, roleData.Phone),
                    Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfYear),
                    Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfYear)
                );

                var startOfMonthPayed = new DateTime(now.Year, now.Month, 1);
                var endOfMonthPayed = startOfMonthPayed.AddMonths(1).AddTicks(-1);
                var existingTransaction = await dataTransaksi.Find(filter).FirstOrDefaultAsync();

                var filterBulanan = Builders<Transaksi>.Filter.And(
                    Builders<Transaksi>.Filter.Eq(_ => _.Type, "KoperasiBulanan"),
                    Builders<Transaksi>.Filter.Eq(_ => _.IdUser, roleData.Phone),
                    Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonthPayed),
                    Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonthPayed)
                );

                var existingTransactionBulanan = await dataTransaksi.Find(filterBulanan).FirstOrDefaultAsync();

                var user = new ModelViewUser
                {
                    Phone = roleData.Phone,
                    FullName = roleData.FullName,
                    Balance = roleData.Balance,
                    Point = roleData.Point,
                    Fcm = roleData.Fcm,
                    Image = roleData.Image,
                    Email = roleData.Email,
                    IsMember = existingTransaction != null,
                    IsPayMonthly = existingTransactionBulanan != null,
                    Role = roleData.IdRole,
                };
                return user;
            }
            catch (CustomException ex)
            {
                throw;
            }
        }

        public async Task<Object> TransferBalance(CreateTransferDto item, string idUser)
        {
            try
            {
                var from = await dataUser.Find(_ => _.Id == idUser).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data User Not Found");
                var destination = await dataUser.Find(_ => _.Phone == item.Phone).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data User Not Found");
                if (from.Balance == null)
                {
                    throw new CustomException(400, "Message", "Saldo anda tidak cukup");
                }
                if (from.Balance < item.Balance)
                {
                    throw new CustomException(400, "Message", "Saldo anda tidak cukup");
                }
                if (from.Phone == destination.Phone)
                {
                    throw new CustomException(400, "Message", "Tidak boleh kirim ke nomor yang sama");
                }
                if (item.Balance < 10000)
                {
                    throw new CustomException(400, "Message", "Minimal Transfer adalah Rp 10.000");
                }
                ///update from balance
                from.Balance -= item.Balance;
                await dataUser.ReplaceOneAsync(x => x.Phone == from.Phone, from);

                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = from.Id,
                    IdTransaksi = Guid.NewGuid().ToString(),
                    Type = "Transfer",
                    Nominal = item.Balance,
                    Ket = "Transfer Kepada " + item.Phone,
                    Status = "Expense",
                    CreatedAt = DateTime.Now
                };
                await dataTransaksi.InsertOneAsync(transaksi);

                ///update destination balance
                destination.Balance += item.Balance;
                await dataUser.ReplaceOneAsync(x => x.Phone == destination.Phone, destination);
                var transaksi2 = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = destination.Id,
                    IdTransaksi = Guid.NewGuid().ToString(),
                    Type = "Transfer",
                    Nominal = item.Balance,
                    Ket = "Transfer Dari " + idUser,
                    Status = "Income",
                    CreatedAt = DateTime.Now
                };
                await dataTransaksi.InsertOneAsync(transaksi2);

                return new { code = 200, message = "Transfer Berhasil" };
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
        public async Task<object> AddUser(CreateUserDto item)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.Phone, item.Phone);
                var user = await dataUser.Find(filter).SingleOrDefaultAsync();
                if (user != null)
                {
                    throw new CustomException(400, "Error", "Phone sudah digunakan.");
                }
                var uuid = Guid.NewGuid().ToString();
                var UserData = new User
                {
                    Id = uuid,
                    Phone = item.Phone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IdRole = "1",
                    FullName = item.FullName,
                    Email = "",
                    Image = "https://apiberes.coderchamps.co.id/api/v1/file/review/68d5c0e92e32a3ea197dc11d",
                    Pin = "",
                    Balance = 0,
                    Point = 0,
                    Fcm = "",
                    NoNIK = "",
                    Address = "",
                    IsActive = true,
                    IsVerification = false
                };
                await dataUser.InsertOneAsync(UserData);
                return new { code = 200, id = UserData.Id, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        // public async Task<object> Put(string id, CreateUserDto item)
        // {
        //     try
        //     {
        //         var UserData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
        //         if (UserData == null)
        //         {
        //             throw new CustomException(400, "Error", "Data Not Found");
        //         }
        //         UserData.FullName = item.Name;
        //         await dataUser.ReplaceOneAsync(x => x.Id == id, UserData);
        //         return new { code = 200, id = UserData.Id.ToString(), message = "Data Updated" };
        //     }
        //     catch (CustomException)
        //     {
        //         throw;
        //     }
        // }
        public async Task<object> Delete(string id)
        {
            try
            {
                var UserData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (UserData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                UserData.IsActive = false;
                await dataUser.ReplaceOneAsync(x => x.Id == id, UserData);
                return new { code = 200, id = UserData.Id.ToString(), message = "Data Deleted" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public class ModelViewUser
        {
            public string? Id { get; set; }
            public string? Phone { get; set; }
            public string? FullName { get; set; }
            public float? Balance { get; set; }
            public float? Point { get; set; }
            public string? Fcm { get; set; }
            public string? Image { get; set; }
            public string? Email { get; set; }
            public bool? IsMember { get; set; }
            public bool? IsPayMonthly { get; set; }

            public string? Role { get; set; }


        }
    }
}