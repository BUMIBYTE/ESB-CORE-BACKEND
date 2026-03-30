using System.Text.Json;

public interface IDummyService
{

    Task<object> GetData();
    Task<object> PatchDummyWA(PushAssetModel pushAssetModel);
    Task<object> GetDataECC();
    Task<object> PostData(AssetModel request);


}