using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentPortalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNoticeIdToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NoticeId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NoticeId",
                table: "Notifications",
                column: "NoticeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Notices_NoticeId",
                table: "Notifications",
                column: "NoticeId",
                principalTable: "Notices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Notices_NoticeId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_NoticeId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NoticeId",
                table: "Notifications");
        }
    }
}
