using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastroGestion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturaAnulacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "Facturas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "Facturas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "Facturas");
        }
    }
}
