using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QACopilot.Migrations
{
    /// <inheritdoc />
    public partial class AddReportHtmlToTestPlanAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "TestPlanAnalyses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportHtml",
                table: "TestPlanAnalyses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "TestPlanAnalyses");

            migrationBuilder.DropColumn(
                name: "ReportHtml",
                table: "TestPlanAnalyses");
        }
    }
}
