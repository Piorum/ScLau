using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatBackend.Migrations
{
    /// <inheritdoc />
    public partial class chathistory_timestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ChatHistories",
                newName: "ChatId");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageTime",
                table: "ChatHistories",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMessageTime",
                table: "ChatHistories");

            migrationBuilder.RenameColumn(
                name: "ChatId",
                table: "ChatHistories",
                newName: "Id");
        }
    }
}
