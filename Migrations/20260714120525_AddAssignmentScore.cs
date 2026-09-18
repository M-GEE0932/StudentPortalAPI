using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentPortalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AssignmentScore",
                table: "StudentCourses",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentScore",
                table: "StudentCourses");
        }
    }
}
