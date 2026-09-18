using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentPortalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddReadPropertyToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsRead",
                table: "Notifications",
                newName: "Read");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Read",
                table: "Notifications",
                newName: "IsRead");
        }
    }
}
