using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_CurrentState_UpdatedAt",
                table: "WorkflowExecutions",
                columns: new[] { "CurrentState", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutions_CurrentState_UpdatedAt",
                table: "WorkflowExecutions");
        }
    }
}
