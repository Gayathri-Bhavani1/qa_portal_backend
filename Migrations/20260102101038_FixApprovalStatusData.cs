using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qa_portal_apis.Migrations
{
    /// <inheritdoc />
    public partial class FixApprovalStatusData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE users SET approval_status = 'Pending' WHERE approval_status = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
