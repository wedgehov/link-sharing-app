using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entity.Migrations
{
    /// <inheritdoc />
    public partial class EnforceRequiredUserFieldsAndRenamePublicGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_public_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "display_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "public_guid",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_users_public_guid",
                table: "users",
                column: "public_guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_public_guid",
                table: "users");

            migrationBuilder.DropColumn(
                name: "public_guid",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "display_email",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_id",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_public_id",
                table: "users",
                column: "public_id",
                unique: true);
        }
    }
}
