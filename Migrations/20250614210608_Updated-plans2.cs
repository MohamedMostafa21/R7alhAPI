using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R7alaAPI.Migrations
{
    /// <inheritdoc />
    public partial class Updatedplans2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "PlanPlaces");

            migrationBuilder.AddColumn<int>(
                name: "Days",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Days",
                table: "PlanPlaces",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Days",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "Days",
                table: "PlanPlaces");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "Plans",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "PlanPlaces",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
