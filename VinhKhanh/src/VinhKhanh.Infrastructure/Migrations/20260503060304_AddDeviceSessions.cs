using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AppUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AppUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    DeviceModel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DevicePlatform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OsVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AppVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PoisVisited = table.Column<int>(type: "integer", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    LanguageUsed = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsReturning = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSessions", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "CooldownSeconds", "CreatedAt", "Description", "Name", "Priority", "TriggerRadiusMeters", "UpdatedAt" },
                values: new object[] { 2, 30, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Quan oc noi tieng nhat khu vuc voi mon oc huong xot trung muoi.", "Oc Oanh Vinh Khanh", 10, 20.0, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Category", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[] { 5, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Lau bo truyen thong voi nuoc dung dam da, thit bo tuoi ngon.", "Lau Bo Khu Nha Chay", new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Category", "CreatedAt", "Description", "Name", "TriggerRadiusMeters", "UpdatedAt" },
                values: new object[] { 5, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Sushi binh dan nhung chat luong, thu hut rat dong gioi tre.", "Sushi Vien Vinh Khanh", 15.0, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Pois",
                columns: new[] { "Id", "AudioViUrl", "Category", "ContentVersion", "CooldownSeconds", "CreatedAt", "Description", "ImageUrl", "IsActive", "Latitude", "Longitude", "MapX", "MapY", "Name", "OwnerInfo", "OwnerUserId", "Priority", "QrCode", "TriggerRadiusMeters", "UpdatedAt" },
                values: new object[,]
                {
                    { 4, null, 0, 1, 60, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Com tam dem ngon nhat khu vuc, suon nuong thom phuc.", null, true, 10.753500000000001, 106.6782, 45.0, 40.0, "Com Tam Tu Map", null, null, 0, "VK-POI-004", 10.0, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, null, 3, 1, 60, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Che khuc bach va cac loai che giai nhiet truyen thong.", null, true, 10.7538, 106.6784, 60.0, 40.0, "Che Hien Khanh", null, null, 3, "VK-POI-005", 15.0, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.UpdateData(
                table: "Tours",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSessions_DevicePlatform",
                table: "DeviceSessions",
                column: "DevicePlatform");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSessions_SessionId",
                table: "DeviceSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSessions_StartedAt",
                table: "DeviceSessions",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceSessions");

            migrationBuilder.DeleteData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AppUsers");

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "CooldownSeconds", "CreatedAt", "Description", "Name", "Priority", "TriggerRadiusMeters", "UpdatedAt" },
                values: new object[] { 0, 60, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Com tam dac trung Sai Gon 30 nam.", "Quan Com Tam Ba Ghien", 9, 15.0, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Category", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Banh canh cua tuoi boc day, 40 nam.", "Banh Canh Cua Ba Suong", new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Category", "CreatedAt", "Description", "Name", "TriggerRadiusMeters", "UpdatedAt" },
                values: new object[] { 3, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Khu vuc tap trung hang che.", "Khu Che Cuoi Pho", 20.0, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Tours",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
