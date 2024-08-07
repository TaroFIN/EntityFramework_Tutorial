using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pelican.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RollBackOrderHeaderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "OrderHeaders",
                newName: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "OrderHeaders",
                newName: "Id");
        }
    }
}
