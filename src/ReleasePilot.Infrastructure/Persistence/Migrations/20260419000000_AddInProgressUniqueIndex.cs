using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReleasePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInProgressUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 2 is the integer value of PromotionState.InProgress in the enum.
            // If the enum order ever changes, this index must be updated manually.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Promotions_InProgress_AppId_TargetEnv\" " +
                "ON \"Promotions\" (\"ApplicationId\", \"TargetEnvironment\") " +
                "WHERE \"State\" = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Promotions_InProgress_AppId_TargetEnv\";");
        }
    }
}
