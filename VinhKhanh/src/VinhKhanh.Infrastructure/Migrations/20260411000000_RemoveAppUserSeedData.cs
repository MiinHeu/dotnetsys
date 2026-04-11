using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAppUserSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the hardcoded seed AppUser inserted by previous HasData().
            // DbSeeder now manages user creation at runtime — so the seeded row
            // with the stale BCrypt hash is no longer needed.
            migrationBuilder.DeleteData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-insert the seed user for rollback.
            // NOTE: the hash will NOT match across machines; DbSeeder is the
            // canonical source for credential management.
            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Id", "Username", "PasswordHash", "Role", "IsActive", "CreatedAt" },
                values: new object[] { 1, "admin", "$2a$11$PBSPXvfmAZ.W8yyJfGlYOOqiMEgPBBCJOmYDGrqp8qJW3nDEFU.hm", "Admin", true, new System.DateTime(2026, 3, 25, 0, 0, 0, System.DateTimeKind.Utc) });
        }
    }
}
