-- CẬP NHẬT ẢNH SẢN PHẨM NGÀY 08/11/2026
-- SANPHAM.HinhAnh chỉ lưu tên file ảnh, không lưu URL hoặc tiền tố image/.
-- Chép thư mục image vào wwwroot/image trong dự án ASP.NET Core MVC.
-- Giữ 22 sản phẩm đã có ảnh tải thành công; bỏ 18 sản phẩm không lấy được ảnh.
-- Bỏ toàn bộ hóa đơn mẫu chứa sản phẩm bị bỏ, cùng mọi chi tiết của hóa đơn đó.
-- Bỏ đánh giá và giỏ hàng tham chiếu sản phẩm bị bỏ để bảo toàn khóa ngoại.
-- Không thay đổi số tiền của các hóa đơn mẫu còn lại.
-- Xem HUONG_DAN.md để biết danh sách bị loại và cách dùng.

-- CƠ SỞ DỮ LIỆU WEBSITE BÁN ĐIỆN THOẠI
-- Chuyển từ shopdienthoai(1).sql (MySQL) sang SQL Server.
-- Cách trình bày dựa theo shop.sql của bài thực hành trên lớp.
-- Tổng cộng 10 bảng, 68 bản ghi sau khi lọc ảnh; giữ nguyên mã của các bản ghi còn lại.
-- Tên bảng viết hoa; giữ tên cột gốc để dễ đối chiếu và phát sinh Model.
-- Bổ sung MaCTHD, MaDG làm khóa chính tự tăng cho Entity Framework.
-- Bổ sung ThanhTien = SoLuong * DonGia theo mẫu chi tiết hóa đơn trên lớp.
-- Số tiền dùng decimal(18,2); số lượng dùng int và CHECK không âm/dương.
-- Chuỗi tiếng Việt dùng nvarchar và N'...'; ngày giờ dùng ISO 8601.
-- Các INSERT ghi rõ mã cũ bằng IDENTITY_INSERT để giữ đúng liên kết.
-- MatKhau mở rộng lên varchar(255), nhưng giữ nguyên giá trị MD5 từ file gốc.
-- Khi làm đăng nhập theo BTTH09, cần tạo lại mật khẩu bằng PasswordHasher;
-- không đưa trực tiếp chuỗi MD5 cũ vào VerifyHashedPassword.
-- GIOHANG được giữ lại; giỏ hàng Session theo BTTH08 có thể dùng độc lập.
-- QUY TẮC GIẢM GIÁ THEO HẠNG KHÁCH HÀNG
-- 1. Chính sách mẫu có thể sửa: Thường 0%, VIP 5%, VVIP 10%.
-- 2. NGUOIDUNG.MaHang mặc định là 1 (Thường). Quản trị viên gán VIP/VVIP;
--    phiên bản này chưa tự nâng hạng theo doanh số.
-- 3. PHANQUYEN tiếp tục quản lý quyền Customer/Admin; HANGKHACHHANG quản lý ưu đãi.
-- 4. Khi lập đơn mới, code thanh toán phải lấy hạng và tỷ lệ từ database:
--       hd.MaHang = hang.MaHang;
--       hd.PhanTramGiam = hang.PhanTramGiam;
--       hd.TongTien = tổng ThanhTien của các dòng CHITIETHOADON;
--    DonGia của dòng hàng là giá đã chốt, đã xử lý khuyến mãi sản phẩm nếu có.
-- 5. SQL Server tự tính hai cột chỉ đọc (không gán hai cột này khi INSERT/UPDATE):
--       TienGiam = ROUND(TongTien * PhanTramGiam / 100, 0), làm tròn đến đồng.
--       TongThanhToan = TongTien - TienGiam.
--    Trang thanh toán hiển thị TongThanhToan làm số tiền cuối cùng.
-- 6. MaHang và PhanTramGiam trong HOADON là thông tin tại thời điểm đặt hàng.
--    Đổi hạng của khách hoặc đổi tỷ lệ trong HANGKHACHHANG không đổi tiền đơn cũ.
-- 7. Hóa đơn gốc giữ MaHang = NULL, PhanTramGiam = 0, TongThanhToan = TongTien.
--    Không tự áp dụng ưu đãi mới cho các đơn cũ.

-- LƯU Ý: chạy toàn bộ script sẽ XÓA và TẠO LẠI database shopdienthoai.
-- Nếu đã có CSDL và muốn giữ dữ liệu, dùng riêng 02_Cap_nhat_chuc_nang.sql.

Use Master
GO
    IF exists(Select name From sys.databases Where name='shopdienthoai')
    DROP Database shopdienthoai
