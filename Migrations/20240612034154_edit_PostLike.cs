using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChitChit.Migrations
{
    public partial class edit_PostLike : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Like_CreateAt",
                table: "PostLike",
                newName: "RecordTime");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PostLike",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PostLike");

            migrationBuilder.RenameColumn(
                name: "RecordTime",
                table: "PostLike",
                newName: "Like_CreateAt");
        }
    }
}
