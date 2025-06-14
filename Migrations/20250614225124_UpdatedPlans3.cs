using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R7alaAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedPlans3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Days",
                table: "PlanPlaces");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "PlanPlaces",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "PlanId1",
                table: "PlanPlaces",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPlaces_PlanId1",
                table: "PlanPlaces",
                column: "PlanId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanPlaces_Plans_PlanId1",
                table: "PlanPlaces",
                column: "PlanId1",
                principalTable: "Plans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanPlaces_Plans_PlanId1",
                table: "PlanPlaces");

            migrationBuilder.DropIndex(
                name: "IX_PlanPlaces_PlanId1",
                table: "PlanPlaces");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "PlanPlaces");

            migrationBuilder.DropColumn(
                name: "PlanId1",
                table: "PlanPlaces");

            migrationBuilder.AddColumn<int>(
                name: "Days",
                table: "PlanPlaces",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
