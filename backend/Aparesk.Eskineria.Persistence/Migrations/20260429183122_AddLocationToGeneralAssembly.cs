using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aparesk.Eskineria.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToGeneralAssembly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "GeneralAssemblies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "GeneralAssemblies");
        }
    }
}
