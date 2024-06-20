using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hamdlebot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTwitchUserIdColumnAndChangeNamesOfChannelNameAndChannelId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bot_channel_channel_id",
                table: "bot_channel");

            migrationBuilder.DropColumn(
                name: "channel_id",
                table: "bot_channel");

            migrationBuilder.AddColumn<string>(
                name: "twitch_channel_name",
                table: "bot_channel",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "twitch_user_id",
                table: "bot_channel",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "bot_channel_command",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BotChannelId = table.Column<int>(type: "integer", nullable: false),
                    command = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bot_channel_command", x => x.id);
                    table.ForeignKey(
                        name: "FK_bot_channel_command_bot_channel_BotChannelId",
                        column: x => x.BotChannelId,
                        principalTable: "bot_channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bot_channel_twitch_user_id",
                table: "bot_channel",
                column: "twitch_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bot_channel_command_BotChannelId",
                table: "bot_channel_command",
                column: "BotChannelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_channel_command");

            migrationBuilder.DropIndex(
                name: "IX_bot_channel_twitch_user_id",
                table: "bot_channel");

            migrationBuilder.DropColumn(
                name: "twitch_channel_name",
                table: "bot_channel");

            migrationBuilder.DropColumn(
                name: "twitch_user_id",
                table: "bot_channel");

            migrationBuilder.AddColumn<int>(
                name: "channel_id",
                table: "bot_channel",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_bot_channel_channel_id",
                table: "bot_channel",
                column: "channel_id",
                unique: true);
        }
    }
}
