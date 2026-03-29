using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Runtime.InteropServices;

namespace RepositoryPattern.Services.SystemService
{
    public class SystemService : ISystemService
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly IMongoCollection<BsonDocument> _collectionEsb;
        private readonly IMongoCollection<BsonDocument> _collectionSAP;


        public SystemService(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            var database = mongoClient.GetDatabase("primakom");
            _collection = database.GetCollection<BsonDocument>("primakomSH");
            _collectionEsb = database.GetCollection<BsonDocument>("primakomESB");
            _collectionSAP = database.GetCollection<BsonDocument>("primakomSAP");


        }

        public async Task<CpuInfo> GetCpuDetail()
        {
            var startTime = DateTime.UtcNow;

            var startCpu = Process.GetProcesses().Sum(p =>
            {
                try { return p.TotalProcessorTime.TotalMilliseconds; }
                catch { return 0; }
            });

            await Task.Delay(500);

            var endTime = DateTime.UtcNow;

            var endCpu = Process.GetProcesses().Sum(p =>
            {
                try { return p.TotalProcessorTime.TotalMilliseconds; }
                catch { return 0; }
            });

            var cpuUsedMs = endCpu - startCpu;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds * Environment.ProcessorCount;

            double used = (cpuUsedMs / totalMsPassed) * 100;

            if (used < 0) used = 0;
            if (used > 100) used = 100;

            double idle = 100 - used;

            return new CpuInfo
            {
                Used = Math.Round(used, 2),
                Idle = Math.Round(idle, 2),
                Available = Math.Round(idle, 2) // sama dengan idle
            };
        }

        public async Task<MemoryInfo> GetMemoryDetail()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return await GetMacMemory();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return await GetLinuxMemory();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await GetWindowsMemory();

            throw new NotSupportedException("OS tidak didukung");
        }

        public async Task<StorageInfo> GetStorageDetail()
        {
            DriveInfo drive;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.IsReady && d.Name == "C:\\");
            }
            else
            {
                drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.IsReady && d.Name == "/");
            }

            if (drive == null)
                throw new Exception("Disk tidak ditemukan");

            double total = drive.TotalSize / 1024.0 / 1024.0 / 1024.0; // GB
            double free = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0; // GB
            double used = total - free;

            return new StorageInfo
            {
                Total = Math.Round(total, 2),
                Used = Math.Round(used, 2),
                Free = Math.Round(free, 2),
                Available = Math.Round(free, 2)
            };
        }

        private async Task<MemoryInfo> GetMacMemory()
        {
            var output = await RunCommand("/bin/bash", "-c \"vm_stat\"");

            var lines = output.Split('\n');

            double pageSize = 4096;
            double free = 0;
            double active = 0;
            double inactive = 0;
            double wired = 0;

            foreach (var line in lines)
            {
                if (line.Contains("page size of"))
                {
                    var parts = line.Split(' ');
                    pageSize = double.Parse(parts[7]);
                }
                if (line.StartsWith("Pages free"))
                    free = ParseVm(line);
                if (line.StartsWith("Pages active"))
                    active = ParseVm(line);
                if (line.StartsWith("Pages inactive"))
                    inactive = ParseVm(line);
                if (line.StartsWith("Pages wired"))
                    wired = ParseVm(line);
            }

            double total = (free + active + inactive + wired) * pageSize / 1024 / 1024;
            double used = (active + wired) * pageSize / 1024 / 1024;
            double freeMb = free * pageSize / 1024 / 1024;
            double available = (free + inactive) * pageSize / 1024 / 1024;

            return new MemoryInfo
            {
                Total = Math.Round(total, 2),
                Used = Math.Round(used, 2),
                Free = Math.Round(freeMb, 2),
                Available = Math.Round(available, 2)
            };
        }

        private async Task<MemoryInfo> GetLinuxMemory()
        {
            var output = await RunCommand("/bin/bash", "-c \"cat /proc/meminfo\"");

            double totalKb = ParseMeminfo(output, "MemTotal");
            double freeKb = ParseMeminfo(output, "MemFree");
            double availableKb = ParseMeminfo(output, "MemAvailable");

            double totalMb = totalKb / 1024;
            double freeMb = freeKb / 1024;
            double availableMb = availableKb / 1024;
            double usedMb = totalMb - availableMb;

            return new MemoryInfo
            {
                Total = Math.Round(totalMb, 2),
                Used = Math.Round(usedMb, 2),
                Free = Math.Round(freeMb, 2),
                Available = Math.Round(availableMb, 2)
            };
        }

        private async Task<MemoryInfo> GetWindowsMemory()
        {
            var output = await RunCommand("wmic", "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value");

            double totalKb = ParseValue(output, "TotalVisibleMemorySize");
            double freeKb = ParseValue(output, "FreePhysicalMemory");

            double totalMb = totalKb / 1024;
            double freeMb = freeKb / 1024;
            double usedMb = totalMb - freeMb;

            return new MemoryInfo
            {
                Total = Math.Round(totalMb, 2),
                Used = Math.Round(usedMb, 2),
                Free = Math.Round(freeMb, 2),
                Available = Math.Round(freeMb, 2)
            };
        }

        private async Task<string> RunCommand(string fileName, string args)
        {
            var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = args;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();

            string result = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            return result;
        }

        private double ParseVm(string line)
        {
            var parts = line.Split(':');
            return double.Parse(parts[1].Trim().Replace(".", ""));
        }

        private double ParseMeminfo(string text, string key)
        {
            var line = text.Split('\n')
                           .FirstOrDefault(x => x.StartsWith(key));

            if (line == null) return 0;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return double.Parse(parts[1]);
        }

        private double ParseValue(string text, string key)
        {
            var line = text.Split('\n')
                           .FirstOrDefault(x => x.StartsWith(key));

            if (line == null) return 0;

            return double.Parse(line.Split('=')[1].Trim());
        }

        public ServerInfo GetServerInfo()
        {
            return new ServerInfo
            {
                OperatingSystem = GetOs(),
                IpAddress = GetIp(),
                CpuModel = GetCpuModel(),
                Uptime = GetUptime(),
                NetworkPeak = GetNetworkUsage()
            };
        }

        private string RunCommand(string cmd)
        {
            var process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{cmd}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return result.Trim();
        }

        private string GetOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return RunCommand("lsb_release -d | cut -f2");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return RunCommand("sw_vers -productName && sw_vers -productVersion");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Environment.OSVersion.ToString();

            return "Unknown OS";
        }

        private string GetCpuModel()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return RunCommand("cat /proc/cpuinfo | grep 'model name' | head -1 | cut -d ':' -f2");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return RunCommand("sysctl -n machdep.cpu.brand_string");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return RunCommand("wmic cpu get name");

            return "Unknown CPU";
        }

        private string GetUptime()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return RunCommand("uptime -p");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return RunCommand("uptime");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return TimeSpan.FromMilliseconds(Environment.TickCount64).ToString();

            return "Unknown";
        }

        private string GetIp()
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.ToString();
        }

        private string GetNetworkUsage()
        {
            // simple snapshot (bukan realtime peak)
            var ni = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                     n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            if (ni == null) return "N/A";

            var stats = ni.GetIPv4Statistics();

            long totalBytes = stats.BytesSent + stats.BytesReceived;

            double mb = totalBytes / 1024.0 / 1024.0;

            return $"{Math.Round(mb, 2)} MB (total)";
        }
    }
}