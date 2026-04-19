using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sportlifeapi.Migrations
{
    /// <inheritdoc />
    public partial class AddMercadoPagoConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mercadopago_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    public_key = table.Column<string>(type: "text", nullable: false),
                    webhook_secret = table.Column<string>(type: "text", nullable: false),
                    notification_url = table.Column<string>(type: "text", nullable: false),
                    success_url = table.Column<string>(type: "text", nullable: false),
                    failure_url = table.Column<string>(type: "text", nullable: false),
                    pending_url = table.Column<string>(type: "text", nullable: false),
                    is_test_mode = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mercadopago_config", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "mercadopago_config");
        }
    }
}
