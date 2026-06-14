using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastroGestion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPedidoAndStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrecioConfirmado",
                table: "PedidoLineas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioConfirmado",
                table: "PedidoLineas");
        }
    }
}
