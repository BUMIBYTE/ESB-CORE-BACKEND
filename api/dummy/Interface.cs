using System.Text.Json;

public interface IRedPayService
{

    Task<object> GetData();
    Task<object> GetDataESB();
    Task<object> PostData(JsonElement request);


}