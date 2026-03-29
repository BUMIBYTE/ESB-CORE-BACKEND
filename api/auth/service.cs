

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CheckId;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Beres.Shared.Models;
using MongoDB.Bson;

namespace RepositoryPattern.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly IMongoCollection<User> dataUser;


        private readonly string key;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("itacha");
            dataUser = database.GetCollection<User>("User");
            this.key = configuration.GetSection("AppSettings")["JwtKey"];
            _logger = logger;
        }

        public async Task<object> Aktifasi(string id)
        {
            try
            {
                var roleData = await dataUser.Find(x => x.Id == id).FirstOrDefaultAsync() ?? throw new CustomException(400, "Error", "Data not found");
                var user = new ModelViewUser
                {
                    Id = roleData.Id,
                    Phone = roleData.Phone,
                    FullName = roleData.FullName,
                    Email = roleData.Email,
                    Role = roleData.IdRole,
                };
                return new { code = 200, Id = roleData.Id, Data = user };
            }
            catch (CustomException ex)
            {
                throw;
            }
        }

        public async Task<Object> LoginAsync([FromBody] LoginDto login)
        {
            try
            {
                var user = await dataUser.Find(u => u.Email == login.Email).FirstOrDefaultAsync();
                Console.WriteLine("User data: " + JsonSerializer.Serialize(user));
                if (user == null)
                {
                    throw new CustomException(400, "Message", "Email tidak ditemukan");
                }
                if (user.Pin == null || user.Pin == "")
                {
                    throw new CustomException(400, "Message", "Anda belum mengatur PIN, silahkan atur PIN terlebih dahulu");
                }
                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(login.Pin, user.Pin);
                if (!isPasswordCorrect)
                {
                    throw new CustomException(400, "Message", "PIN Salah");
                }

                var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
                var jwtService = new JwtService(configuration);
                string userId = user.Id;
                string token = jwtService.GenerateJwtToken(userId);
                string idAsString = user.Id.ToString();
                return new { code = 200, id = idAsString, accessToken = token };
            }
            catch (CustomException ex)
            {

                throw;
            }
        }
    }

    public class ModelViewUser
    {
        public string? Id { get; set; }
        public string? Phone { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}