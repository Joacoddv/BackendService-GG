using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastroGestion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MesasCoordsAndClienteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Entrega_Zona",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PosicionX",
                table: "Mesas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PosicionY",
                table: "Mesas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Apellido",
                table: "Clientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "Clientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Clientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Zona",
                table: "ClienteDirecciones",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Entrega_Zona",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PosicionX",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "PosicionY",
                table: "Mesas");

            migrationBuilder.DropColumn(
                name: "Apellido",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Zona",
                table: "ClienteDirecciones");
        }
    }
}
