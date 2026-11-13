namespace MyRazorXe_Xemay.Data;

public static class MotorcycleAvailabilityStatus
{
    public const string ForSale = "ForSale";
    public const string Incoming = "Incoming";
}

public sealed class Motorcycle
{
    public Motorcycle() { }

    public Motorcycle(int id, string name, string brand, string type, long price, string engine,
        string power, string gear, string image, string code)
    {
        Id = id;
        Name = name;
        Brand = brand;
        Type = type;
        Price = price;
        Engine = engine;
        Power = power;
        Gear = gear;
        Image = image;
        Code = code;
    }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Price { get; set; }
    public string Engine { get; set; } = string.Empty;
    public string Power { get; set; } = string.Empty;
    public string Gear { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHiddenByStock
    {
        get => StockQuantity == 0 && !IsActive;
        set { }
    }
    public string AvailabilityStatus
    {
        get => StockQuantity > 0 ? MotorcycleAvailabilityStatus.ForSale : MotorcycleAvailabilityStatus.Incoming;
        set { }
    }
    public bool CanBePurchased => IsActive
        && AvailabilityStatus == MotorcycleAvailabilityStatus.ForSale
        && StockQuantity > 0;
}
