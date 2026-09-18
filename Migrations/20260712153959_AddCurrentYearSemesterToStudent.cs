using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentPortalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentYearSemesterToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSemester",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentYear",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSemester",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CurrentYear",
                table: "Students");
        }
    }
}
