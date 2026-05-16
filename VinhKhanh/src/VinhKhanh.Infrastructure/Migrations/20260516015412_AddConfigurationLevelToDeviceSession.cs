using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationLevelToDeviceSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "DeviceSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfigurationLevel",
                table: "DeviceSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "DeviceSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "DeviceSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "DeviceSessions");

            migrationBuilder.DropColumn(
                name: "ConfigurationLevel",
                table: "DeviceSessions");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "DeviceSessions");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "DeviceSessions");
        }
    }
}
