using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Playlists_StreamingUser_UserId",
                table: "Playlists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StreamingUser",
                table: "StreamingUser");

            migrationBuilder.RenameTable(
                name: "StreamingUser",
                newName: "StreamingUsers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StreamingUsers",
                table: "StreamingUsers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Playlists_StreamingUsers_UserId",
                table: "Playlists",
                column: "UserId",
                principalTable: "StreamingUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Playlists_StreamingUsers_UserId",
                table: "Playlists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StreamingUsers",
                table: "StreamingUsers");

            migrationBuilder.RenameTable(
                name: "StreamingUsers",
                newName: "StreamingUser");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StreamingUser",
                table: "StreamingUser",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Playlists_StreamingUser_UserId",
                table: "Playlists",
                column: "UserId",
                principalTable: "StreamingUser",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
