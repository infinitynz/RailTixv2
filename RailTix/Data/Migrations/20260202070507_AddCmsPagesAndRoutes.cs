using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RailTix.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCmsPagesAndRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CmsPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsHomepage = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CustomUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CmsPages_CmsPages_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CmsPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CmsReservedRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsReservedRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CmsPageComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsPageComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CmsPageComponents_CmsPages_PageId",
                        column: x => x.PageId,
                        principalTable: "CmsPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CmsPages",
                columns: new[] { "Id", "CreatedAt", "CustomUrl", "IsHomepage", "IsPublished", "ParentId", "Path", "Position", "Slug", "Title", "UpdatedAt" },
                values: new object[] { new Guid("7ae3c9b2-43e9-4d72-88f0-9efb6a0a1f10"), new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, null, "/", 0, "home", "Home", new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "CmsReservedRoutes",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Segment", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2c4f37b4-3b91-4e21-82a1-5b6d5f60d7c5"), new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "events", new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("9bb81f34-5b12-4bc0-a2a5-66e9b5d2c1ff"), new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), true, "account", new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CmsPageComponents_PageId_Position",
                table: "CmsPageComponents",
                columns: new[] { "PageId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_CmsPages_IsHomepage",
                table: "CmsPages",
                column: "IsHomepage",
                unique: true,
                filter: "[IsHomepage] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CmsPages_ParentId_Slug",
                table: "CmsPages",
                columns: new[] { "ParentId", "Slug" },
                unique: true,
                filter: "[ParentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CmsPages_Path",
                table: "CmsPages",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CmsReservedRoutes_Segment",
                table: "CmsReservedRoutes",
                column: "Segment",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CmsPageComponents");

            migrationBuilder.DropTable(
                name: "CmsReservedRoutes");

            migrationBuilder.DropTable(
                name: "CmsPages");
        }
    }
}
