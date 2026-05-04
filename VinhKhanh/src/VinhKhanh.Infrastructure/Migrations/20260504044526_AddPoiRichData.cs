using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiRichData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Pois",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagesJson",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MenuJson",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingHours",
                table: "Pois",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Pois",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Pois",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "ImagesJson", "MenuJson", "OperatingHours", "PhoneNumber", "Rating", "TagsJson" },
                values: new object[] { "534 Vĩnh Khánh, Phường 10, Quận 4, TP.HCM", "[\"https://cdn.tgdd.vn/Files/2021/08/10/1374136/an-sap-quan-4-voi-quan-oc-oanh-ngon-nuc-tieng-202108101416557675.jpg\"]", "[{\"name\":\"Ốc hương xào bơ tỏi\",\"price\":150000}, {\"name\":\"Càng ghẹ rang muối\",\"price\":180000}, {\"name\":\"Sò điệp nướng mỡ hành\",\"price\":120000}]", "15:00 - 23:00", null, 4.7999999999999998, null });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Address", "ImagesJson", "MenuJson", "OperatingHours", "PhoneNumber", "Rating", "TagsJson" },
                values: new object[] { "Đoạn 1 Vĩnh Khánh, Phường 8, Quận 4, TP.HCM", "[\"https://cdn.tgdd.vn/Files/2021/11/24/1399899/cung-kham-pha-quan-lau-bo-khu-nha-chay-cuc-noi-tieng-tai-quan-4-202111241103031024.jpg\"]", "[{\"name\":\"Lẩu bò thập cẩm\",\"price\":250000}, {\"name\":\"Bò nướng ngói\",\"price\":150000}]", "16:00 - 02:00", null, 4.5, null });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Address", "ImagesJson", "MenuJson", "OperatingHours", "PhoneNumber", "Rating", "TagsJson" },
                values: new object[] { "Ngã 3 Vĩnh Khánh - Hoàng Diệu, Quận 4, TP.HCM", "[\"https://static.vinwonders.com/production/sushi-vien-quan-4-1.jpg\"]", "[{\"name\":\"Sushi cá hồi\",\"price\":10000}, {\"name\":\"Maki lươn nhật\",\"price\":15000}]", "17:00 - 22:30", null, 4.2000000000000002, null });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Address", "ImagesJson", "MenuJson", "OperatingHours", "PhoneNumber", "Rating", "TagsJson" },
                values: new object[] { null, null, null, null, null, 5.0, null });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Address", "ImagesJson", "MenuJson", "OperatingHours", "PhoneNumber", "Rating", "TagsJson" },
                values: new object[] { null, null, null, null, null, 5.0, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "ImagesJson",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "MenuJson",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "OperatingHours",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "Pois");
        }
    }
}
