using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace RepositoryPattern.Services.JbangService
{
    public class JbangService : IJbangService
    {
        private readonly string _basePath;
        private static ConcurrentDictionary<string, JbangJob> _jobs = new();

        private static int _currentPort = 3000;
        private static readonly object _lock = new();
        private readonly PortManager _portManager;


        public JbangService(IConfiguration configuration, PortManager portManager)
        {
            var mongoClient = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            var database = mongoClient.GetDatabase("primakom");
            _portManager = portManager;

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

        public List<FolderItem> ReadRootFolders()
        {
            string fullPath = _basePath;

            if (!Directory.Exists(fullPath))
                throw new Exception("Base folder tidak ditemukan");

            var folders = Directory.GetDirectories(fullPath)
                .Select(d => new FolderItem
                {
                    Name = Path.GetFileName(d),
                    Type = "folder",
                    Size = null,
                    CreatedAt = Directory.GetCreationTime(d)
                })
                .OrderBy(x => x.Name)
                .ToList();

            return folders;
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
        public FileDetail ReadFile(string filePath)
        {
            if (filePath.Contains(".."))
                throw new Exception("Path tidak valid");

            string fullPath = Path.GetFullPath(Path.Combine(_basePath, filePath));

            if (!fullPath.StartsWith(_basePath))
                throw new Exception("Akses ditolak");

            if (!File.Exists(fullPath))
                throw new Exception("File tidak ditemukan");

            return new FileDetail
            {
                FileName = Path.GetFileName(fullPath),
                Path = filePath,
                Content = File.ReadAllText(fullPath)
            };
        }

        // 🔥 UPDATE FILE + VERSIONING
        public string UpdateFile(string filePath, string newContent)
        {
            ValidatePath(filePath);

            string fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
                throw new Exception("File tidak ditemukan");

            // 🔥 ambil info file
            string fileName = Path.GetFileName(filePath);
            string folderPath = Path.GetDirectoryName(filePath) ?? "";

            // 🔥 version folder
            string versionFolder = Path.Combine(_basePath, ".versions", folderPath, fileName);

            if (!Directory.Exists(versionFolder))
            {
                Directory.CreateDirectory(versionFolder);
            }

            // 🔥 backup lama
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFile = Path.Combine(versionFolder, $"{timestamp}.bak");

            File.Copy(fullPath, backupFile, true);

            // 🔥 tulis content baru
            File.WriteAllText(fullPath, newContent);

            return $"File berhasil diupdate (backup: {timestamp})";
        }

        // 🔥 LIST VERSION
        public List<string> GetFileVersions(string filePath)
        {
            ValidatePath(filePath);

            string fileName = Path.GetFileName(filePath);
            string folderPath = Path.GetDirectoryName(filePath) ?? "";

            string versionFolder = Path.Combine(_basePath, ".versions", folderPath, fileName);

            if (!Directory.Exists(versionFolder))
                return new List<string>();

            return Directory.GetFiles(versionFolder)
                .Select(f => Path.GetFileName(f))
                .OrderByDescending(x => x)
                .ToList();
        }

        // 🔥 RESTORE VERSION (UNDO)
        public string RestoreVersion(string filePath, string versionFile)
        {
            ValidatePath(filePath);

            string fullPath = GetFullPath(filePath);

            string fileName = Path.GetFileName(filePath);
            string folderPath = Path.GetDirectoryName(filePath) ?? "";

            string versionPath = Path.Combine(_basePath, ".versions", folderPath, fileName, versionFile);

            if (!File.Exists(versionPath))
                throw new Exception("Version tidak ditemukan");

            File.Copy(versionPath, fullPath, true);

            return "File berhasil di-restore";
        }

        public FileDetail ReadVersion(string filePath, string versionFile)
        {
            ValidatePath(filePath);

            string fileName = Path.GetFileName(filePath);
            string folderPath = Path.GetDirectoryName(filePath) ?? "";

            string versionPath = Path.Combine(_basePath, ".versions", folderPath, fileName, versionFile);

            if (!File.Exists(versionPath))
                throw new Exception("Version tidak ditemukan");

            return new FileDetail
            {
                FileName = fileName,
                Path = filePath,
                Content = File.ReadAllText(versionPath)
            };
        }

        // 🔒 VALIDATION
        private void ValidatePath(string filePath)
        {
            if (filePath.Contains(".."))
                throw new Exception("Path tidak valid");
        }

        private string GetFullPath(string filePath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(_basePath, filePath));

            if (!fullPath.StartsWith(_basePath))
                throw new Exception("Akses ditolak");

            return fullPath;
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

        public string RunJbang(string filePath, int? portInput = null)
        {
            var jobId = Guid.NewGuid().ToString();

            string fullPath = Path.GetFullPath(Path.Combine(_basePath, filePath));

            if (!File.Exists(fullPath))
                throw new Exception($"File tidak ditemukan: {fullPath}");

            int port = portInput.HasValue
                ? _portManager.ReservePort(portInput.Value)
                : _portManager.GetNextAvailablePort();

            var process = new Process();

            process.StartInfo.FileName = "jbang";
            process.StartInfo.Arguments =
                $"camel@apache/camel run \"{fullPath}\" " +
                $"--runtime=main " +
                $"--dep=org.apache.camel:camel-yaml-dsl " +
                $"--dep=org.apache.camel:camel-kafka " +
                $"--dep=org.apache.camel:camel-http " +
                $"--property camel.main.restConfiguration.port={port}";

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            var job = new JbangJob
            {
                Id = jobId,
                Status = "starting",
                StartedAt = DateTime.Now,
                Port = port,
                FilePath = filePath,
                Logs = new List<string>()
            };

            _jobs[jobId] = job;

            // 🔥 HANDLE STDOUT
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    lock (job)
                    {
                        job.Logs.Add(args.Data);
                    }
                }
            };

            // 🔥 HANDLE STDERR
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    lock (job)
                    {
                        job.Logs.Add("[ERROR] " + args.Data);
                    }
                }
            };

            process.Start();
            job.ProcessId = process.Id;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Task.Run(async () =>
            {
                try
                {
                    job.Status = "running";

                    await process.WaitForExitAsync();

                    job.Status = process.ExitCode == 0 ? "finished" : "failed";
                }
                catch (Exception ex)
                {
                    job.Status = "failed";
                    job.Logs.Add("[EXCEPTION] " + ex.Message);
                }
                finally
                {
                    _portManager.ReleasePort(job.Port);
                }
            });

            return jobId;
        }

        public List<string> GetLogs(string jobId)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
                throw new Exception("Job tidak ditemukan");

            return [.. job.Logs.TakeLast(100)];
        }

        private bool IsPortAvailable(int port)
        {
            return !System.Net.NetworkInformation.IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(p => p.Port == port);
        }

        private int GetNextAvailablePort()
        {
            int port = 3000;

            while (!IsPortAvailable(port))
            {
                port++;
            }

            return port;
        }

        public JbangJob GetStatus(string jobId)
        {
            if (_jobs.ContainsKey(jobId))
            {
                return _jobs[jobId];
            }

            return null;
        }

        public string StopJob(string jobId)
        {
            if (!_jobs.ContainsKey(jobId))
                return "Job tidak ditemukan";

            var job = _jobs[jobId];

            try
            {
                var process = Process.GetProcessById(job.ProcessId);

                if (!process.HasExited)
                    process.Kill();

                job.Status = "stopped";

                // 🔥 release port
                _portManager.ReleasePort(job.Port);

                _jobs.TryRemove(jobId, out _);

                return $"Job dihentikan, port {job.Port} dibebaskan";
            }
            catch
            {
                _portManager.ReleasePort(job.Port);
                _jobs.TryRemove(jobId, out _);

                return "Process sudah selesai";
            }
        }

        public string ResumeJob(string jobId)
        {
            if (!_jobs.ContainsKey(jobId))
                return "Job tidak ditemukan";

            var oldJob = _jobs[jobId];

            // 🔥 jalankan ulang dengan file yang sama
            return RunJbang(oldJob.FilePath);
        }

        public List<JbangJob> GetAllJobs()
        {
            return _jobs.Values
                .OrderByDescending(x => x.StartedAt)
                .ToList();
        }

        private int GetNextPort()
        {
            lock (_lock)
            {
                return _currentPort++;
            }
        }

        
    }
}