using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PopCultureMashup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dropWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Weights");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Weights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Franchise = table.Column<double>(type: "float", nullable: false),
                    Genres = table.Column<double>(type: "float", nullable: false),
                    Popularity = table.Column<double>(type: "float", nullable: false),
                    Text = table.Column<double>(type: "float", nullable: false),
                    Themes = table.Column<double>(type: "float", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weights", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Weights_UserId",
                table: "Weights",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }
    }
}
