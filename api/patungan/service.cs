using MongoDB.Driver;
using Beres.Shared.Models;
using Google.Cloud.Location;

namespace RepositoryPattern.Services.PatunganService
{
    public class PatunganService : IPatunganService
    {
        private readonly IMongoCollection<Patungan> dataUser;
        private readonly IMongoCollection<Transaksi> dataTransaksi;
        private readonly IMongoCollection<User> User;

        private readonly string key;

        public PatunganService(IConfiguration configuration)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            dataUser = database.GetCollection<Patungan>("Patungan");
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

                foreach (var Patungan in items)
                {
                    var totalLotTerpakai = Patungan.MemberPatungans?.Sum(m => m.JumlahLot) ?? 0;
                    var sisaSlot = Patungan.TargetLot - totalLotTerpakai;

                    var memberList = new List<object>();
                    if (Patungan.MemberPatungans != null)
                    {
                        foreach (var member in Patungan.MemberPatungans)
                        {
                            var filter = Builders<Transaksi>.Filter.And(
                                Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, Patungan.Id),
                                Builders<Transaksi>.Filter.Eq(_ => _.Type, "Patungan"),
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
                        Id = Patungan.Id,
                        Title = Patungan.Title,
                        Desc = Patungan.Description,
                        Keterangan = Patungan.Keterangan,
                        Banner = Patungan.Banner,
                        Doc = Patungan.Document,
                        TotalPrice = Patungan.TargetAmount * Patungan.TargetLot,
                        TotalSlot = Patungan.TargetLot,
                        SisaSlot = sisaSlot,
                        TargetPay = Patungan.TargetAmount,
                        JumlahMember = Patungan.MemberPatungans?.Count ?? 0,
                        MemberPatungan = memberList,
                        Kenaikan = Patungan.Location,
                        Status = Patungan.IsAvailable
                    });
                }

