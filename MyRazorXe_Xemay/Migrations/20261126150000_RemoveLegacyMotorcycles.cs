using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyRazorXe_Xemay.Data;

#nullable disable

namespace MyRazorXe_Xemay.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261126150000_RemoveLegacyMotorcycles")]
public sealed class RemoveLegacyMotorcycles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.Motorcycles', N'U') IS NOT NULL
            BEGIN
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Motorcycles AS oldBike
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM dbo.XeMays AS currentBike
                        WHERE currentBike.MaXe = oldBike.Code
                    )
                )
                    THROW 51010, 'Cannot remove dbo.Motorcycles: at least one legacy motorcycle has no matching row in dbo.XeMays.', 1;

                DROP TABLE dbo.Motorcycles;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.Motorcycles', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Motorcycles
                (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Motorcycles PRIMARY KEY,
                    Name nvarchar(max) NOT NULL,
                    Brand nvarchar(max) NOT NULL,
                    Type nvarchar(max) NOT NULL,
                    Price bigint NOT NULL,
                    Engine nvarchar(max) NOT NULL,
                    Power nvarchar(max) NOT NULL,
                    Gear nvarchar(max) NOT NULL,
                    Image nvarchar(max) NOT NULL,
                    Code nvarchar(40) NOT NULL,
                    StockQuantity int NOT NULL,
                    IsActive bit NOT NULL,
                    IsHiddenByStock bit NOT NULL CONSTRAINT DF_Motorcycles_IsHiddenByStock DEFAULT 0,
                    AvailabilityStatus nvarchar(20) NOT NULL CONSTRAINT DF_Motorcycles_AvailabilityStatus DEFAULT N'ForSale'
                );

                SET IDENTITY_INSERT dbo.Motorcycles ON;
                INSERT dbo.Motorcycles
                    (Id, Name, Brand, Type, Price, Engine, Power, Gear, Image, Code,
                     StockQuantity, IsActive, IsHiddenByStock, AvailabilityStatus)
                SELECT Id, LEFT(TenXe, 450), LEFT(HangXe, 450), LEFT(LoaiXe, 450),
                       CONVERT(bigint, GiaBan), LEFT(MoTa, 450), N'Updating', N'Updating',
                       HinhAnh, MaXe, SoLuong, DangKinhDoanh, 0,
                       CASE WHEN SoLuong > 0 THEN N'ForSale' ELSE N'Incoming' END
                FROM dbo.XeMays;
                SET IDENTITY_INSERT dbo.Motorcycles OFF;
            END;
            """);
    }
}
