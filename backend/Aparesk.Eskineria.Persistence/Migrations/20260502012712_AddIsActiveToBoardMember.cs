using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aparesk.Eskineria.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToBoardMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BoardMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BoardMembers");
        }
    }
}
