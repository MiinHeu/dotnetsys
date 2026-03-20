using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OwnedPoiId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    MapX = table.Column<double>(type: "float", nullable: false),
                    MapY = table.Column<double>(type: "float", nullable: false),
                    TriggerRadiusMeters = table.Column<double>(type: "float", nullable: false),
                    CooldownSeconds = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioViUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pois", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoiTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoiId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AudioUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoiTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoiTranslations_Pois_PoiId",
                        column: x => x.PoiId,
                        principalTable: "Pois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoiVisitLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoiId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    VisitedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ListenDurationSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoiVisitLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoiVisitLogs_Pois_PoiId",
                        column: x => x.PoiId,
                        principalTable: "Pois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Pois",
                columns: new[] { "Id", "AudioViUrl", "Category", "CooldownSeconds", "CreatedAt", "Description", "ImageUrl", "IsActive", "Latitude", "Longitude", "MapX", "MapY", "Name", "OwnerInfo", "Priority", "TriggerRadiusMeters", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, 0, 60, new DateTime(2026, 3, 20, 13, 19, 52, 780, DateTimeKind.Utc).AddTicks(9431), "Com tam dac trung Sai Gon 30 nam. Suon nuong thom lung, bi ro gion, chan ga beo ngay.", null, true, 10.7531, 106.678, 15.0, 40.0, "Quan Com Tam Ba Ghien", null, 9, 15.0, new DateTime(2026, 3, 20, 13, 19, 52, 780, DateTimeKind.Utc).AddTicks(9434) },
                    { 2, null, 1, 60, new DateTime(2026, 3, 20, 13, 19, 52, 781, DateTimeKind.Utc).AddTicks(2007), "Banh canh cua tuoi boc day, nuoc leo ngot thanh, gan 40 nam phuc vu.", null, true, 10.753299999999999, 106.6781, 30.0, 40.0, "Banh Canh Cua Ba Suong", null, 8, 15.0, new DateTime(2026, 3, 20, 13, 19, 52, 781, DateTimeKind.Utc).AddTicks(2007) },
                    { 3, null, 3, 120, new DateTime(2026, 3, 20, 13, 19, 52, 781, DateTimeKind.Utc).AddTicks(2012), "Khu vuc tap trung hang che, trang miem, banh ngot — ly tuong sau bua an.", null, true, 10.754, 106.6785, 75.0, 40.0, "Khu Che Cuoi Pho", null, 5, 20.0, new DateTime(2026, 3, 20, 13, 19, 52, 781, DateTimeKind.Utc).AddTicks(2012) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pois_Category",
                table: "Pois",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Pois_Latitude_Longitude",
                table: "Pois",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_PoiTranslations_PoiId_LanguageCode",
                table: "PoiTranslations",
                columns: new[] { "PoiId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoiVisitLogs_PoiId",
                table: "PoiVisitLogs",
                column: "PoiId");

            migrationBuilder.CreateIndex(
                name: "IX_PoiVisitLogs_VisitedAt",
                table: "PoiVisitLogs",
                column: "VisitedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "PoiTranslations");

            migrationBuilder.DropTable(
                name: "PoiVisitLogs");

            migrationBuilder.DropTable(
                name: "Pois");
        }
    }
}
