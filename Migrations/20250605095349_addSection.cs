using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JapaneseMealReservation.Migrations
{
    /// <inheritdoc />
    public partial class addSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "section",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "section",
                table: "users");
        }
    }
}
