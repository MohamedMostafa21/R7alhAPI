using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R7alaAPI.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Tourguide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "TourGuideApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "TourGuideApplications");
        }
    }
}