GO
    Create Database shopdienthoai
GO

USE shopdienthoai;
GO

CREATE TABLE PHANQUYEN(
    MaQuyen int primary key identity(1,1),
    TenQuyen nvarchar(20) not null,
    ChiTietQuyen nvarchar(255) not null
)
GO

CREATE TABLE HANGKHACHHANG(
    MaHang int primary key identity(1,1),
    TenHang nvarchar(20) not null unique,
    PhanTramGiam decimal(5,2) not null default 0 check(PhanTramGiam between 0 and 100),
    MoTa nvarchar(255)
)
GO

CREATE TABLE LOAISANPHAM(
    MaLSP int primary key identity(1,1),
    TenLSP nvarchar(70) not null,
    HinhAnh varchar(200) not null,
    Mota nvarchar(200) not null
)
GO

CREATE TABLE KHUYENMAI(
    MaKM int primary key identity(1,1),
    TenKM nvarchar(100) not null,
    LoaiKM nvarchar(20) not null,
    GiaTriKM decimal(18,2) not null default 0 check(GiaTriKM >= 0),
    NgayBD datetime not null,
    NgayKT datetime not null
)
GO

CREATE TABLE NGUOIDUNG(
    MaND int primary key identity(1,1),
    Ho nvarchar(20) not null,
    Ten nvarchar(20) not null,
    GioiTinh nvarchar(10) not null,
    SDT varchar(20) not null,
    Email varchar(50) not null,
    DiaChi nvarchar(200) not null,
    TaiKhoan nvarchar(100) not null,
    MatKhau varchar(255) not null,
    MaQuyen int not null references PHANQUYEN(MaQuyen),
    MaHang int not null default 1 references HANGKHACHHANG(MaHang),
    TrangThai int not null default 1
)
GO

CREATE TABLE SANPHAM(
    MaSP int primary key identity(1,1),
    MaLSP int not null references LOAISANPHAM(MaLSP),
    TenSP nvarchar(70) not null,
    DonGia decimal(18,2) not null default 0 check(DonGia >= 0),
    SoLuong int not null default 1 check(SoLuong >= 0),
    HinhAnh varchar(200) not null,
    MaKM int not null references KHUYENMAI(MaKM),
    ManHinh nvarchar(50) not null,
    HDH nvarchar(50) not null,
    CamSau nvarchar(50) not null,
    CamTruoc nvarchar(50) not null,
    CPU nvarchar(50) not null,
    Ram nvarchar(50) not null,
    Rom nvarchar(50) not null,
    SDCard nvarchar(50) not null,
    Pin nvarchar(50) not null,
    SoSao int not null default 0 check(SoSao between 0 and 5),
    SoDanhGia int not null default 0 check(SoDanhGia >= 0),
    TrangThai int not null default 1
)
GO

CREATE TABLE HOADON(
    MaHD int primary key identity(1,1),
    MaND int not null references NGUOIDUNG(MaND),
    NgayLap datetime not null default getdate(),
    NguoiNhan nvarchar(50) not null,
    SDT varchar(20) not null,
    DiaChi nvarchar(100) not null,
    PhuongThucTT nvarchar(20) not null,
    TongTien decimal(18,2) not null default 0 check(TongTien >= 0),
    MaHang int null references HANGKHACHHANG(MaHang),
    PhanTramGiam decimal(5,2) not null default 0 check(PhanTramGiam between 0 and 100),
    TienGiam AS CAST(ROUND(TongTien * PhanTramGiam / 100, 0) AS decimal(18,2)) PERSISTED,
    TongThanhToan AS CAST(TongTien - ROUND(TongTien * PhanTramGiam / 100, 0) AS decimal(18,2)) PERSISTED,
    TrangThai nvarchar(70) not null default N'Chờ xử lý',
    check(MaHang is not null or PhanTramGiam = 0)
)
GO

CREATE TABLE CHITIETHOADON(
    MaCTHD int primary key identity(1,1),
    MaHD int not null references HOADON(MaHD),
    MaSP int not null references SANPHAM(MaSP),
    SoLuong int not null default 1 check(SoLuong > 0),
    DonGia decimal(18,2) not null default 0 check(DonGia >= 0),
    ThanhTien decimal(18,2) not null default 0
)
GO

CREATE TABLE DANHGIA(
    MaDG int primary key identity(1,1),
    MaSP int not null references SANPHAM(MaSP),
    MaND int not null references NGUOIDUNG(MaND),
    SoSao int not null check(SoSao between 1 and 5),
    BinhLuan nvarchar(255) not null,
    NgayLap datetime not null default getdate()
)
GO

