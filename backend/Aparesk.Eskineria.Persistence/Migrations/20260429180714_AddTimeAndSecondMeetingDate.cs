using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aparesk.Eskineria.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeAndSecondMeetingDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "MeetingDate",
                table: "GeneralAssemblies",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<DateTime>(
                name: "SecondMeetingDate",
                table: "GeneralAssemblies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondMeetingDate",
                table: "GeneralAssemblies");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "MeetingDate",
                table: "GeneralAssemblies",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
