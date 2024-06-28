using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hamdlebot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowAccessToObsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_access_to_obs",
                table: "bot_channel",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_access_to_obs",
                table: "bot_channel");
        }
    }
}
