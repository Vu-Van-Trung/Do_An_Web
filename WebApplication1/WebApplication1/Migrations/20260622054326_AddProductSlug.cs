using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Products",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            // Generate slugs for existing products based on their Name
            migrationBuilder.Sql(@"
                UPDATE Products 
                SET Slug = LOWER(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(Name, ' ', '-'),
                                    '.', ''),
                                '/', '-'),
                            '''', ''),
                        ',', ''),
                    '--', '-')
                ) + '-' + CAST(Id AS NVARCHAR(10))
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Products");
        }
    }
}
