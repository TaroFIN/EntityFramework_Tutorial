using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pelican.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductToDbAndSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AlterColumn<string>(
            //    name: "Name",
            //    table: "Categories",
            //    type: "nvarchar(30)",
            //    maxLength: 30,
            //    nullable: false,
            //    oldClrType: typeof(string),
            //    oldType: "varchar(30)",
            //    oldMaxLength: 30);

            //migrationBuilder.AlterColumn<int>(
            //    name: "Id",
            //    table: "Categories",
            //    type: "int",
            //    nullable: false,
            //    oldClrType: typeof(int),
            //    oldType: "int")
            //    .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ISBN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ListPrice = table.Column<double>(type: "float", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: false),
                    Price50 = table.Column<double>(type: "float", nullable: false),
                    Price100 = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Author", "ISBN", "ListPrice", "Price", "Price100", "Price50", "Title" },
                values: new object[] { 1, "Stan Lee", "ISBN 91-4335-5587", 100.0, 100.0, 90.0, 95.0, "Iron Man" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            //migrationBuilder.AlterColumn<string>(
            //    name: "Name",
            //    table: "Categories",
            //    type: "varchar(30)",
            //    maxLength: 30,
            //    nullable: false,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(30)",
            //    oldMaxLength: 30);

            //migrationBuilder.AlterColumn<int>(
            //    name: "Id",
            //    table: "Categories",
            //    type: "int",
            //    nullable: false,
            //    oldClrType: typeof(int),
            //    oldType: "int")
            //    .OldAnnotation("SqlServer:Identity", "1, 1");
        }
    }
}
