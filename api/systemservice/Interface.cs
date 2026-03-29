using System.Text.Json;

public interface ISystemService
{

    Task<CpuInfo> GetCpuDetail();
    Task<MemoryInfo> GetMemoryDetail();
    Task<StorageInfo> GetStorageDetail();
    ServerInfo GetServerInfo();


}