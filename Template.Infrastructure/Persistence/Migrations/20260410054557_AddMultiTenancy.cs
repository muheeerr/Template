using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_user_code",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_roles_code",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_auth_accounts_username",
                table: "auth_accounts");

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            var migrationUtc = DateTimeOffset.UtcNow;
            migrationBuilder.InsertData(
                table: "tenants",
                columns: ["id", "slug", "name", "is_active", "created_at", "updated_at", "is_deleted"],
                values: new object[] { DefaultTenantId, "default", "Default", true, migrationUtc, migrationUtc, false });

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "user_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "user_role_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "roles",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "auth_accounts",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_id_email",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_id_user_code",
                table: "users",
                columns: new[] { "tenant_id", "user_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_tenant_id",
                table: "user_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_tenant_id",
                table: "user_role_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_tenant_id_code",
                table: "roles",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_accounts_tenant_id_username",
                table: "auth_accounts",
                columns: new[] { "tenant_id", "username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_slug",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_accounts_tenants_tenant_id",
                table: "auth_accounts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_roles_tenants_tenant_id",
                table: "roles",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_role_assignments_tenants_tenant_id",
                table: "user_role_assignments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_sessions_tenants_tenant_id",
                table: "user_sessions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_tenants_tenant_id",
                table: "users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_auth_accounts_tenants_tenant_id",
                table: "auth_accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_roles_tenants_tenant_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_role_assignments_tenants_tenant_id",
                table: "user_role_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_user_sessions_tenants_tenant_id",
                table: "user_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_users_tenants_tenant_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_users_tenant_id_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_tenant_id_user_code",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_user_sessions_tenant_id",
                table: "user_sessions");

            migrationBuilder.DropIndex(
                name: "IX_user_role_assignments_tenant_id",
                table: "user_role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_roles_tenant_id_code",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_auth_accounts_tenant_id_username",
                table: "auth_accounts");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "user_role_assignments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "auth_accounts");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_user_code",
                table: "users",
                column: "user_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_accounts_username",
                table: "auth_accounts",
                column: "username",
                unique: true);
        }
    }
}
