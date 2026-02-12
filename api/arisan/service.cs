using MongoDB.Driver;
using Beres.Shared.Models;

namespace RepositoryPattern.Services.ArisanService
{
    public class ArisanService : IArisanService
    {
        private readonly IMongoCollection<Arisan> dataUser;
        private readonly IMongoCollection<Transaksi> dataTransaksi;
        private readonly IMongoCollection<User> User;


        private readonly string key;

        public ArisanService(IConfiguration configuration)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            dataUser = database.GetCollection<Arisan>("Arisan");
            dataTransaksi = database.GetCollection<Transaksi>("Transaksi");

            User = database.GetCollection<User>("User");
            this.key = configuration.GetSection("AppSettings")["JwtKey"];
        }
        public async Task<object> Get()
        {
            try
            {
                var items = await dataUser.Find(_ => _.IsActive == true).ToListAsync();
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var result = new List<object>();

                foreach (var arisan in items)
                {
                    var totalLotTerpakai = arisan.MemberArisans?.Sum(m => m.JumlahLot) ?? 0;
                    var sisaSlot = arisan.TargetLot - totalLotTerpakai;

                    var memberList = new List<object>();
                    if (arisan.MemberArisans != null)
                    {
                        foreach (var member in arisan.MemberArisans)
                        {
                            var filter = Builders<Transaksi>.Filter.And(
                                Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, arisan.Id),
                                Builders<Transaksi>.Filter.Eq(_ => _.Type, "Arisan"),
                                Builders<Transaksi>.Filter.Eq(_ => _.IdUser, member.IdUser),
                                Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonth),
                                Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonth)
                            );

                            var user = Builders<User>.Filter.And(
                                Builders<User>.Filter.Eq(_ => _.Phone, member.IdUser)
                            );

                            var cekDbPayment = await dataTransaksi.Find(filter).FirstOrDefaultAsync();
                            var cekUser = await User.Find(user).FirstOrDefaultAsync();
                            var sudahBayar = cekDbPayment != null;

                            memberList.Add(new
                            {
                                member.Id,
                                member.IsPayed,
                                member.IdUser,
                                member.PhoneNumber,
                                member.JumlahLot,
                                name = cekUser?.FullName ?? "Unknown",
                                IsMonthPayed = sudahBayar
                            });
                        }
                    }

