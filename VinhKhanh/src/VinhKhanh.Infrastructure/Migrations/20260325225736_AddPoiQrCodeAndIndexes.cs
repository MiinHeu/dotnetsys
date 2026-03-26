using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiQrCodeAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TriggerType",
                table: "PoiVisitLogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<int>(
                name: "ContentVersion",
                table: "Pois",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Pois",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ContentVersion", "QrCode" },
                values: new object[] { 1, "VK-POI-001" });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ContentVersion", "QrCode" },
                values: new object[] { 1, "VK-POI-002" });

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ContentVersion", "QrCode" },
                values: new object[] { 1, "VK-POI-003" });

            migrationBuilder.Sql(
                """
                UPDATE "Pois" SET "ContentVersion" = 1 WHERE "ContentVersion" = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Pois_ContentVersion",
                table: "Pois",
                column: "ContentVersion");

            migrationBuilder.CreateIndex(
                name: "IX_Pois_QrCode",
                table: "Pois",
                column: "QrCode",
                unique: true,
                filter: "\"QrCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pois_ContentVersion",
                table: "Pois");

            migrationBuilder.DropIndex(
                name: "IX_Pois_QrCode",
                table: "Pois");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "ContentVersion",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Pois");

            migrationBuilder.AlterColumn<string>(
                name: "TriggerType",
                table: "PoiVisitLogs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }
    }
}
