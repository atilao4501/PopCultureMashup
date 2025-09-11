using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PopCultureMashup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dropDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecommendationResults_RecommendationId",
                table: "RecommendationResults");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Recommendations");

            migrationBuilder.AddColumn<decimal>(
                name: "NoveltyW",
                table: "Recommendations",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PopularityW",
                table: "Recommendations",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecencyW",
                table: "Recommendations",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SimilarityW",
                table: "Recommendations",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalCandidates",
                table: "Recommendations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalReturned",
                table: "Recommendations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "YearScore",
                table: "RecommendationResults",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ThemesScore",
                table: "RecommendationResults",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TextScore",
                table: "RecommendationResults",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "RecommendationResults",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PopularityScore",
                table: "RecommendationResults",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GenresScore",
                table: "RecommendationResults",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FranchiseBonus",
                table: "RecommendationResults",
                type: "decimal(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationResults_RecommendationId_Rank",
                table: "RecommendationResults",
                columns: new[] { "RecommendationId", "Rank" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecommendationResults_RecommendationId_Rank",
                table: "RecommendationResults");

            migrationBuilder.DropColumn(
                name: "NoveltyW",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "PopularityW",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "RecencyW",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "SimilarityW",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "TotalCandidates",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "TotalReturned",
                table: "Recommendations");

            migrationBuilder.AddColumn<byte>(
                name: "Direction",
                table: "Recommendations",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<decimal>(
                name: "YearScore",
                table: "RecommendationResults",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ThemesScore",
                table: "RecommendationResults",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TextScore",
                table: "RecommendationResults",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "RecommendationResults",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PopularityScore",
                table: "RecommendationResults",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GenresScore",
                table: "RecommendationResults",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FranchiseBonus",
                table: "RecommendationResults",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationResults_RecommendationId",
                table: "RecommendationResults",
                column: "RecommendationId");
        }
    }
}
