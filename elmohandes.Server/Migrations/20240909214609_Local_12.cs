using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace elmohandes.Server.Migrations
{
    /// <inheritdoc />
    public partial class Local_12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailUser",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailUser",
                table: "Orders");
        }
    }
}
