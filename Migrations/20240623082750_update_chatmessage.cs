using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChitChit.Migrations
{
    public partial class update_chatmessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMessage_GroupChat_GroupChatId",
                table: "GroupMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateMessage_PrivateChat_PrivateChatId",
                table: "PrivateMessage");

            migrationBuilder.RenameColumn(
                name: "PrivateChatId",
                table: "PrivateMessage",
                newName: "ChatId");

            migrationBuilder.RenameIndex(
                name: "IX_PrivateMessage_PrivateChatId",
                table: "PrivateMessage",
                newName: "IX_PrivateMessage_ChatId");

            migrationBuilder.RenameColumn(
                name: "GroupChatId",
                table: "GroupMessage",
                newName: "ChatId");

            migrationBuilder.RenameIndex(
                name: "IX_GroupMessage_GroupChatId",
                table: "GroupMessage",
                newName: "IX_GroupMessage_ChatId");

            migrationBuilder.AlterColumn<string>(
                name: "NewsImg",
                table: "News",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewsContent",
                table: "News",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "NewTitle",
                table: "News",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMessage_GroupChat_ChatId",
                table: "GroupMessage",
                column: "ChatId",
                principalTable: "GroupChat",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateMessage_PrivateChat_ChatId",
                table: "PrivateMessage",
                column: "ChatId",
                principalTable: "PrivateChat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMessage_GroupChat_ChatId",
                table: "GroupMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateMessage_PrivateChat_ChatId",
                table: "PrivateMessage");

            migrationBuilder.DropColumn(
                name: "NewTitle",
                table: "News");

            migrationBuilder.RenameColumn(
                name: "ChatId",
                table: "PrivateMessage",
                newName: "PrivateChatId");

            migrationBuilder.RenameIndex(
                name: "IX_PrivateMessage_ChatId",
                table: "PrivateMessage",
                newName: "IX_PrivateMessage_PrivateChatId");

            migrationBuilder.RenameColumn(
                name: "ChatId",
                table: "GroupMessage",
                newName: "GroupChatId");

            migrationBuilder.RenameIndex(
                name: "IX_GroupMessage_ChatId",
                table: "GroupMessage",
                newName: "IX_GroupMessage_GroupChatId");

            migrationBuilder.AlterColumn<string>(
                name: "NewsImg",
                table: "News",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "NewsContent",
                table: "News",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMessage_GroupChat_GroupChatId",
                table: "GroupMessage",
                column: "GroupChatId",
                principalTable: "GroupChat",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateMessage_PrivateChat_PrivateChatId",
                table: "PrivateMessage",
                column: "PrivateChatId",
                principalTable: "PrivateChat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
