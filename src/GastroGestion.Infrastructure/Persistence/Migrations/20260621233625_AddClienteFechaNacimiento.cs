using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastroGestion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteFechaNacimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaNacimiento",
                table: "Clientes",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Clientes");
        }
    }
}
