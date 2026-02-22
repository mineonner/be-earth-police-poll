using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace police_poll_service.Migrations
{
    /// <inheritdoc />
    public partial class inittable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "config",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(200)", nullable: false),
                    description = table.Column<string>(type: "varchar(500)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "evaluation",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(100)", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    create_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    org_unit_code = table.Column<string>(type: "varchar(100)", nullable: false),
                    service_work_score = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    service_work_count = table.Column<int>(type: "int", nullable: false),
                    investigative_work_score = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    investigative_work_count = table.Column<int>(type: "int", nullable: false),
                    crime_prevention_work_score = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    crime_prevention_work_count = table.Column<int>(type: "int", nullable: false),
                    traffic_work_score = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    traffic_work_count = table.Column<int>(type: "int", nullable: false),
                    satisfaction_score = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    satisfaction_count = table.Column<int>(type: "int", nullable: false),
                    evaluators_amount = table.Column<int>(type: "int", nullable: false),
                    evaluation_year = table.Column<string>(type: "varchar(4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation", x => x.id);
                });

            // เพิ่ม Unique Index สำหรับ code
            migrationBuilder.CreateIndex(
                name: "IX_evaluation_code",
                table: "evaluation",
                column: "code",
                unique: true);

            migrationBuilder.CreateTable(
                name: "org_unit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(100)", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    create_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    role_code = table.Column<string>(type: "varchar(100)", nullable: false),
                    evaluation_type = table.Column<string>(type: "varchar(50)", nullable: true),
                    is_evaluation = table.Column<bool>(type: "bit", nullable: false),
                    head_org_unit = table.Column<string>(type: "varchar(500)", nullable: false),
                    service_work_total = table.Column<int>(type: "int", nullable: false),
                    investigative_work_total = table.Column<int>(type: "int", nullable: false),
                    crime_prevention_work_total = table.Column<int>(type: "int", nullable: false),
                    traffic_work_total = table.Column<int>(type: "int", nullable: false),
                    satisfaction_total = table.Column<int>(type: "int", nullable: false),
                    evaluators_total = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_unit", x => x.id);
                });

            // เพิ่ม Unique Index สำหรับ code
            migrationBuilder.CreateIndex(
                name: "IX_org_unit_code",
                table: "org_unit",
                column: "code",
                unique: true);

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(100)", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.id);
                });

            // เพิ่ม Unique Index สำหรับ code
            migrationBuilder.CreateIndex(
                name: "IX_role_code",
                table: "role",
                column: "code",
                unique: true);

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<long>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user = table.Column<string>(type: "varchar(100)", nullable: false),
                    password = table.Column<string>(type: "varchar(500)", nullable: false),
                    token = table.Column<string>(type: "varchar(500)", nullable: true),
                    create_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    role_code = table.Column<string>(type: "varchar(100)", nullable: false),
                    org_unit_code = table.Column<string>(type: "varchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_code",
                table: "role",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "config");

            migrationBuilder.DropTable(
                name: "evaluation");

            migrationBuilder.DropTable(
                name: "org_unit");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
