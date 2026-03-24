using System.Net.NetworkInformation;

public class PortManager
{
    private readonly HashSet<int> _usedPorts = new();
    private readonly object _lock = new();

    private int _startPort = 3000;

    // 🔥 ambil port otomatis
    public int GetNextAvailablePort()
    {
        lock (_lock)
        {
            int port = _startPort;

            while (_usedPorts.Contains(port) || !IsPortAvailable(port))
            {
                port++;
            }

            _usedPorts.Add(port);
            _startPort = port + 1;

            return port;
        }
    }

    // 🔥 reserve manual port
    public int ReservePort(int port)
    {
        lock (_lock)
        {
            if (_usedPorts.Contains(port))
                throw new Exception($"Port {port} sudah dipakai (internal)");

            if (!IsPortAvailable(port))
                throw new Exception($"Port {port} sudah dipakai (OS)");

            _usedPorts.Add(port);
            return port;
        }
    }

    // 🔥 release port saat job stop
    public void ReleasePort(int port)
    {
        lock (_lock)
        {
            _usedPorts.Remove(port);
        }
    }

    // 🔥 cek OS
    private bool IsPortAvailable(int port)
    {
        return !IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Any(p => p.Port == port);
    }
}