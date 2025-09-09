using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PopCultureMashup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addGenreToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Weights_UserId",
                table: "Weights",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Seeds_UserId_ItemId",
                table: "Seeds",
                columns: new[] { "UserId", "ItemId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemCreators_Items_ItemId",
                table: "ItemCreators",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemGenres_Items_ItemId",
                table: "ItemGenres",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemThemes_Items_ItemId",
                table: "ItemThemes",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemCreators_Items_ItemId",
                table: "ItemCreators");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemGenres_Items_ItemId",
                table: "ItemGenres");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemThemes_Items_ItemId",
                table: "ItemThemes");

            migrationBuilder.DropIndex(
                name: "IX_Weights_UserId",
                table: "Weights");

            migrationBuilder.DropIndex(
                name: "IX_Seeds_UserId_ItemId",
                table: "Seeds");
        }
    }
}
