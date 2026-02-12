using MongoDB.Driver;
using Beres.Shared.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;

namespace RepositoryPattern.Services.ChatService
{
    public class ChatService : IChatService
    {
        private readonly IMongoCollection<ChatModel> _ChatCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Setting> _settingCollection;

        public ChatService(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            var database = mongoClient.GetDatabase("beres2db");
            _ChatCollection = database.GetCollection<ChatModel>("Chat");
            _userCollection = database.GetCollection<User>("User");
            _settingCollection = database.GetCollection<Setting>("Setting");
        }

        public async Task<object> SendChatWAAsync(string idUser, CreateChatDto dto)
        {
            try
            {
                var roleData = await _userCollection.Find(x => x.Phone == idUser).FirstOrDefaultAsync();
                if (roleData == null)
                {
                    return new { code = 404, message = "User not found" };
                }

                var openAPI = await _settingCollection.Find(x => x.Key == "OpenAPI").FirstOrDefaultAsync();
                if (openAPI == null)
                {
                    return new { code = 404, message = "User not found" };
                }

                // Simpan pesan dari user
                var userMessage = new ChatModel
                {
                    Id = Guid.NewGuid().ToString(),
                    IdOrder = idUser,
                    IdUser = idUser,
                    Name = roleData.FullName,
                    Sender = "User",
                    CreatedAt = DateTime.UtcNow,
                    Message = dto.Message,
                    Image = dto.Image
                };
                await _ChatCollection.InsertOneAsync(userMessage);

                var currentChat = await _ChatCollection
                    .Find(x => x.IdOrder == idUser && x.Sender == "AI")
                    .SortByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

                var contentChat = "";

                if (currentChat == null || string.IsNullOrEmpty(currentChat.Message))
                {
                    contentChat = dto.Message;
                }
                else if (currentChat.Message.Length <= 20)
                {
                    contentChat = dto.Message;
                }
                else
                {
                    contentChat = "balas dengan limit token 20, dengan pertanyaan :" + dto.Message + " dan jangan lupa untuk menjawab dengan bahasa Indonesia" ;
                }

                // Siapkan payload ke OpenAI
                var openAiPayload = new
                {
                    model = "gpt-4o-mini",
                    store = true,
                    messages = new[]
                    {
                        new { role = "user", content = contentChat }
                    }
                };

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAPI.Value);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var jsonContent = JsonConvert.SerializeObject(openAiPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var openAiResponse = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var openAiResult = await openAiResponse.Content.ReadAsStringAsync();

                if (!openAiResponse.IsSuccessStatusCode)
                {
                    return new { code = (int)openAiResponse.StatusCode, message = "OpenAI API call failed", detail = openAiResult };
                }

                dynamic resultObj = JsonConvert.DeserializeObject(openAiResult);
                string generatedMessage = resultObj?.choices?[0]?.message?.content ?? "No response from AI";

                // Simpan pesan dari AI
                var aiMessage = new ChatModel
                {
                    Id = Guid.NewGuid().ToString(),
                    IdOrder = idUser,
                    IdUser = idUser,
                    Name = "Beres AI",
                    Sender = "AI",
                    CreatedAt = DateTime.UtcNow,
                    Message = generatedMessage,
                    Image = null
                };
                await _ChatCollection.InsertOneAsync(aiMessage);

                return new { code = 200, data = "Berhasil" };
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

        public async Task<object> GetChatWAAsync(string idUser)
        {
            try
            {
                var items = await _ChatCollection
                    .Find(x => x.IdOrder == idUser)
                    .SortBy(x => x.CreatedAt)
                    .ToListAsync();

                if (items == null || !items.Any())
                {
                    return new { code = 404, data = "Chat data not found" };
                }

                var result = items.Select(x => new
                {
                    Message = x.Message,
                    Sender = x.Sender
                }).ToList();

                return new { code = 200, data = result };
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
    }
}
