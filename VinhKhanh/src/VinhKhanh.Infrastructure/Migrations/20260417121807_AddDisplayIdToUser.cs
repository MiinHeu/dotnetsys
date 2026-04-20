using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourTranslations_TourId",
                table: "TourTranslations");

            migrationBuilder.AddColumn<string>(
                name: "DisplayId",
                table: "AppUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourTranslations_TourId_LanguageCode",
                table: "TourTranslations",
                columns: new[] { "TourId", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourTranslations_TourId_LanguageCode",
                table: "TourTranslations");

            migrationBuilder.DropColumn(
                name: "DisplayId",
                table: "AppUsers");

            migrationBuilder.CreateIndex(
                name: "IX_TourTranslations_TourId",
                table: "TourTranslations",
                column: "TourId");
        }
    }
}
