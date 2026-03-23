using System.Text.Json;

public interface IJbangService
{
    string CreateFolder(string path);
    string CreateFile(string folderPath, string fileName, string content);
    string ReadFile(string filePath);
    object ReadFolder(string path);
    string DeleteFile(string path);
    string DeleteFolder(string path);
}