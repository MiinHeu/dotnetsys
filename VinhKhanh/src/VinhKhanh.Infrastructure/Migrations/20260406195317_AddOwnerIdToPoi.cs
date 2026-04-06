using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerIdToPoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnedPoiId",
                table: "AppUsers");

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Pois",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 1,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 2,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Pois",
                keyColumn: "Id",
                keyValue: 3,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Pois_OwnerUserId",
                table: "Pois",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pois_AppUsers_OwnerUserId",
                table: "Pois",
                column: "OwnerUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pois_AppUsers_OwnerUserId",
                table: "Pois");

            migrationBuilder.DropIndex(
                name: "IX_Pois_OwnerUserId",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Pois");

            migrationBuilder.AddColumn<int>(
                name: "OwnedPoiId",
                table: "AppUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "OwnedPoiId",
                value: null);
        }
    }
}