CREATE TABLE GIOHANG(
    MaND int not null references NGUOIDUNG(MaND),
    MaSP int not null references SANPHAM(MaSP),
    SoLuong int not null default 1 check(SoLuong > 0),
    primary key(MaND,MaSP)
)
GO

-- Các chỉ mục truy vấn dữ liệu và hạng khách hàng
CREATE INDEX IX_NGUOIDUNG_MaHang ON NGUOIDUNG(MaHang);
CREATE INDEX IX_HOADON_MaHang ON HOADON(MaHang);
CREATE INDEX IX_NGUOIDUNG_MaQuyen ON NGUOIDUNG(MaQuyen);
CREATE INDEX IX_SANPHAM_MaLSP ON SANPHAM(MaLSP);
CREATE INDEX IX_SANPHAM_MaKM ON SANPHAM(MaKM);
CREATE INDEX IX_HOADON_MaND ON HOADON(MaND);
CREATE INDEX IX_CHITIETHOADON_MaHD ON CHITIETHOADON(MaHD);
CREATE INDEX IX_CHITIETHOADON_MaSP ON CHITIETHOADON(MaSP);
CREATE INDEX IX_DANHGIA_MaSP ON DANHGIA(MaSP);
GO

-- Dữ liệu bảng PHANQUYEN (2 bản ghi)
SET IDENTITY_INSERT PHANQUYEN ON;
INSERT INTO PHANQUYEN(MaQuyen,TenQuyen,ChiTietQuyen) VALUES(1,N'Customer',N'Khách hàng có tài khoản');
INSERT INTO PHANQUYEN(MaQuyen,TenQuyen,ChiTietQuyen) VALUES(2,N'Admin',N'Quản Trị Viên');
SET IDENTITY_INSERT PHANQUYEN OFF;
GO

-- Dữ liệu bảng HANGKHACHHANG (3 bản ghi)
SET IDENTITY_INSERT HANGKHACHHANG ON;
INSERT INTO HANGKHACHHANG(MaHang,TenHang,PhanTramGiam,MoTa) VALUES(1,N'Thường',0,N'Hạng mặc định khi đăng ký tài khoản, không giảm theo hạng.');
INSERT INTO HANGKHACHHANG(MaHang,TenHang,PhanTramGiam,MoTa) VALUES(2,N'VIP',5,N'Giảm 5% trên tổng tiền hàng của hóa đơn.');
INSERT INTO HANGKHACHHANG(MaHang,TenHang,PhanTramGiam,MoTa) VALUES(3,N'VVIP',10,N'Giảm 10% trên tổng tiền hàng của hóa đơn.');
SET IDENTITY_INSERT HANGKHACHHANG OFF;
GO

-- Dữ liệu bảng LOAISANPHAM (14 bản ghi)
SET IDENTITY_INSERT LOAISANPHAM ON;
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(1,N'Apple','Apple.jpg',N'Các sản phẩm của Apple');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(2,N'Coolpad','Coolpad.png',N'Các sản phẩm của coolpad');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(3,N'HTC','HTC.jpg',N'Các sản phẩm đến từ HTC');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(4,N'Itel','Itel.jpg',N'Các sản phẩm của Itel');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(5,N'Mobell','Mobell.jpg',N'Các sản phẩm của mobell');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(6,N'Vivo','Vivo.jpg',N'Các sản phẩm của Vivo');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(7,N'Oppo','Oppo.jpg',N'Camara Selphi cuc chat tu Oppo');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(8,N'SamSung','Samsung.jpg',N'Khuyen mai lon tu SamSung');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(9,N'Phillips','Philips.jpg',N'Cac san pham tuyet dep tu Phillip');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(10,N'Nokia','Nokia.jpg',N'Các sản phẩm đến từ thương hiệu Nokia');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(11,N'Motorola','Motorola.jpg',N'Motorola is the best');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(12,N'Mobiistar','Mobiistar.jpg',N'Các sản phẩm đến từ thương hiệu Mobiistar');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(14,N'Xiaomi','Xiaomi.png',N'Các sản phẩm đến từ thương hiệu Xiaomi');
INSERT INTO LOAISANPHAM(MaLSP,TenLSP,HinhAnh,Mota) VALUES(15,N'Realme','thuonghieurealme.png',N'tốt bổ và rẻ');
SET IDENTITY_INSERT LOAISANPHAM OFF;
GO

