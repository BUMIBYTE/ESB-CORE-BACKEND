public class CpuInfo
{
    public double Used { get; set; }
    public double Idle { get; set; }
    public double Available { get; set; }
}

public class MemoryInfo
{
    public double Total { get; set; }
    public double Used { get; set; }
    public double Free { get; set; }
    public double Available { get; set; }
}

public class StorageInfo
{
    public double Total { get; set; }
    public double Used { get; set; }
    public double Free { get; set; }
    public double Available { get; set; }
}

public class ServerInfo
{
    public string OperatingSystem { get; set; }
    public string IpAddress { get; set; }
    public string CpuModel { get; set; }
    public string Uptime { get; set; }
    public string NetworkPeak { get; set; }
}