using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentRentalSystem.Infrastructure.Migrations
{
    public partial class AddImagePathToApartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Apartments",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Apartments");
        }
    }
}
