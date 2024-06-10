using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hamdlebot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_bot_channel_channel_id",
                table: "bot_channel",
                column: "channel_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bot_channel_channel_id",
                table: "bot_channel");
        }
    }
}