                    result.Add(new
                    {
                        Id = arisan.Id,
                        Title = arisan.Title,
                        Desc = arisan.Description,
                        Keterangan = arisan.Keterangan,
                        Banner = arisan.Banner,
                        Doc = arisan.Document,
                        TotalPrice = arisan.TargetAmount * arisan.TargetLot,
                        TotalSlot = arisan.TargetLot,
                        SisaSlot = sisaSlot,
                        TargetPay = arisan.TargetAmount,
                        JumlahMember = arisan.MemberArisans?.Count ?? 0,
                        MemberArisan = memberList,
                        Kenaikan = arisan.Location,
                        Status = arisan.IsAvailable
                    });
                }

                return new { code = 200, data = result, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> GetbyID(string id, string idUser)
        {
            try
            {
                var items = await dataUser.Find(_ => _.Id == id && _.IsActive == true).FirstOrDefaultAsync();
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // var result = new List<object>();

                var totalLotTerpakai = items.MemberArisans?.Sum(m => m.JumlahLot) ?? 0;
                var sisaSlot = items.TargetLot - totalLotTerpakai;

                var memberList = new List<dynamic>();

                if (items.MemberArisans != null)
                {
                    foreach (var member in items.MemberArisans)
                    {
                        var filter = Builders<Transaksi>.Filter.And(
                            Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, items.Id),
                            Builders<Transaksi>.Filter.Eq(_ => _.Type, "Arisan"),
                            Builders<Transaksi>.Filter.Eq(_ => _.IdUser, member.IdUser),
                            Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonth),
                            Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonth)
                        );

                        var user = Builders<User>.Filter.And(
                            Builders<User>.Filter.Eq(_ => _.Phone, member.IdUser)
                        );

                        var cekDbPayment = await dataTransaksi.Find(filter).FirstOrDefaultAsync();
                        var cekUser = await User.Find(user).FirstOrDefaultAsync();
                        var sudahBayar = cekDbPayment != null;

                        memberList.Add(new
                        {
                            member.IsPayed,
                            member.IdUser,
                            member.PhoneNumber,
                            member.JumlahLot,
                            name = cekUser?.FullName ?? "Unknown",
                            IsMonthPayed = sudahBayar
                        });
                    }
                }

                var Ismembership = memberList.Any(m => m.IdUser == idUser);

                var result = new
                {
                    Id = items.Id,
                    idUser = items.IdUser,
                    Title = items.Title,
                    Desc = items.Description,
                    Keterangan = items.Keterangan,
                    Banner = items.Banner,
                    Doc = items.Document,
                    TotalPrice = items.TargetAmount * items.TargetLot,
                    TotalSlot = items.TargetLot,
                    SisaSlot = sisaSlot,
                    TargetPay = items.TargetAmount,
                    JumlahMember = items.MemberArisans?.Count ?? 0,
                    MemberArisan = memberList,
                    Status = items.IsAvailable,
                    Type = "Arisan",
                    CreatedAt = items.CreatedAt,
                    Kenaikan = items.Location,
                    StatusMember = new
                    {
                        IsMembership = Ismembership,
                        IsPayMonth = memberList.Any(m => m.IdUser == idUser && m.IsMonthPayed),
                        IsPayedClear = memberList.Any(m => m.IdUser == idUser && m.IsPayed)
                    }
                };

                return new { code = 200, data = result, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<Object> GetUser(string Users)
        {
            try
            {
                var items = await dataUser.Find(_ => _.IsActive == true).ToListAsync();
                var userObj = await User.Find(x => x.Id == Users).FirstOrDefaultAsync();
                var idUser = userObj?.Phone;

                // Filter hanya arisan yang memiliki member dengan IdUser yang sesuai
                var filtered = items.Where(arisan =>
                    arisan.MemberArisans != null &&
                    arisan.MemberArisans.Any(m => m.IdUser == idUser)
                );

                var result = filtered.Select(arisan =>
                {
                    var totalLotTerpakai = arisan.MemberArisans?.Sum(m => m.JumlahLot) ?? 0;
                    var sisaSlot = arisan.TargetLot - totalLotTerpakai;

                    return new
                    {
                        Id = arisan.Id,
                        Title = arisan.Title,
                        Desc = arisan.Description,
                        Keterangan = arisan.Keterangan,
                        Banner = arisan.Banner,
                        Doc = arisan.Document,
                        TotalPrice = arisan.TargetAmount * arisan.TargetLot,
                        TotalSlot = arisan.TargetLot,
                        SisaSlot = sisaSlot,
                        TargetPay = arisan.TargetAmount,
                        JumlahMember = arisan.MemberArisans?.Count ?? 0,
                        Status = arisan.IsAvailable,
                        MemberArisan = arisan.MemberArisans,
                        Type = "Arisan",
                        CreatedAt = arisan.CreatedAt,
                    };
                });

                return new { code = 200, data = result, message = "Data berhasil diambil." };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> GetById(string id, string idUser)
        {
            try
            {
                var cekDbArisan = await dataUser.Find(_ => _.Id == id).FirstOrDefaultAsync();
                if (cekDbArisan == null)
                {
                    throw new CustomException(404, "Error", "Data arisan tidak ditemukan.");
                }

                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // Filter transaksi berdasarkan: ID Arisan, ID User, Type, dan tanggal
                var filter = Builders<Transaksi>.Filter.And(
                    Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, id),
                    Builders<Transaksi>.Filter.Eq(_ => _.Type, "Arisan"),
                    Builders<Transaksi>.Filter.Eq(_ => _.IdUser, idUser),
                    Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonth),
                    Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonth)
                );

                var cekDbPayment = await dataTransaksi.Find(filter).FirstOrDefaultAsync();
                var sudahBayar = cekDbPayment != null;

                return new
                {
                    code = 200,
                    IsPay = sudahBayar,
                    message = "Cek pembayaran berhasil."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }

        public async Task<object> PayArisan(CreatePaymentArisan id, string idUser)
        {
            try
            {
                var cekDbArisan = await dataUser.Find(_ => _.Id == id.IdTransaksi).FirstOrDefaultAsync();
                if (cekDbArisan == null)
                {
                    throw new CustomException(404, "Error", "Data arisan tidak ditemukan.");
                }
                var roleData = await User.Find(x => x.Phone == idUser).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data Error");
                var member = cekDbArisan.MemberArisans.Where(m => m.IdUser == idUser && m.IsActive).Sum(m => m.JumlahLot ?? 0);
                if (member == null)
                {
                    throw new CustomException(404, "Error", "Data member arisan tidak ditemukan.");
                }
                if (roleData.Balance < cekDbArisan.TargetAmount * member)
                {
                    throw new CustomException(400, "Error", "Saldo tidak cukup untuk melakukan pembayaran.");
                }
                // Cek apakah transaksi arisan bulan ini sudah ada
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                var filter = Builders<Transaksi>.Filter.And(
                    Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, cekDbArisan.Id),
                    Builders<Transaksi>.Filter.Eq(_ => _.Type, "Arisan"),
                    Builders<Transaksi>.Filter.Eq(_ => _.IdUser, idUser),
                    Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonth),
                    Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonth)
                );
                var existingTransaction = await dataTransaksi.Find(filter).FirstOrDefaultAsync();
                if (existingTransaction != null)
                {
                    throw new CustomException(400, "Error", "Transaksi arisan bulan ini sudah ada.");
                }
                // Kurangi saldo user
                roleData.Balance -= cekDbArisan.TargetAmount * member ?? 0;
                await User.ReplaceOneAsync(x => x.Phone == idUser, roleData);
                // Buat transaksi baru  
                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = idUser,
                    IdTransaksi = cekDbArisan.Id,
                    Type = "Arisan",
                    Nominal = cekDbArisan.TargetAmount * member,
                    Ket = "Pembayaran Arisan",
                    Status = "Expense",
                    CreatedAt = DateTime.Now
                };
                await dataTransaksi.InsertOneAsync(transaksi);

                return new
                {
                    code = 200,
                    message = "Cek pembayaran berhasil."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }

        public async Task<object> PayCompleteArisan(CreatePaymentArisan2 id, string idUser)
        {
            try
            {
                var cekDbArisan = await dataUser.Find(_ => _.Id == id.IdTransaksi).FirstOrDefaultAsync();
                if (cekDbArisan == null)
                {
                    throw new CustomException(404, "Error", "Data arisan tidak ditemukan.");
                }
                var roleData = await User.Find(x => x.Phone == idUser).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data Error");
                if (roleData.IdRole != "2")
                {
                    throw new CustomException(400, "Error", "Hanya admin yang dapat melakukan pembayaran arisan.");
                }
                var member = cekDbArisan.MemberArisans?.FirstOrDefault(m => m.IdUser == id.IdUser && m.IsActive && m.Id == id.Id);
                if (member == null)
                {
                    throw new CustomException(404, "Error", "Data member arisan tidak ditemukan.");
                }
                // Kurangi saldo user
                member.IsPayed = true;
                await dataUser.ReplaceOneAsync(x => x.Id == cekDbArisan.Id, cekDbArisan);

                return new
                {
                    code = 200,
                    message = "Cek pembayaran berhasil."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }

        public async Task<object> Post(CreateArisanDto item, string id)
        {
            try
            {
                // Mapping DTO ke model
                var arisanData = new Arisan
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = id,
                    Title = item.Title,
                    Description = item.Description,
                    Keterangan = item.Keterangan,
                    Banner = item.Banner?.ToList(),
                    Document = item.Document?.ToList(),
                    Location = item.Location,
                    TargetLot = item.TargetLot,
                    MemberArisans = [],
                    TargetAmount = item.TargetAmount,
                    PenagihanDate = item.PenagihanDate,
                    IsAvailable = item.IsAvailable,
                    IsActive = true,                     // properti dari BaseModel
                    IsVerification = false,              // properti dari BaseModel
                    IsPromo = false,
                    CreatedAt = DateTime.Now,
                };

                // Simpan ke database
                await dataUser.InsertOneAsync(arisanData);

                // Response sukses
                return new
                {
                    code = 200,
                    id = arisanData.Id,
                    message = "Data arisan berhasil ditambahkan."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Tangani error umum
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }

        public async Task<object> AddMemberToArisan(CreateMemberArisan newMember)
        {
            try
            {
                // Cek apakah Arisan dengan ID yang diberikan ada
                var filter = Builders<Arisan>.Filter.Eq(a => a.Id, newMember.IdArisan);
                var arisan = await dataUser.Find(filter).FirstOrDefaultAsync();

                if (arisan == null)
                {
                    throw new CustomException(404, "Error", "Data arisan tidak ditemukan.");
                }

                // Cek apakah member dengan ID atau nomor telepon sudah ada
                var isExistingMember = arisan.MemberArisans != null && arisan.MemberArisans.Any(m =>
                    m.IdUser == newMember.IdUser || m.PhoneNumber == newMember.PhoneNumber);

                if (isExistingMember)
                {
                    throw new CustomException(400, "Error", "Member sudah terdaftar dalam arisan ini.");
                }

                await PayArisanFirst(new CreatePaymentArisan
                {
                    IdTransaksi = newMember.IdArisan
                }, newMember.IdUser, newMember.JumlahLot ?? 1);

                var dataArisan = new MemberArisan
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = newMember.IdUser,
                    PhoneNumber = newMember.PhoneNumber,
                    JumlahLot = newMember.JumlahLot,
                    IsActive = newMember.IsActive,
                    IsPayed = newMember.IsPayed
                };

                // Tambahkan member baru ke list
                var update = Builders<Arisan>.Update.Push("MemberArisans", dataArisan);
                await dataUser.UpdateOneAsync(filter, update);

                return new
                {
                    code = 200,
                    message = "Member berhasil ditambahkan ke arisan."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }

        public async Task<object> AddMemberToArisanByAdmin(CreateMemberArisan newMember)
        {
            try
            {
                // Cek apakah Arisan dengan ID yang diberikan ada
                var filterUser = Builders<User>.Filter.Eq(a => a.Phone, newMember.IdUser);
                var user = await User.Find(filterUser).FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new CustomException(404, "Error", "Ponsel tidak ditemukans.");
                }

                var filter = Builders<Arisan>.Filter.Eq(a => a.Id, newMember.IdArisan);
                var arisan = await dataUser.Find(filter).FirstOrDefaultAsync();

                if (arisan == null)
                {
                    throw new CustomException(404, "Error", "Data arisan tidak ditemukan.");
                }

                if(arisan.MemberArisans.Count >= arisan.TargetLot)
                {
                    throw new CustomException(400, "Error", "Arisan sudah penuh.");
                }

                var dataArisan = new MemberArisan
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = newMember.IdUser,
                    PhoneNumber = newMember.PhoneNumber,
                    JumlahLot = newMember.JumlahLot,
                    IsActive = newMember.IsActive,
                    IsPayed = newMember.IsPayed
                };

                // Tambahkan member baru ke list
                var update = Builders<Arisan>.Update.Push("MemberArisans", dataArisan);
                await dataUser.UpdateOneAsync(filter, update);

                return new
                {
                    code = 200,
                    data = update,
                    message = "Member berhasil ditambahkan ke arisan."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }

        public async Task<object> DeleteMemberArisan(DeleteMemberArisan request)
        {
            try
            {
                // Cek apakah Arisan dengan ID yang diberikan ada
                var filter = Builders<Arisan>.Filter.Eq(a => a.Id, request.IdArisan);
                var arisan = await dataUser.Find(filter).FirstOrDefaultAsync();

                if (arisan == null)
                {
                    throw new CustomException(404, "Error", "Data arisan tidak ditemukan.");
                }

                // Buat filter untuk menarik member dengan IdUser yang ingin dihapus
                var memberFilter = Builders<MemberArisan>.Filter.And(
                    Builders<MemberArisan>.Filter.Eq(m => m.IdUser, request.IdUser),
                    Builders<MemberArisan>.Filter.Eq(m => m.Id, request.Id)
                );

                var update = Builders<Arisan>.Update.PullFilter(a => a.MemberArisans, memberFilter);


                var result = await dataUser.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    throw new CustomException(400, "Bad Request", "Member tidak ditemukan dalam arisan.");
                }

                return new
                {
                    code = 200,
                    data = result,
                    message = "Member berhasil dihapus dari arisan."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }


        public async Task<object> PayArisanFirst(CreatePaymentArisan id, string idUser, float lot)
        {
            try
            {
                var cekDbArisan = await dataUser.Find(_ => _.Id == id.IdTransaksi).FirstOrDefaultAsync();
                if (cekDbArisan == null)
                {
                    throw new CustomException(404, "Error", "Data arisan tidak ditemukan.");
                }
                var roleData = await User.Find(x => x.Phone == idUser).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data Error");
                if (roleData.Balance < cekDbArisan.TargetAmount * lot)
                {
                    throw new CustomException(400, "Error", "Saldo tidak cukup untuk melakukan pembayaran.");
                }
                // Cek apakah transaksi arisan bulan ini sudah ada
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                var filter = Builders<Transaksi>.Filter.And(
                    Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, cekDbArisan.Id),
                    Builders<Transaksi>.Filter.Eq(_ => _.Type, "Arisan"),
                    Builders<Transaksi>.Filter.Eq(_ => _.IdUser, idUser),
                    Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonth),
                    Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonth)
                );
                var existingTransaction = await dataTransaksi.Find(filter).FirstOrDefaultAsync();
                if (existingTransaction != null)
                {
                    throw new CustomException(400, "Error", "Transaksi arisan bulan ini sudah ada.");
                }
                // Kurangi saldo user
                roleData.Balance -= cekDbArisan.TargetAmount * lot ?? 0;
                await User.ReplaceOneAsync(x => x.Phone == idUser, roleData);
                // Buat transaksi baru  
                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = idUser,
                    IdTransaksi = cekDbArisan.Id,
                    Type = "Arisan",
                    Nominal = cekDbArisan.TargetAmount * lot,
                    Ket = "Pembayaran Arisan",
                    Status = "Expense",
                    CreatedAt = DateTime.Now
                };
                await dataTransaksi.InsertOneAsync(transaksi);

                return new
                {
                    code = 200,
                    message = "Cek pembayaran berhasil."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }

        public async Task<object> AddBannerToArisan(CreateBannerArisan newMember)
        {
            try
            {
                // Cek apakah Arisan dengan ID yang diberikan ada
                var filter = Builders<Arisan>.Filter.Eq(a => a.Id, newMember.IdArisan);
                var arisan = await dataUser.Find(filter).FirstOrDefaultAsync();

                if (arisan == null)
                {
                    throw new CustomException(404, "Error", "Data arisan tidak ditemukan.");
                }

                var update = Builders<Arisan>.Update
                    .Set(a => a.Banner, newMember.UrlBanner ?? new List<string>())
                    .Set(a => a.Document, newMember.UrlDocument ?? new List<string>());

                await dataUser.UpdateOneAsync(filter, update);

                return new
                {
                    code = 200,
                    message = "Banner dan document berhasil ditambahkan ke arisan."
                };
            }
            catch (CustomException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CustomException(500, "Internal Server Error", ex.Message);
            }
        }

        public async Task<object> Put(string id, CreateArisanDto item)
        {
            try
            {
                var ArisanData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (ArisanData == null)
                {
                    throw new CustomException(400, "Error", "Data Error");
                }
                ArisanData.Title = item.Title;
                ArisanData.Description = item.Description;
                ArisanData.Keterangan = item.Keterangan;
                ArisanData.Banner = item.Banner?.ToList();
                ArisanData.Document = item.Document?.ToList();
                ArisanData.TargetLot = item.TargetLot;
                ArisanData.TargetAmount = item.TargetAmount;
                ArisanData.Location = item.Location;
                // ArisanData.IsActive = item.IsAvailable;
                await dataUser.ReplaceOneAsync(x => x.Id == id, ArisanData);
                return new { code = 200, id = ArisanData.Id.ToString(), message = "Data Updated" };
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
                var ArisanData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (ArisanData == null)
                {
                    throw new CustomException(400, "Error", "Data Error");
                }
                ArisanData.IsActive = false;
                await dataUser.ReplaceOneAsync(x => x.Id == id, ArisanData);
                return new { code = 200, id = ArisanData.Id.ToString(), message = "Data Deleted" };
            }
            catch (CustomException)
            {
                throw;
            }
        }
    }
}