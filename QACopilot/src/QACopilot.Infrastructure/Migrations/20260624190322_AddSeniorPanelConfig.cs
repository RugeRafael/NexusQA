using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QACopilot.Migrations
{
    /// <inheritdoc />
    public partial class AddSeniorPanelConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeniorPanelConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MetaDocumentos = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeniorPanelConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeniorPanelConfigs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeniorPanelConfigs_UserId",
                table: "SeniorPanelConfigs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeniorPanelConfigs");
        }
    }
}
