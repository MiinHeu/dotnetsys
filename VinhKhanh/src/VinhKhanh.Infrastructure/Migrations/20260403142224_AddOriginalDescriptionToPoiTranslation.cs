using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalDescriptionToPoiTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalDescription",
                table: "PoiTranslations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Id", "CreatedAt", "IsActive", "OwnedPoiId", "PasswordHash", "Role", "Username" },
                values: new object[] { 1, new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "$2a$11$PBSPXvfmAZ.W8yyJfGlYOOqiMEgPBBCJOmYDGrqp8qJW3nDEFU.hm", "Admin", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "OriginalDescription",
                table: "PoiTranslations");
        }
    }
}
