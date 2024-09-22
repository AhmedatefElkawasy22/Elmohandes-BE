using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace elmohandes.Server.Migrations
{
    /// <inheritdoc />
    public partial class local_13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CountOfProduct",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountOfProduct",
                table: "OrderItems");
        }
    }
}
