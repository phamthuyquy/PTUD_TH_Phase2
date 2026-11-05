using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QTKD.Api.Migrations
{
    /// <inheritdoc />
    public partial class Week06BusinessFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContractId",
                table: "PriceLists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "PriceLists",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Customers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "Customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeName",
                table: "Customers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContractValue",
                table: "Contracts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "Contracts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceTerms",
                table: "Contracts",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractSubmissions_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                UPDATE "Customers"
                SET "Code" = 'LEGACY-CUS-' || "Id",
                    "TaxCode" = 'LEGACY-TAX-' || "Id",
                    "Address" = 'Chưa cập nhật',
                    "RepresentativeName" = 'Chưa cập nhật',
                    "ContactInfo" = 'Chưa cập nhật'
                WHERE "Code" IS NULL;

                UPDATE "PriceLists"
                SET "ContractId" = (SELECT MIN("Id") FROM "Contracts"),
                    "EndDate" = ("EffectiveDate" + INTERVAL '1 year')::date
                WHERE "ContractId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ContractId",
                table: "PriceLists",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "PriceLists",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Customers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactInfo",
                table: "Customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RepresentativeName",
                table: "Customers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxCode",
                table: "Customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ContractValue", "PaymentTerms", "ServiceTerms" },
                values: new object[] { 10000000m, "Thanh toán theo tháng", "Dịch vụ khai thác cảng theo thỏa thuận." });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "Code", "ContactInfo", "RepresentativeName", "TaxCode" },
                values: new object[] { "TP. Hồ Chí Minh", "CUS-001", "minhphat@example.test", "Nguyễn Minh", "0310000001" });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Address", "Code", "ContactInfo", "RepresentativeName", "TaxCode" },
                values: new object[] { "TP. Hồ Chí Minh", "CUS-002", "anbinh@example.test", "Trần Bình", "0310000002" });

            migrationBuilder.UpdateData(
                table: "PriceLists",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ContractId", "EndDate" },
                values: new object[] { 1, new DateOnly(2026, 12, 31) });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_ContractId",
                table: "PriceLists",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxCode",
                table: "Customers",
                column: "TaxCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractSubmissions_ContractId",
                table: "ContractSubmissions",
                column: "ContractId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractSubmissions_IdempotencyKey",
                table: "ContractSubmissions",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceLists_Contracts_ContractId",
                table: "PriceLists",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceLists_Contracts_ContractId",
                table: "PriceLists");

            migrationBuilder.DropTable(
                name: "ContractSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceLists_ContractId",
                table: "PriceLists");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Code",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TaxCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RepresentativeName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContractValue",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ServiceTerms",
                table: "Contracts");
        }
    }
}
