using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASPHotelM2631.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCounterBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KHACHHANG",
                columns: table => new
                {
                    MaKH = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KHACHHANG", x => x.MaKH);
                });

            migrationBuilder.CreateTable(
                name: "LOAIPHONG",
                columns: table => new
                {
                    MaLoaiPhong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiPhong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GiaCoBan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOAIPHONG", x => x.MaLoaiPhong);
                    table.CheckConstraint("CK_LOAIPHONG_GiaCoBan", "[GiaCoBan] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "GIAMUAVU",
                columns: table => new
                {
                    MaGia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLoaiPhong = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayKetThuc = table.Column<DateOnly>(type: "date", nullable: false),
                    GiaApDung = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GIAMUAVU", x => x.MaGia);
                    table.CheckConstraint("CK_GIAMUAVU_Gia", "[GiaApDung] >= 0");
                    table.CheckConstraint("CK_GIAMUAVU_Ngay", "[NgayKetThuc] >= [NgayBatDau]");
                    table.ForeignKey(
                        name: "FK_GIAMUAVU_LOAIPHONG_MaLoaiPhong",
                        column: x => x.MaLoaiPhong,
                        principalTable: "LOAIPHONG",
                        principalColumn: "MaLoaiPhong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PHONG",
                columns: table => new
                {
                    MaPhong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhong = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SoTang = table.Column<int>(type: "int", nullable: false),
                    MaLoaiPhong = table.Column<int>(type: "int", nullable: false),
                    TrangThaiPhong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHONG", x => x.MaPhong);
                    table.ForeignKey(
                        name: "FK_PHONG_LOAIPHONG_MaLoaiPhong",
                        column: x => x.MaLoaiPhong,
                        principalTable: "LOAIPHONG",
                        principalColumn: "MaLoaiPhong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DATPHONG",
                columns: table => new
                {
                    MaDatPhong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKH = table.Column<int>(type: "int", nullable: false),
                    MaPhong = table.Column<int>(type: "int", nullable: false),
                    NguoiTaoId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NgayNhan = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayTra = table.Column<DateOnly>(type: "date", nullable: false),
                    SoKhach = table.Column<int>(type: "int", nullable: false),
                    KenhDatPhong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThaiDatPhong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DaThu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SoTienHoan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HuyLuc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DATPHONG", x => x.MaDatPhong);
                    table.CheckConstraint("CK_DATPHONG_Ngay", "[NgayTra] > [NgayNhan]");
                    table.CheckConstraint("CK_DATPHONG_SoKhach", "[SoKhach] > 0");
                    table.CheckConstraint("CK_DATPHONG_Tien", "[TongTien] >= 0 AND [DaThu] >= 0 AND [DaThu] <= [TongTien] AND ([SoTienHoan] IS NULL OR ([SoTienHoan] >= 0 AND [SoTienHoan] <= [DaThu]))");
                    table.ForeignKey(
                        name: "FK_DATPHONG_AspNetUsers_NguoiTaoId",
                        column: x => x.NguoiTaoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DATPHONG_KHACHHANG_MaKH",
                        column: x => x.MaKH,
                        principalTable: "KHACHHANG",
                        principalColumn: "MaKH",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DATPHONG_PHONG_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "PHONG",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DATPHONG_MaKH",
                table: "DATPHONG",
                column: "MaKH");

            migrationBuilder.CreateIndex(
                name: "IX_DATPHONG_MaPhong_NgayNhan_NgayTra",
                table: "DATPHONG",
                columns: new[] { "MaPhong", "NgayNhan", "NgayTra" });

            migrationBuilder.CreateIndex(
                name: "IX_DATPHONG_NguoiTaoId",
                table: "DATPHONG",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_GIAMUAVU_MaLoaiPhong",
                table: "GIAMUAVU",
                column: "MaLoaiPhong");

            migrationBuilder.CreateIndex(
                name: "IX_KHACHHANG_SoDienThoai",
                table: "KHACHHANG",
                column: "SoDienThoai");

            migrationBuilder.CreateIndex(
                name: "IX_LOAIPHONG_TenLoaiPhong",
                table: "LOAIPHONG",
                column: "TenLoaiPhong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PHONG_MaLoaiPhong",
                table: "PHONG",
                column: "MaLoaiPhong");

            migrationBuilder.CreateIndex(
                name: "IX_PHONG_SoPhong",
                table: "PHONG",
                column: "SoPhong",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DATPHONG");

            migrationBuilder.DropTable(
                name: "GIAMUAVU");

            migrationBuilder.DropTable(
                name: "KHACHHANG");

            migrationBuilder.DropTable(
                name: "PHONG");

            migrationBuilder.DropTable(
                name: "LOAIPHONG");
        }
    }
}
