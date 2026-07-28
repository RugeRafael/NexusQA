using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QACopilot.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIdToTrainingDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "TrainingDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDocuments_ProjectId",
                table: "TrainingDocuments",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingDocuments_Projects_ProjectId",
                table: "TrainingDocuments",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingDocuments_Projects_ProjectId",
                table: "TrainingDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TrainingDocuments_ProjectId",
                table: "TrainingDocuments");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TrainingDocuments");
        }
    }
}
