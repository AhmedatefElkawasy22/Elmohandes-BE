using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace elmohandes.Server.Migrations
{
    /// <inheritdoc />
    public partial class Loacl_7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "ProductImages");

            migrationBuilder.AddColumn<string>(
                name: "PathImage",
                table: "ProductImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PathImage",
                table: "ProductImages");

            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "ProductImages",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
