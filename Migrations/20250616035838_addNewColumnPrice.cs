using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JapaneseMealReservation.Migrations
{
    /// <inheritdoc />
    public partial class addNewColumnPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Orders",
                newName: "Price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Orders",
                newName: "TotalAmount");
        }
    }
}
