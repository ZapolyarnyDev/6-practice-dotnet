using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Practice6.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261017090000_AddStoredFiles")]
public sealed class AddStoredFiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StoredFiles",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                AdId = table.Column<int>(type: "INTEGER", nullable: false),
                OriginalName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                StoredRelativePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Size = table.Column<long>(type: "INTEGER", nullable: false),
                UploadedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StoredFiles", f => f.Id);
                table.ForeignKey("FK_StoredFiles_Ads_AdId", f => f.AdId, "Ads", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_StoredFiles_Users_UploadedBy", f => f.UploadedBy, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_StoredFiles_AdId", "StoredFiles", "AdId");
        migrationBuilder.CreateIndex("IX_StoredFiles_UploadedBy", "StoredFiles", "UploadedBy");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("StoredFiles");
}
