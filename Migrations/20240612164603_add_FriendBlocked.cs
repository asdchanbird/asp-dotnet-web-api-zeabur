using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChitChit.Migrations
{
    public partial class add_FriendBlocked : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FriendBlocked",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlockerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlockedId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendBlocked", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FriendBlocked");
        }
    }
}