                return new { code = 200, data = result, message = "Data Add Complete" };
            }
            catch (CustomException)
            {
                throw;
            }
        }

        public async Task<object> GetbyID(string id, string Users)
        {
            try
            {
                var items = await dataUser.Find(_ => _.Id == id && _.IsActive == true).FirstOrDefaultAsync();
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var userObj = await User.Find(x => x.Id == Users).FirstOrDefaultAsync();
                var idUser = userObj?.Phone;

                // var result = new List<object>();

                var totalLotTerpakai = items.MemberPatungans?.Sum(m => m.JumlahLot) ?? 0;
                var sisaSlot = items.TargetLot - totalLotTerpakai;

                var memberList = new List<dynamic>();

                if (items.MemberPatungans != null)
                {
                    foreach (var member in items.MemberPatungans)
                    {
                        var filter = Builders<Transaksi>.Filter.And(
                            Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, items.Id),
                            Builders<Transaksi>.Filter.Eq(_ => _.Type, "Patungan"),
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
                    JumlahMember = items.MemberPatungans?.Count ?? 0,
                    MemberPatungan = memberList,
                    Status = items.IsAvailable,
                    Type = "Patungan",
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

                // Filter hanya Patungan yang memiliki member dengan IdUser yang sesuai
                var filtered = items.Where(Patungan =>
                    Patungan.MemberPatungans != null &&
                    Patungan.MemberPatungans.Any(m => m.IdUser == idUser)
                );

                var result = filtered.Select(Patungan =>
                {
                    var totalLotTerpakai = Patungan.MemberPatungans?.Sum(m => m.JumlahLot) ?? 0;
                    var sisaSlot = Patungan.TargetLot - totalLotTerpakai;

                    return new
                    {
                        Id = Patungan.Id,
                        Title = Patungan.Title,
                        Desc = Patungan.Description,
                        Keterangan = Patungan.Keterangan,
                        Banner = Patungan.Banner,
                        Doc = Patungan.Document,
                        TotalPrice = Patungan.TargetAmount * Patungan.TargetLot,
                        TotalSlot = Patungan.TargetLot,
                        SisaSlot = sisaSlot,
                        TargetPay = Patungan.TargetAmount,
                        JumlahMember = Patungan.MemberPatungans?.Count ?? 0,
                        Status = Patungan.IsAvailable,
                        MemberPatungan = Patungan.MemberPatungans,
                        Type = "Patungan",
                        CreatedAt = Patungan.CreatedAt,
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
                var cekDbPatungan = await dataUser.Find(_ => _.Id == id).FirstOrDefaultAsync();
                if (cekDbPatungan == null)
                {
                    throw new CustomException(404, "Not Found", "Data Patungan tidak ditemukan.");
                }

                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // Filter transaksi berdasarkan: ID Patungan, ID User, Type, dan tanggal
                var filter = Builders<Transaksi>.Filter.And(
                    Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, id),
                    Builders<Transaksi>.Filter.Eq(_ => _.Type, "Patungan"),
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

        public async Task<object> PayPatungan(CreatePaymentPatungan id, string Users)
        {
            try
            {
                var userObj = await User.Find(x => x.Id == Users).FirstOrDefaultAsync();
                var idUser = userObj?.Phone;

                var cekDbPatungan = await dataUser.Find(_ => _.Id == id.IdTransaksi).FirstOrDefaultAsync();
                if (cekDbPatungan == null)
                {
                    throw new CustomException(404, "Not Found", "Data Patungan tidak ditemukan.");
                }
                var roleData = await User.Find(x => x.Phone == idUser).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data not found");
                var member = cekDbPatungan.MemberPatungans?.FirstOrDefault(m => m.IdUser == idUser && m.IsActive);
                if (member == null)
                {
                    throw new CustomException(404, "Not Found", "Data member Patungan tidak ditemukan.");
                }
                if (roleData.Balance < cekDbPatungan.TargetAmount * member.JumlahLot)
                {
                    throw new CustomException(400, "Error", "Saldo tidak cukup untuk melakukan pembayaran.");
                }
                // Cek apakah transaksi Patungan bulan ini sudah ada
                // var now = DateTime.Now;
                // var startOfMonth = new DateTime(now.Year, now.Month, 1);
                // var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                // var filter = Builders<Transaksi>.Filter.And(
                //     Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, cekDbPatungan.Id),
                //     Builders<Transaksi>.Filter.Eq(_ => _.Type, "Patungan"),
                //     Builders<Transaksi>.Filter.Eq(_ => _.IdUser, idUser),
                //     Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonth),
                //     Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonth)
                // );
                // var existingTransaction = await dataTransaksi.Find(filter).FirstOrDefaultAsync();
                // if (existingTransaction != null)
                // {
                //     throw new CustomException(400, "Error", "Transaksi Patungan bulan ini sudah ada.");
                // }
                // Kurangi saldo user
                roleData.Balance -= cekDbPatungan.TargetAmount * member.JumlahLot ?? 0;
                await User.ReplaceOneAsync(x => x.Phone == idUser, roleData);
                // Buat transaksi baru  
                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = Users,
                    IdTransaksi = cekDbPatungan.Id,
                    Type = "Patungan",
                    Nominal = cekDbPatungan.TargetAmount * member.JumlahLot,
                    Ket = "Pembayaran Patungan",
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
        
        public async Task<object> RefundPatunganMemberbyAdmin(RefundMemberPatungan id)
        {
            try
            {
                var filter = Builders<Patungan>.Filter.Eq(a => a.Id, id.IdPatungan);
                var cekDbPatungan = await dataUser.Find(filter).FirstOrDefaultAsync();
                if (cekDbPatungan == null)
                {
                    throw new CustomException(404, "Error", "Data Patungan tidak ditemukan.");
                }
                var roleData = await User.Find(x => x.Phone == id.IdUser).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data not found");
                var member = cekDbPatungan.MemberPatungans?.FirstOrDefault(m => m.IdUser == id.IdUser && m.IsActive);
                if (member == null)
                {
                    throw new CustomException(404, "Error", "Data member Patungan tidak ditemukan.");
                }
                 // Buat filter untuk menarik member dengan IdUser yang ingin dihapus
                var memberFilter = Builders<MemberPatungan>.Filter.And(
                    Builders<MemberPatungan>.Filter.Eq(m => m.IdUser, id.IdUser),
                    Builders<MemberPatungan>.Filter.Eq(m => m.Id, id.Id)
                );

                var update = Builders<Patungan>.Update.PullFilter(a => a.MemberPatungans, memberFilter);


                var result = await dataUser.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    throw new CustomException(400, "Error", "Member tidak ditemukan dalam Patungan.");
                }
                // Kurangi saldo user
                roleData.Balance += cekDbPatungan.TargetAmount * member.JumlahLot ?? 0;
                await User.ReplaceOneAsync(x => x.Phone == id.IdUser, roleData);
                // Buat transaksi baru  
                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = id.IdUser,
                    IdTransaksi = cekDbPatungan.Id,
                    Type = "Patungan",
                    Nominal = cekDbPatungan.TargetAmount * member.JumlahLot,
                    Ket = "Refund Patungan",
                    Status = "Income",
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

        public async Task<object> RefundPatunganMember(RefundMemberPatungan id, string idUser)
        {
            try
            {
                var cekDbPatungan = await dataUser.Find(_ => _.Id == id.IdPatungan).FirstOrDefaultAsync();
                if (cekDbPatungan == null)
                {
                    throw new CustomException(404, "Not Found", "Data Patungan tidak ditemukan.");
                }
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

        public async Task<object> PayCompletePatungan(CreatePaymentPatungan2 id, string idUser)
        {
            try
            {
                var cekDbPatungan = await dataUser.Find(_ => _.Id == id.IdTransaksi).FirstOrDefaultAsync();
                if (cekDbPatungan == null)
                {
                    throw new CustomException(404, "Not Found", "Data Patungan tidak ditemukan.");
                }
                var roleData = await User.Find(x => x.Phone == idUser).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data not found");
                if (roleData.IdRole != "2")
                {
                    throw new CustomException(400, "Error", "Hanya admin yang dapat melakukan pembayaran Patungan.");
                }
                var member = cekDbPatungan.MemberPatungans?.FirstOrDefault(m => m.IdUser == id.IdUser && m.IsActive);
                if (member == null)
                {
                    throw new CustomException(404, "Not Found", "Data member Patungan tidak ditemukan.");
                }
                // Kurangi saldo user
                member.IsPayed = true;
                await dataUser.ReplaceOneAsync(x => x.Id == cekDbPatungan.Id, cekDbPatungan);

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

        public async Task<object> Post(CreatePatunganDto item, string id)
        {
            try
            {
                // Mapping DTO ke model
                var PatunganData = new Patungan
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
                    MemberPatungans = [],
                    TargetAmount = item.TargetAmount,
                    PenagihanDate = item.PenagihanDate,
                    IsAvailable = item.IsAvailable,
                    IsActive = true,                     // properti dari BaseModel
                    IsVerification = false,              // properti dari BaseModel
                    IsPromo = false,
                    CreatedAt = DateTime.Now,
                };

                // Simpan ke database
                await dataUser.InsertOneAsync(PatunganData);

                // Response sukses
                return new
                {
                    code = 200,
                    id = PatunganData.Id,
                    message = "Data Patungan berhasil ditambahkan."
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

        public async Task<object> AddMemberToPatungan(CreateMemberPatungan newMember)
        {
            try
            {
                // Cek apakah Patungan dengan ID yang diberikan ada
                var filter = Builders<Patungan>.Filter.Eq(a => a.Id, newMember.IdPatungan);
                var Patungan = await dataUser.Find(filter).FirstOrDefaultAsync();

                if (Patungan == null)
                {
                    throw new CustomException(404, "Not Found", "Data Patungan tidak ditemukan.");
                }

                // Cek apakah member dengan ID atau nomor telepon sudah ada
                // var isExistingMember = Patungan.MemberPatungans != null && Patungan.MemberPatungans.Any(m =>
                //     m.IdUser == newMember.IdUser || m.PhoneNumber == newMember.PhoneNumber);

                // if (isExistingMember)
                // {
                //     throw new CustomException(400, "Error", "Member sudah terdaftar dalam Patungan ini.");
                // }

                await PayPatunganFirst(new CreatePaymentPatungan
                {
                    IdTransaksi = newMember.IdPatungan
                }, newMember.IdUser, newMember.JumlahLot ?? 1);

                var dataPatungan = new MemberPatungan
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = newMember.IdUser,
                    PhoneNumber = newMember.PhoneNumber,
                    JumlahLot = newMember.JumlahLot,
                    IsActive = newMember.IsActive,
                    IsPayed = newMember.IsPayed
                };

                // Tambahkan member baru ke list
                var update = Builders<Patungan>.Update.Push("MemberPatungans", dataPatungan);
                await dataUser.UpdateOneAsync(filter, update);

                return new
                {
                    code = 200,
                    message = "Member berhasil ditambahkan ke Patungan."
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

        public async Task<object> PayPatunganFirst(CreatePaymentPatungan id, string idUser, float lot)
        {
            try
            {
                var cekDbPatungan = await dataUser.Find(_ => _.Id == id.IdTransaksi).FirstOrDefaultAsync();
                if (cekDbPatungan == null)
                {
                    throw new CustomException(404, "Not Found", "Data Patungan tidak ditemukan.");
                }
                var roleData = await User.Find(x => x.Phone == idUser).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data not found");
                if (roleData.Balance < cekDbPatungan.TargetAmount * lot)
                {
                    throw new CustomException(400, "Error", "Saldo tidak cukup untuk melakukan pembayaran.");
                }
                // Cek apakah transaksi Patungan bulan ini sudah ada
                // var now = DateTime.Now;
                // var startOfMonth = new DateTime(now.Year, now.Month, 1);
                // var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                // var filter = Builders<Transaksi>.Filter.And(
                //     Builders<Transaksi>.Filter.Eq(_ => _.IdTransaksi, cekDbPatungan.Id),
                //     Builders<Transaksi>.Filter.Eq(_ => _.Type, "Patungan"),
                //     Builders<Transaksi>.Filter.Eq(_ => _.IdUser, idUser),
                //     Builders<Transaksi>.Filter.Gte(_ => _.CreatedAt, startOfMonth),
                //     Builders<Transaksi>.Filter.Lte(_ => _.CreatedAt, endOfMonth)
                // );
                // var existingTransaction = await dataTransaksi.Find(filter).FirstOrDefaultAsync();
                // if (existingTransaction != null)
                // {
                //     throw new CustomException(400, "Error", "Transaksi Patungan bulan ini sudah ada.");
                // }
                // Kurangi saldo user
                roleData.Balance -= cekDbPatungan.TargetAmount * lot ?? 0;
                await User.ReplaceOneAsync(x => x.Phone == idUser, roleData);
                // Buat transaksi baru  
                var transaksi = new Transaksi
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = roleData.Id,
                    IdTransaksi = cekDbPatungan.Id,
                    Type = "Patungan",
                    Nominal = cekDbPatungan.TargetAmount * lot,
                    Ket = "Pembayaran Patungan",
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

        public async Task<object> AddMemberToPatunganByAdmin(CreateMemberPatungan newMember)
        {
            try
            {
                // Cek apakah Patungan dengan ID yang diberikan ada
                var filterUser = Builders<User>.Filter.Eq(a => a.Phone, newMember.IdUser);
                var user = await User.Find(filterUser).FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new CustomException(404, "Error", "Ponsel tidak ditemukans.");
                }

                var filter = Builders<Patungan>.Filter.Eq(a => a.Id, newMember.IdPatungan);
                var Patungan = await dataUser.Find(filter).FirstOrDefaultAsync();

                if (Patungan == null)
                {
                    throw new CustomException(404, "Error", "Data Patungan tidak ditemukan.");
                }

                if(Patungan.MemberPatungans.Count >= Patungan.TargetLot)
                {   
                    throw new CustomException(400, "Error", "Patungan sudah penuh.");
                }

                var dataPatungan = new MemberPatungan
                {
                    Id = Guid.NewGuid().ToString(),
                    IdUser = newMember.IdUser,
                    PhoneNumber = newMember.PhoneNumber,
                    JumlahLot = newMember.JumlahLot,
                    IsActive = newMember.IsActive,
                    IsPayed = newMember.IsPayed
                };

                // Tambahkan member baru ke list
                var update = Builders<Patungan>.Update.Push("MemberPatungans", dataPatungan);
                await dataUser.UpdateOneAsync(filter, update);

                return new
                {
                    code = 200,
                    data = update,
                    message = "Member berhasil ditambahkan ke Patungan."
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

        public async Task<object> DeleteMemberPatungan(DeleteMemberPatungan request)
        {
            try
            {
                // Cek apakah Patungan dengan ID yang diberikan ada
                var filter = Builders<Patungan>.Filter.Eq(a => a.Id, request.IdPatungan);
                var Patungan = await dataUser.Find(filter).FirstOrDefaultAsync();

                if (Patungan == null)
                {
                    throw new CustomException(404, "Error", "Data Patungan tidak ditemukan.");
                }

                // Buat filter untuk menarik member dengan IdUser yang ingin dihapus
                var memberFilter = Builders<MemberPatungan>.Filter.And(
                    Builders<MemberPatungan>.Filter.Eq(m => m.IdUser, request.IdUser),
                    Builders<MemberPatungan>.Filter.Eq(m => m.Id, request.Id)
                );

                var update = Builders<Patungan>.Update.PullFilter(a => a.MemberPatungans, memberFilter);


                var result = await dataUser.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    throw new CustomException(400, "Bad Request", "Member tidak ditemukan dalam Patungan.");
                }

                return new
                {
                    code = 200,
                    data = result,
                    message = "Member berhasil dihapus dari Patungan."
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

        public async Task<object> AddBannerToPatungan(CreateBannerPatungan newMember)
        {
            try
            {
                // Cek apakah Patungan dengan ID yang diberikan ada
                var filter = Builders<Patungan>.Filter.Eq(a => a.Id, newMember.IdPatungan);
                var Patungan = await dataUser.Find(filter).FirstOrDefaultAsync();

                if (Patungan == null)
                {
                    throw new CustomException(404, "Not Found", "Data Patungan tidak ditemukan.");
                }

                var update = Builders<Patungan>.Update
                    .Set(a => a.Banner, newMember.UrlBanner ?? new List<string>())
                    .Set(a => a.Document, newMember.UrlDocument ?? new List<string>());

                await dataUser.UpdateOneAsync(filter, update);

                return new
                {
                    code = 200,
                    message = "Banner dan document berhasil ditambahkan ke Patungan."
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

        public async Task<object> Put(string id, CreatePatunganDto item)
        {
            try
            {
                var PatunganData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (PatunganData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                PatunganData.Title = item.Title;
                PatunganData.Description = item.Description;
                PatunganData.Keterangan = item.Keterangan;
                PatunganData.Banner = item.Banner?.ToList();
                PatunganData.Document = item.Document?.ToList();
                PatunganData.TargetLot = item.TargetLot;
                PatunganData.TargetAmount = item.TargetAmount;
                PatunganData.Location = item.Location;
                await dataUser.ReplaceOneAsync(x => x.Id == id, PatunganData);
                return new { code = 200, id = PatunganData.Id.ToString(), message = "Data Updated" };
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
                var PatunganData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (PatunganData == null)
                {
                    throw new CustomException(400, "Error", "Data Not Found");
                }
                PatunganData.IsActive = false;
                await dataUser.ReplaceOneAsync(x => x.Id == id, PatunganData);
                return new { code = 200, id = PatunganData.Id.ToString(), message = "Data Deleted" };
            }
            catch (CustomException)
            {
                throw;
            }
        }
    }
}