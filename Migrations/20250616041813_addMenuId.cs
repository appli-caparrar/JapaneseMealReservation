using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JapaneseMealReservation.Migrations
{
    /// <inheritdoc />
    public partial class addMenuId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "Menu_Id",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Menu_Id",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalAmount",
                table: "Orders",
                type: "integer",
                nullable: true);
        }
    }
}
