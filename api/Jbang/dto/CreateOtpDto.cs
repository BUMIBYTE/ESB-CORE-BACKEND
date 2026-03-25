public class FolderItem
{
    public string Name { get; set; }
    public string Type { get; set; } // file / folder
    public long? Size { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FileDetail
{
    public string FileName { get; set; }
    public string Path { get; set; }
    public string Content { get; set; }
}