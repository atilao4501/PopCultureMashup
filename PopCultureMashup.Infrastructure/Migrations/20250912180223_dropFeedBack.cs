using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PopCultureMashup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dropFeedBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedback");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedback_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedback_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_ItemId",
                table: "Feedback",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_RecommendationId",
                table: "Feedback",
                column: "RecommendationId");
        }
    }
}
