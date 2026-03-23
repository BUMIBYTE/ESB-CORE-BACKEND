using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;

namespace RepositoryPattern.Services.JbangService
{
    public class JbangService : IJbangService
    {
        private readonly string _basePath;

        public JbangService(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            var database = mongoClient.GetDatabase("primakom");

            // base folder (biar aman)
            _basePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        // ✅ Create Folder
        public string CreateFolder(string path)
        {
            string fullPath = Path.Combine(_basePath, path);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                return $"Folder berhasil dibuat: {fullPath}";
            }

            return $"Folder sudah ada: {fullPath}";
        }

        // ✅ Create File + Write Content
        public string CreateFile(string folderPath, string fileName, string content)
        {
            string fullFolderPath = Path.Combine(_basePath, folderPath);

            // pastikan folder ada
            if (!Directory.Exists(fullFolderPath))
            {
                Directory.CreateDirectory(fullFolderPath);
            }

            string filePath = Path.Combine(fullFolderPath, fileName);

            File.WriteAllText(filePath, content);

            return $"File berhasil dibuat: {filePath}";
        }

        // ✅ Read File
        public string ReadFile(string filePath)
        {
            string fullPath = Path.Combine(_basePath, filePath);

            if (!File.Exists(fullPath))
            {
                return "File tidak ditemukan";
            }

            return File.ReadAllText(fullPath);
        }

        public object ReadFolder(string path)
        {
            string fullPath = Path.Combine(_basePath, path);

            if (!Directory.Exists(fullPath))
            {
                return new { message = "Folder tidak ditemukan" };
            }

            var directories = Directory.GetDirectories(fullPath)
                .Select(d => new
                {
                    name = Path.GetFileName(d),
                    type = "folder"
                });

            var files = Directory.GetFiles(fullPath)
                .Select(f => new
                {
                    name = Path.GetFileName(f),
                    type = "file",
                    size = new FileInfo(f).Length
                });

            return new
            {
                path = fullPath,
                folders = directories,
                files = files
            };
        }

        public string DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Path tidak boleh kosong";

            if (path.Contains(".."))
                return "Path tidak valid";

            string fullPath = Path.GetFullPath(Path.Combine(_basePath, path));

            if (!fullPath.StartsWith(_basePath))
                return "Akses ditolak";

            if (!File.Exists(fullPath))
                return "File tidak ditemukan";

            File.Delete(fullPath);

            return $"File berhasil dihapus: {fullPath}";
        }

        public string DeleteFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Path tidak boleh kosong";

            if (path.Contains(".."))
                return "Path tidak valid";

            string fullPath = Path.GetFullPath(Path.Combine(_basePath, path));

            // 🔒 pastikan tetap di dalam storage
            if (!fullPath.StartsWith(_basePath))
                return "Akses ditolak";

            if (!Directory.Exists(fullPath))
                return "Folder tidak ditemukan";

            // 🔥 cek isi folder
            bool hasFiles = Directory.GetFiles(fullPath).Any();
            bool hasFolders = Directory.GetDirectories(fullPath).Any();

            if (hasFiles || hasFolders)
            {
                return "Folder tidak kosong, tidak bisa dihapus";
            }

            Directory.Delete(fullPath);

            return $"Folder berhasil dihapus: {fullPath}";
        }
    }
}