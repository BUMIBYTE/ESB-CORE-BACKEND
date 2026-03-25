using System.Text.Json;

public interface IJbangService
{
    string CreateFolder(string path);
    List<FolderItem> ReadRootFolders();
    string CreateFile(string folderPath, string fileName, string content);
    FileDetail ReadFile(string filePath);

    string UpdateFile(string filePath, string newContent);
    object ReadFolder(string path);
    string DeleteFile(string path);
    string DeleteFolder(string path);

    string RunJbang(string filePath, int? port = null);
    string StopJob(string jobId);
    string ResumeJob(string jobId);
    JbangJob GetStatus(string jobId);
    List<JbangJob> GetAllJobs();
}