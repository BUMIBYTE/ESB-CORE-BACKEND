using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using System.Diagnostics;

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
            var process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-c \"vm_stat\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // parsing vm_stat
            var lines = output.Split('\n');

            double pageSize = 4096; // default macOS
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

        private double ParseVm(string line)
        {
            var value = line.Split(':')[1].Trim().Replace(".", "");
            return double.Parse(value);
        }
    }
}