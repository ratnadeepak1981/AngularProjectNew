using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordSecurityAndLockoutToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'MustChangePassword')
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [MustChangePassword] bit NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'TemporaryPasswordExpiresAt')
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [TemporaryPasswordExpiresAt] datetime2 NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'FailedLoginAttempts')
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'LockoutEndUtc')
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [LockoutEndUtc] datetime2 NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'MustChangePassword')
                BEGIN
                    ALTER TABLE [dbo].[Users] DROP COLUMN [MustChangePassword];
                END

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'TemporaryPasswordExpiresAt')
                BEGIN
                    ALTER TABLE [dbo].[Users] DROP COLUMN [TemporaryPasswordExpiresAt];
                END

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'FailedLoginAttempts')
                BEGIN
                    ALTER TABLE [dbo].[Users] DROP COLUMN [FailedLoginAttempts];
                END

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'LockoutEndUtc')
                BEGIN
                    ALTER TABLE [dbo].[Users] DROP COLUMN [LockoutEndUtc];
                END
            ");
        }
    }
}
