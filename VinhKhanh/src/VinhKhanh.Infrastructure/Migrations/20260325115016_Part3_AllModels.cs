using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Part3_AllModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Tours",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "TourStops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StayMinutes",
                table: "TourStops",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tours",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "Tours",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tours",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tours",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Pois",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Pois",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Pois",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioViUrl",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Pois",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CooldownSeconds",
                table: "Pois",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Pois",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Pois",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "MapX",
                table: "Pois",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MapY",
                table: "Pois",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "OwnerInfo",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Pois",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TriggerRadiusMeters",
                table: "Pois",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Pois",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "AppHistoryLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    PoiId = table.Column<int>(type: "integer", nullable: true),
                    TourId = table.Column<int>(type: "integer", nullable: true),
                    LanguageCode = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppHistoryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OwnedPoiId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovementLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    AccuracyMeters = table.Column<float>(type: "real", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovementLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoiTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PoiId = table.Column<int>(type: "integer", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AudioUrl = table.Column<string>(type: "text", nullable: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PoiId = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VisitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ListenDurationSeconds = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "TourTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TourId = table.Column<int>(type: "integer", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourTranslations_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Pois",
                columns: new[] { "Id", "AudioViUrl", "Category", "CooldownSeconds", "CreatedAt", "Description", "ImageUrl", "IsActive", "Latitude", "Longitude", "MapX", "MapY", "Name", "OwnerInfo", "Priority", "TriggerRadiusMeters", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, 0, 60, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Com tam dac trung Sai Gon 30 nam.", null, true, 10.7531, 106.678, 15.0, 40.0, "Quan Com Tam Ba Ghien", null, 9, 15.0, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, 1, 60, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Banh canh cua tuoi boc day, 40 nam.", null, true, 10.753299999999999, 106.6781, 30.0, 40.0, "Banh Canh Cua Ba Suong", null, 8, 15.0, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, null, 3, 120, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Khu vuc tap trung hang che.", null, true, 10.754, 106.6785, 75.0, 40.0, "Khu Che Cuoi Pho", null, 5, 20.0, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Tours",
                columns: new[] { "Id", "CreatedAt", "Description", "EstimatedMinutes", "IsActive", "Name", "ThumbnailUrl", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Com tam -> Banh canh -> Che", 60, true, "Tour Am Thuc 1 Gio Vinh Khanh", null, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "TourStops",
                columns: new[] { "Id", "Note", "PoiId", "StayMinutes", "StopOrder", "TourId" },
                values: new object[,]
                {
                    { 1, null, 1, 20, 1, 1 },
                    { 2, null, 2, 20, 2, 1 },
                    { 3, null, 3, 20, 3, 1 }
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
                name: "IX_AppHistoryLogs_CreatedAt",
                table: "AppHistoryLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppHistoryLogs_EventType",
                table: "AppHistoryLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_RecordedAt",
                table: "MovementLogs",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MovementLogs_SessionId",
                table: "MovementLogs",
                column: "SessionId");

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

            migrationBuilder.CreateIndex(
                name: "IX_TourTranslations_TourId",
                table: "TourTranslations",
                column: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppHistoryLogs");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "MovementLogs");

            migrationBuilder.DropTable(
                name: "PoiTranslations");

            migrationBuilder.DropTable(
                name: "PoiVisitLogs");

            migrationBuilder.DropTable(
                name: "TourTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Pois_Category",
                table: "Pois");

            migrationBuilder.DropIndex(
                name: "IX_Pois_Latitude_Longitude",
                table: "Pois");

            migrationBuilder.DeleteData(
                table: "TourStops",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TourStops",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TourStops",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Tours",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "Note",
                table: "TourStops");

            migrationBuilder.DropColumn(
                name: "StayMinutes",
                table: "TourStops");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "AudioViUrl",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "CooldownSeconds",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "MapX",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "MapY",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "OwnerInfo",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TriggerRadiusMeters",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Pois");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Tours",
                newName: "Title");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Pois",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Pois",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Pois",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