-- Dữ liệu bảng KHUYENMAI (5 bản ghi)
SET IDENTITY_INSERT KHUYENMAI ON;
INSERT INTO KHUYENMAI(MaKM,TenKM,LoaiKM,GiaTriKM,NgayBD,NgayKT) VALUES(1,N'Không khuyến mãi',N'Nothing',0,'2026-04-14T00:00:00','2026-04-22T00:00:00');
INSERT INTO KHUYENMAI(MaKM,TenKM,LoaiKM,GiaTriKM,NgayBD,NgayKT) VALUES(2,N'Giảm giá',N'GiamGia',500000,'2026-04-01T00:00:00','2026-04-14T00:00:00');
INSERT INTO KHUYENMAI(MaKM,TenKM,LoaiKM,GiaTriKM,NgayBD,NgayKT) VALUES(3,N'Giá rẻ online',N'GiaReOnline',650000,'2026-03-31T00:00:00','2026-04-04T00:00:00');
INSERT INTO KHUYENMAI(MaKM,TenKM,LoaiKM,GiaTriKM,NgayBD,NgayKT) VALUES(4,N'Trả góp',N'TraGop',0,'2026-04-08T00:00:00','2026-04-20T00:00:00');
INSERT INTO KHUYENMAI(MaKM,TenKM,LoaiKM,GiaTriKM,NgayBD,NgayKT) VALUES(5,N'Mới ra mắt',N'MoiRaMat',0,'2026-04-12T00:00:00','2026-04-30T00:00:00');
SET IDENTITY_INSERT KHUYENMAI OFF;
GO

-- Dữ liệu bảng NGUOIDUNG (8 bản ghi)
SET IDENTITY_INSERT NGUOIDUNG ON;
INSERT INTO NGUOIDUNG(MaND,Ho,Ten,GioiTinh,SDT,Email,DiaChi,TaiKhoan,MatKhau,MaQuyen,TrangThai) VALUES(1,N'Văn',N'Chiến',N'Nam','0012232312','admin@gmail.com',N'',N'admin','e10adc3949ba59abbe56e057f20f883e',2,1);
INSERT INTO NGUOIDUNG(MaND,Ho,Ten,GioiTinh,SDT,Email,DiaChi,TaiKhoan,MatKhau,MaQuyen,TrangThai) VALUES(2,N'Tran',N'Hoang',N'Nam','0123456789','th@gmail.com',N'',N'Hoang016','e10adc3949ba59abbe56e057f20f883e',1,1);
INSERT INTO NGUOIDUNG(MaND,Ho,Ten,GioiTinh,SDT,Email,DiaChi,TaiKhoan,MatKhau,MaQuyen,TrangThai) VALUES(3,N'Nguyen',N'Danh',N'Nam','0123456789','danh@123',N'',N'Abc','158419fbf7d650a81effada265a0851f',1,1);
INSERT INTO NGUOIDUNG(MaND,Ho,Ten,GioiTinh,SDT,Email,DiaChi,TaiKhoan,MatKhau,MaQuyen,TrangThai) VALUES(4,N'Nguyễn',N'Huệ',N'Nữ','0384229772','hue@gamil.com',N'số nhà 577, ấp Bắc Thạnh, xã Thoại Sơn , tỉnh An Giang',N'Hue','202cb962ac59075b964b07152d234b70',1,1);
INSERT INTO NGUOIDUNG(MaND,Ho,Ten,GioiTinh,SDT,Email,DiaChi,TaiKhoan,MatKhau,MaQuyen,TrangThai) VALUES(5,N'Dương Thành',N'Đạt',N'Nam','01207764668','dat@gmail',N'Châu Đốc, An Giang',N'dat','202cb962ac59075b964b07152d234b70',1,1);
INSERT INTO NGUOIDUNG(MaND,Ho,Ten,GioiTinh,SDT,Email,DiaChi,TaiKhoan,MatKhau,MaQuyen,TrangThai) VALUES(6,N'Le Nhut',N'Hao',N'Nam','0384229774','hao@gamil.com',N'Long Xuyên',N'hao','202cb962ac59075b964b07152d234b70',1,1);
INSERT INTO NGUOIDUNG(MaND,Ho,Ten,GioiTinh,SDT,Email,DiaChi,TaiKhoan,MatKhau,MaQuyen,TrangThai) VALUES(7,N'Nguyễn',N'Nam',N'','','',N'',N'nam','202cb962ac59075b964b07152d234b70',1,1);
INSERT INTO NGUOIDUNG(MaND,Ho,Ten,GioiTinh,SDT,Email,DiaChi,TaiKhoan,MatKhau,MaQuyen,TrangThai) VALUES(8,N'Nguyễn Văn',N'Thắng',N'Nam','0322288332','',N'Thoại Sơn, An Giang',N'thang','202cb962ac59075b964b07152d234b70',1,1);
SET IDENTITY_INSERT NGUOIDUNG OFF;
GO

