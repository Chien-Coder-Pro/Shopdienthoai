using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Models;

namespace Shopdienthoai.Data
{
    public partial class ShopDbContext : DbContext
    {
        public ShopDbContext()
        {
        }

        public ShopDbContext(DbContextOptions<ShopDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Chitiethoadon> Chitiethoadons
        {
            get; set;
        }

        public virtual DbSet<Danhgium> Danhgia
        {
            get; set;
        }

        public virtual DbSet<Giohang> Giohangs
        {
            get; set;
        }

        public virtual DbSet<Hangkhachhang> Hangkhachhangs
        {
            get; set;
        }

        public virtual DbSet<Hoadon> Hoadons
        {
            get; set;
        }

        public virtual DbSet<Khuyenmai> Khuyenmais
        {
            get; set;
        }

        public virtual DbSet<Loaisanpham> Loaisanphams
        {
            get; set;
        }

        public virtual DbSet<Nguoidung> Nguoidungs
        {
            get; set;
        }

        public virtual DbSet<Phanquyen> Phanquyens
        {
            get; set;
        }

        public virtual DbSet<Sanpham> Sanphams
        {
            get; set;
        }

        public virtual DbSet<Giaodichthanhtoan> Giaodichthanhtoans
        {
            get; set;
        }
        public virtual DbSet<Emailhangdoi> Emailhangdois
        {
            get; set;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Chitiethoadon>(entity =>
            {
                entity.HasKey(e => e.MaCthd).HasName("PK__CHITIETH__1E4FA7710E6FAD88");

                entity.Property(e => e.SoLuong).HasDefaultValue(1);

                entity.HasOne(d => d.MaHdNavigation).WithMany(p => p.Chitiethoadons)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__CHITIETHOA__MaHD__60A75C0F");

                entity.HasOne(d => d.MaSpNavigation).WithMany(p => p.Chitiethoadons)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__CHITIETHOA__MaSP__619B8048");
            });

            modelBuilder.Entity<Danhgium>(entity =>
            {
                entity.HasKey(e => e.MaDg).HasName("PK__DANHGIA__272586603572DE42");

                entity.Property(e => e.NgayLap).HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.MaNdNavigation).WithMany(p => p.Danhgia)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DANHGIA__MaND__6A30C649");

                entity.HasOne(d => d.MaSpNavigation).WithMany(p => p.Danhgia)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DANHGIA__MaSP__693CA210");
            });

            modelBuilder.Entity<Giohang>(entity =>
            {
                entity.HasKey(e => new { e.MaNd, e.MaSp }).HasName("PK__GIOHANG__F55787A5CCB4F63B");

                entity.Property(e => e.SoLuong).HasDefaultValue(1);

                entity.HasOne(d => d.MaNdNavigation).WithMany(p => p.Giohangs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__GIOHANG__MaND__6EF57B66");

                entity.HasOne(d => d.MaSpNavigation).WithMany(p => p.Giohangs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__GIOHANG__MaSP__6FE99F9F");
            });

            modelBuilder.Entity<Hangkhachhang>(entity =>
            {
                entity.HasKey(e => e.MaHang).HasName("PK__HANGKHAC__19C0DB1D8639E513");
            });

            modelBuilder.Entity<Hoadon>(entity =>
            {
                entity.HasKey(e => e.MaHd).HasName("PK__HOADON__2725A6E0D37FE427");

                entity.Property(e => e.NgayLap).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.TienGiam).HasComputedColumnSql("(CONVERT([decimal](18,2),round(([TongTien]*[PhanTramGiam])/(100),(0))))", true);
                entity.Property(e => e.TongThanhToan).HasComputedColumnSql("(CONVERT([decimal](18,2),[TongTien]-round(([TongTien]*[PhanTramGiam])/(100),(0))))", true);
                entity.Property(e => e.TrangThai).HasDefaultValue("Chờ xử lý");

                entity.HasOne(d => d.MaHangNavigation).WithMany(p => p.Hoadons).HasConstraintName("FK__HOADON__MaHang__59FA5E80");

                entity.HasOne(d => d.MaNdNavigation).WithMany(p => p.Hoadons)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__HOADON__MaND__5629CD9C");
            });

            modelBuilder.Entity<Khuyenmai>(entity =>
            {
                entity.HasKey(e => e.MaKm).HasName("PK__KHUYENMA__2725CF15AFE69812");
            });

            modelBuilder.Entity<Loaisanpham>(entity =>
            {
                entity.HasKey(e => e.MaLsp).HasName("PK__LOAISANP__3B983FFE55C5717F");
            });

            modelBuilder.Entity<Nguoidung>(entity =>
            {
                entity.HasKey(e => e.MaNd).HasName("PK__NGUOIDUN__2725D724DA8C5348");

                entity.Property(e => e.MaHang).HasDefaultValue(1);
                entity.Property(e => e.TrangThai).HasDefaultValue(1);

                entity.HasOne(d => d.MaHangNavigation).WithMany(p => p.Nguoidungs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__NGUOIDUNG__MaHan__45F365D3");

                entity.HasOne(d => d.MaQuyenNavigation).WithMany(p => p.Nguoidungs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__NGUOIDUNG__MaQuy__440B1D61");
            });

            modelBuilder.Entity<Phanquyen>(entity =>
            {
                entity.HasKey(e => e.MaQuyen).HasName("PK__PHANQUYE__1D4B7ED40F5A88B6");
            });

            modelBuilder.Entity<Sanpham>(entity =>
            {
                entity.HasKey(e => e.MaSp).HasName("PK__SANPHAM__2725081C5CA01F27");

                entity.Property(e => e.SoLuong).HasDefaultValue(1);
                entity.Property(e => e.TrangThai).HasDefaultValue(1);

                entity.HasOne(d => d.MaKmNavigation).WithMany(p => p.Sanphams)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__SANPHAM__MaKM__4E88ABD4");

                entity.HasOne(d => d.MaLspNavigation).WithMany(p => p.Sanphams)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__SANPHAM__MaLSP__49C3F6B7");
            });

            modelBuilder.Entity<Nguoidung>().HasIndex(x => x.TaiKhoan).IsUnique();
            modelBuilder.Entity<Nguoidung>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<Nguoidung>().HasIndex(x => x.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");
            modelBuilder.Entity<Hoadon>().HasIndex(x => x.MaDatHang).IsUnique().HasFilter("[MaDatHang] IS NOT NULL");
            modelBuilder.Entity<Giaodichthanhtoan>().HasIndex(x => x.OrderId).IsUnique();
            modelBuilder.Entity<Giaodichthanhtoan>().HasIndex(x => x.RequestId).IsUnique();
            modelBuilder.Entity<Giaodichthanhtoan>().HasIndex(x => x.MaHd);
            modelBuilder.Entity<Giaodichthanhtoan>().HasOne<Hoadon>().WithMany().HasForeignKey(x => x.MaHd).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Emailhangdoi>().HasIndex(x => x.Khoa).IsUnique();
        }

    }
}
