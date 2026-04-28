using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aparesk.Eskineria.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceNotesWithAgendaItems_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "GeneralAssemblies");

            migrationBuilder.CreateTable(
                name: "GeneralAssemblyAgendaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneralAssemblyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralAssemblyAgendaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralAssemblyAgendaItems_GeneralAssemblies_GeneralAssemblyId",
                        column: x => x.GeneralAssemblyId,
                        principalTable: "GeneralAssemblies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneralAssemblyAgendaItems_GeneralAssemblyId",
                table: "GeneralAssemblyAgendaItems",
                column: "GeneralAssemblyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneralAssemblyAgendaItems");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "GeneralAssemblies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
