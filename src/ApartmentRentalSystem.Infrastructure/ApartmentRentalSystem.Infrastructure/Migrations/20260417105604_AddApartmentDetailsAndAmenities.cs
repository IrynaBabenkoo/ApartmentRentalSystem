using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 

namespace ApartmentRentalSystem.Infrastructure.Migrations
{
    public partial class AddApartmentDetailsAndAmenities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Area",
                table: "Apartments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Apartments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Amenities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApartmentAmenities",
                columns: table => new
                {
                    ApartmentId = table.Column<int>(type: "integer", nullable: false),
                    AmenityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApartmentAmenities", x => new { x.ApartmentId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK_ApartmentAmenities_Amenities_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApartmentAmenities_Apartments_ApartmentId",
                        column: x => x.ApartmentId,
                        principalTable: "Apartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Wi-Fi" },
                    { 2, "Парковка" },
                    { 3, "Кухня" },
                    { 4, "Кондиціонер" },
                    { 5, "Пральна машина" },
                    { 6, "Балкон" },
                    { 7, "Телевізор" },
                    { 8, "Дозволено з тваринами" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentAmenities_AmenityId",
                table: "ApartmentAmenities",
                column: "AmenityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApartmentAmenities");

            migrationBuilder.DropTable(
                name: "Amenities");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Apartments");
        }
    }
}
