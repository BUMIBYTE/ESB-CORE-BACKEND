

public class CreateMemberOrder
{
    public string? IdUser { get; set; }
    public string? FullName { get; set; }
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class Asset
{
    public string COMPANYCODE { get; set; }
    public string ASSETCLASS { get; set; }
    public string DESCRIPT { get; set; }
    public string COSTCENTER { get; set; }
    public string PLANT { get; set; }
    public string ULIFE_BOOK { get; set; }
    public string ULIFE_TAX { get; set; }
    public string ULIFE_IFRS { get; set; }
    public long ASSET_x0020_Number { get; set; }
}

public class AssetModel
{
    public string COMPANYCODE { get; set; }
    public string ASSETCLASS { get; set; }
    public string DESCRIPT { get; set; }
    public string COSTCENTER { get; set; }
    public string PLANT { get; set; }
    public string ULIFE_BOOK { get; set; }
    public string ULIFE_TAX { get; set; }
    public string ULIFE_IFRS { get; set; }
}

public class PushAssetModel
{
    public Metadata __metadata { get; set; }

    public string Price_x002f_Original_x0020_Value { get; set; }
}

public class Metadata
{
    public string type { get; set; }
}