-- Dữ liệu bảng SANPHAM (22 bản ghi)
SET IDENTITY_INSERT SANPHAM ON;

INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(21,10,N'Nokia black future',999999000,10,'nokia-black-future.jpg',2,N'4K, Chống nước, Chống trầy',N'iOS + Android song song',N'Bộ tứ camara tàng hình',N'Chuẩn thế giới 50MP',N'16 nhân 128 bit',N'Không giới hạn',N'Dùng thoải mái',N'Không cần',N'Không cần sạc',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(22,8,N'Samsung Galaxy A7 (2018)',8990000,10,'samsung-galaxy-a7-2018.jpg',4,N'Super AMOLED, 6.0'', Full HD+',N'Android 8.0 (Oreo)',N'24 MP, 8 MP và 5 MP (3 camera)',N'24 MP',N'Exynos 7885 8 nhân 64-bit',N'4 GB',N'64 GB',N'MicroSD, hỗ trợ tối đa 512 GB',N'3300 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(30,6,N'Vivo V9',7490000,10,'vivo-v9.jpg',2,N'IPS LCD, 6.3'', Full HD+',N'Android 8.1 (Oreo)',N'16 MP và 5 MP (2 camera)',N'24 MP',N'Snapdragon 626 8 nhân 64-bit',N'4 GB',N'64 GB',N'MicroSD, hỗ trợ tối đa 256 GB',N'3260 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(31,6,N'Vivo V11',10990000,10,'vivo-v11.jpg',4,N'Super AMOLED, 6.41'', Full HD+',N'Android 8.1 (Oreo)',N'12 MP và 5 MP (2 camera)',N'25 MP',N'Qualcomm Snapdragon 660 8 nhân',N'6 GB',N'128 GB',N'MicroSD, hỗ trợ tối đa 256 GB',N'3400 mAh, có sạc nhanh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(32,6,N'Vivo Y71',3290000,10,'vivo-y71.jpg',4,N'IPS LCD, 6.0'', HD+',N'Android 8.1 (Oreo)',N'13 MP',N'5 MP',N'Qualcomm Snapdragon 425 4 nhân 64-bit',N'3 GB',N'16 GB',N'MicroSD, hỗ trợ tối đa 256 GB',N'3360 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(33,6,N'Vivo Y85',4990000,10,'vivo-y85.jpg',2,N'IPS LCD, 6.22'', HD+',N'Android 8.1 (Oreo)',N'13 MP và 2 MP (2 camera)',N'8 MP',N'MediaTek MT6762 8 nhân 64-bit (Helio P22)',N'4 GB',N'32 GB',N'MicroSD, hỗ trợ tối đa 256 GB',N'3260 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(34,5,N'Mobell M789',550000,10,'mobell-m789.jpg',1,N'TFT, 2.4'', 65.536 màu',N'Không',N'Không',N'Không',N'Không',N'Không',N'Không',N'Không',N'1200 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(35,5,N'Mobell Rock 3',590000,10,'mobell-rock-3.jpg',1,N'TFT, 2.4'', 65.536 màu',N'Không',N'Không',N'Không',N'Không',N'Không',N'Không',N'Không',N'5000 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(36,5,N'Mobell S60',1790000,10,'mobell-s60.jpg',5,N'LCD, 5.5'', FWVGA',N'Android 8.1 (Oreo)',N'8 MP và 2 MP (2 camera)',N'5 MP',N'MT6580 4 nhân 32-bit',N'1 GB',N'16 GB',N'MicroSD, hỗ trợ tối đa 32 GB',N'2650 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(37,4,N'Itel P32',1890000,10,'itel-p32.jpg',1,N'IPS LCD, 5.45'', qHD',N'Android 8.1 (Oreo)',N'5 MP và 0.3 MP (2 camera)',N'5 MP',N'MT6580 4 nhân 32-bit',N'1 GB',N'8 GB',N'MicroSD, hỗ trợ tối đa 32 GB',N'4000 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(38,4,N'Itel A32F',1350000,10,'itel-a32f.jpg',5,N'TFT, 5'', FWVGA',N'Android Go Edition',N'5 MP',N'2 MP',N'MediaTek MT6580 4 nhân 32-bit',N'1 GB',N'8 GB',N'MicroSD, hỗ trợ tối đa 32 GB',N'2050 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(39,4,N'Itel it2123',160000,10,'itel-it2123.jpg',1,N'TFT, 1.77'', 65.536 màu',N'Không',N'Không',N'Không',N'Không',N'Không',N'Không',N'Không',N'1000 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(40,4,N'Itel it2161',170000,10,'itel-it2161.jpg',5,N'TFT, 1.77'', WVGA',N'Không',N'Không',N'Không',N'Không',N'Không',N'Không',N'MicroSD, hỗ trợ tối đa 32 GB',N'1000 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(41,2,N'Coolpad N3D',2390000,10,'coolpad-n3d.jpg',5,N'IPS LCD, 5.45'', HD+',N'Android 8.1 (Oreo)',N'8 MP và 0.3 MP (2 camera)',N'5 MP',N'Spreadtrum SC9850K 4 nhân',N'2 GB',N'16 GB',N'MicroSD, hỗ trợ tối đa 32 GB',N'2500 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(42,3,N'HTC U12 life',7690000,10,'htc-u12-life.jpg',5,N'Super LCD, 6'', Full HD+',N'Android 8.1 (Oreo)',N'16 MP và 5 MP (2 camera)',N'13 MP',N'Qualcomm Snapdragon 636 8 nhân',N'4 GB',N'64 GB',N'MicroSD, hỗ trợ tối đa 512 GB',N'3600 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(43,2,N'Coolpad N3 mini',1390000,10,'coolpad-n3-mini.jpg',5,N'IPS LCD, 5'', WVGA',N'Android Go Edition',N'5 MP và 0.3 MP (2 camera)',N'2 MP',N'MT6580 4 nhân 32-bit',N'1 GB',N'8 GB',N'MicroSD, hỗ trợ tối đa 32 GB',N'2000 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(44,2,N'Coolpad N3',1890000,9,'coolpad-n3.jpg',5,N'IPS LCD, 5.45'', HD+',N'Android Go Edition',N'5 MP và 0.3 MP (2 camera)',N'2 MP',N'Spreadtrum SC9850K 4 nhân',N'1 GB',N'16 GB',N'MicroSD, hỗ trợ tối đa 32 GB',N'2300 mAh',4,2,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(45,11,N'Motorola Moto C 4G',1290000,10,'motorola-moto-c-4g.jpg',1,N'TFT, 5'', FWVGA',N'Android 7.1 (Nougat)',N'5 MP',N'2 MP',N'MT6737 4 nhân',N'1 GB',N'16 GB',N'MicroSD, hỗ trợ tối đa 32 GB',N'2350 mAh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(46,1,N'iPhone Xr 128GB',24990000,8,'iphone-xr-128gb.jpg',3,N'IPS LCD, 6.1'', IPS LCD, 16 triệu màu',N'iOS 12',N'12 MP',N'7 MP',N'Apple A12 Bionic 6 nhân',N'3 GB',N'128 GB',N'Không',N'2942 mAh, có sạc nhanh',3,2,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(47,1,N'iPhone 8 Plus 64GB',20990000,10,'iphone-8-plus-64gb.jpg',4,N'LED-backlit IPS LCD, 5.5'', Retina HD',N'iOS 11',N'2 camera 12 MP',N'7 MP',N'Apple A11 Bionic 6 nhân',N'3 GB',N'64 GB',N'Không',N'2691 mAh, có sạc nhanh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(48,1,N'iPhone Xr 64GB',22990000,10,'iphone-xr-64gb.jpg',3,N'IPS LCD, 6.1'', IPS LCD, 16 triệu màu',N'iOS 12',N'12 MP',N'7 MP',N'Apple A12 Bionic 6 nhân',N'3 GB',N'64 GB',N'',N'2942 mAh, có sạc nhanh',0,0,1);
INSERT INTO SANPHAM(MaSP,MaLSP,TenSP,DonGia,SoLuong,HinhAnh,MaKM,ManHinh,HDH,CamSau,CamTruoc,CPU,Ram,Rom,SDCard,Pin,SoSao,SoDanhGia,TrangThai) VALUES(49,1,N'iPhone 8 Plus 256GB',25790000,10,'iphone-8-plus-256gb.jpg',2,N'LED-backlit IPS LCD, 4.7'', Retina HD',N'iOS 11',N'12 MP',N'7 MP',N'Apple A11 Bionic 6 nhân',N'2 GB',N'256 GB',N'Không',N'1821 mAh, có sạc nhanh',0,0,1);

SET IDENTITY_INSERT SANPHAM OFF;
GO

-- Dữ liệu bảng HOADON (4 bản ghi)
SET IDENTITY_INSERT HOADON ON;

INSERT INTO HOADON(MaHD,MaND,NgayLap,NguoiNhan,SDT,DiaChi,PhuongThucTT,TongTien,TrangThai) VALUES(8,8,'2026-04-15T21:08:21',N'Nguyễn Văn Thắng','0322288332',N'Thoại Sơn, An Giang',N'Tiền mặt',3240000,N'Đã hủy');
INSERT INTO HOADON(MaHD,MaND,NgayLap,NguoiNhan,SDT,DiaChi,PhuongThucTT,TongTien,TrangThai) VALUES(9,8,'2026-04-15T21:18:14',N'Nguyễn Văn Thắng','0322288332',N'Thoại Sơn, An Giang',N'Tiền mặt',1390000,N'Đã giao hàng');

INSERT INTO HOADON(MaHD,MaND,NgayLap,NguoiNhan,SDT,DiaChi,PhuongThucTT,TongTien,TrangThai) VALUES(12,8,'2026-04-16T00:37:08',N'Nguyễn Văn Thắng','0322288332',N'Thoại Sơn, An Giang',N'Tiền mặt',1890000,N'Đã hủy');
INSERT INTO HOADON(MaHD,MaND,NgayLap,NguoiNhan,SDT,DiaChi,PhuongThucTT,TongTien,TrangThai) VALUES(13,8,'2026-04-16T00:47:09',N'Nguyễn Văn Thắng','0322288332',N'Thoại Sơn, An Giang',N'Tiền mặt',1890000,N'Đã hủy');

SET IDENTITY_INSERT HOADON OFF;
GO

-- Dữ liệu bảng CHITIETHOADON (5 bản ghi)

INSERT INTO CHITIETHOADON(MaHD,MaSP,SoLuong,DonGia,ThanhTien) VALUES(8,38,1,1350000,1350000);
INSERT INTO CHITIETHOADON(MaHD,MaSP,SoLuong,DonGia,ThanhTien) VALUES(8,37,1,1890000,1890000);
INSERT INTO CHITIETHOADON(MaHD,MaSP,SoLuong,DonGia,ThanhTien) VALUES(9,43,1,1390000,1390000);

INSERT INTO CHITIETHOADON(MaHD,MaSP,SoLuong,DonGia,ThanhTien) VALUES(12,44,1,1890000,1890000);
INSERT INTO CHITIETHOADON(MaHD,MaSP,SoLuong,DonGia,ThanhTien) VALUES(13,44,1,1890000,1890000);

GO

-- Dữ liệu bảng DANHGIA (4 bản ghi)

INSERT INTO DANHGIA(MaSP,MaND,SoSao,BinhLuan,NgayLap) VALUES(46,2,4,N'Đỏ may mắn <3','2019-05-16T19:32:58');
INSERT INTO DANHGIA(MaSP,MaND,SoSao,BinhLuan,NgayLap) VALUES(44,4,5,N'wow, giá rẻ cấu hình ngon đây rồi <3','2019-05-16T19:38:03');

INSERT INTO DANHGIA(MaSP,MaND,SoSao,BinhLuan,NgayLap) VALUES(44,4,3,N'Ram có 1GB tiếc quá','2019-05-16T19:49:20');
INSERT INTO DANHGIA(MaSP,MaND,SoSao,BinhLuan,NgayLap) VALUES(46,4,2,N'Pin khá tệ ','2019-05-16T19:49:44');

GO

-- Dữ liệu bảng GIOHANG (1 bản ghi)

INSERT INTO GIOHANG(MaND,MaSP,SoLuong) VALUES(8,43,1);
GO

-- Kiểm tra dữ liệu sau khi chạy script
SELECT * FROM PHANQUYEN;
SELECT * FROM HANGKHACHHANG;
SELECT * FROM LOAISANPHAM;
SELECT * FROM KHUYENMAI;
SELECT * FROM NGUOIDUNG;
SELECT * FROM SANPHAM;
SELECT * FROM HOADON;
SELECT * FROM CHITIETHOADON;
SELECT * FROM DANHGIA;
SELECT * FROM GIOHANG;
GO

-- ChienNguyenShop: chạy trên database đã tạo bằng file SQL gốc.
-- Không DROP database, không xóa dữ liệu hiện có.
USE shopdienthoai;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('NGUOIDUNG','GoogleId') IS NULL ALTER TABLE NGUOIDUNG ADD GoogleId varchar(100) NULL;
IF COL_LENGTH('NGUOIDUNG','CoMatKhau') IS NULL ALTER TABLE NGUOIDUNG ADD CoMatKhau bit NOT NULL CONSTRAINT DF_ND_CoMatKhau DEFAULT 1;
IF COL_LENGTH('HOADON','TrangThaiThanhToan') IS NULL
BEGIN
    ALTER TABLE HOADON ADD TrangThaiThanhToan nvarchar(30) NOT NULL CONSTRAINT DF_HD_TTTT DEFAULT N'Chưa thanh toán';
END;
IF COL_LENGTH('HOADON','MaDatHang') IS NULL ALTER TABLE HOADON ADD MaDatHang varchar(36) NULL;
IF COL_LENGTH('HANGKHACHHANG','NguongChiTieu') IS NULL ALTER TABLE HANGKHACHHANG ADD NguongChiTieu decimal(18,2) NOT NULL CONSTRAINT DF_HANG_Nguong DEFAULT 0;
COMMIT;
GO
-- Chỉ khởi tạo ngưỡng cho lần đầu. Những lần chạy sau giữ ngưỡng Admin đã sửa.
IF NOT EXISTS (SELECT 1 FROM HANGKHACHHANG WHERE NguongChiTieu > 0)
BEGIN
    UPDATE HANGKHACHHANG SET NguongChiTieu=CASE TenHang WHEN N'VIP' THEN 20000000 WHEN N'VVIP' THEN 50000000 ELSE 0 END;
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_ND_TaiKhoan' AND object_id=OBJECT_ID('NGUOIDUNG')) CREATE UNIQUE INDEX UX_ND_TaiKhoan ON NGUOIDUNG(TaiKhoan);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_ND_Email' AND object_id=OBJECT_ID('NGUOIDUNG')) CREATE UNIQUE INDEX UX_ND_Email ON NGUOIDUNG(Email);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_ND_GoogleId' AND object_id=OBJECT_ID('NGUOIDUNG')) CREATE UNIQUE INDEX UX_ND_GoogleId ON NGUOIDUNG(GoogleId) WHERE GoogleId IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_HD_MaDatHang' AND object_id=OBJECT_ID('HOADON')) CREATE UNIQUE INDEX UX_HD_MaDatHang ON HOADON(MaDatHang) WHERE MaDatHang IS NOT NULL;
GO
IF OBJECT_ID('GIAODICHTHANHTOAN') IS NULL
CREATE TABLE GIAODICHTHANHTOAN(
    MaGd int IDENTITY PRIMARY KEY,
    MaHd int NOT NULL REFERENCES HOADON(MaHD),
    OrderId varchar(100) NOT NULL UNIQUE,
    RequestId varchar(100) NOT NULL UNIQUE,
    SoTien decimal(18,2) NOT NULL CHECK(SoTien>=0),
    TrangThai nvarchar(30) NOT NULL,
    TransId varchar(100) NULL,
    ResultCode int NULL,
    NgayTao datetime2 NOT NULL,
    NgayCapNhat datetime2 NULL,
    PayUrl nvarchar(500) NULL
);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_GD_MaHd' AND object_id=OBJECT_ID('GIAODICHTHANHTOAN'))CREATE INDEX IX_GD_MaHd ON GIAODICHTHANHTOAN(MaHd);
IF OBJECT_ID('EMAILHANGDOI') IS NULL
CREATE TABLE EMAILHANGDOI(
    MaEmail int IDENTITY PRIMARY KEY,
    Khoa nvarchar(100) NOT NULL UNIQUE,
    NguoiNhan nvarchar(100) NOT NULL,
    TieuDe nvarchar(200) NOT NULL,
    NoiDung nvarchar(max) NOT NULL,
    SoLanThu int NOT NULL DEFAULT 0,
    NgayTao datetime2 NOT NULL,
    DaGuiLuc datetime2 NULL,
    ThuTiepLuc datetime2 NULL,
    LoiGanNhat nvarchar(200) NULL
);
GO

-- Xác nhận trạng thái thanh toán cho hóa đơn mẫu đã giao trong bộ dữ liệu demo.
UPDATE HOADON SET TrangThaiThanhToan=N'Đã thanh toán' WHERE TrangThai=N'Đã giao hàng';
GO
UPDATE s SET SoDanhGia=ISNULL(d.SoLuong,0), SoSao=ISNULL(d.SoSao,0)
FROM SANPHAM s LEFT JOIN (SELECT MaSP,COUNT(*) SoLuong,CAST(ROUND(AVG(CAST(SoSao AS decimal(10,2))),0) AS int) SoSao FROM DANHGIA GROUP BY MaSP) d ON s.MaSP=d.MaSP;
GO
