using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Client_Invoice_System.Migrations
{
    /// <inheritdoc />
    public partial class updateddropdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
               name: "OwnerProfileId",
        table: "Resources",
        type: "int",
        nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_OwnerProfileId",
                table: "Resources",
                column: "OwnerProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Owners_OwnerProfileId",
                table: "Resources",
                column: "OwnerProfileId",
                principalTable: "Owners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Owners_OwnerProfileId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_OwnerProfileId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "OwnerProfileId",
                table: "Resources");
        }
    }
}
