public class JbangJob
{
    public string Id { get; set; }
    public int ProcessId { get; set; }
    public string Status { get; set; } // running / finished / failed
    public string Output { get; set; }
    public DateTime StartedAt { get; set; }
    public int Port { get; set; } // 👈 tambah ini
    public string FilePath { get; set; }
    public List<string> Logs { get; set; } = new();
}