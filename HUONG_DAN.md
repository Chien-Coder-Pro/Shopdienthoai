# ChienNguyenShop — hướng dẫn chạy và cấu hình

Đồ án website bán điện thoại bằng ASP.NET Core MVC (.NET 8), SQL Server, EF Core Database First, Razor và Bootstrap. Giao diện xanh lá; giữ cách tổ chức Controllers – Models – Data – Views của mẫu BTTH9, không chuyển sang kiến trúc Repository/BUS/DAO.

**Cửa hàng:** ChienNguyenShop · Long Xuyên, An Giang · 0384229772.

## 1. Chạy nhanh trên máy Windows của bạn

1. Giải nén ZIP vào một thư mục bình thường, không chạy trực tiếp trong cửa sổ xem ZIP.
2. Mở SQL Server Management Studio (SSMS), kết nối SQL Server của bạn.
3. Nếu tạo database demo mới: mở và chạy toàn bộ `Database/01_Tao_moi_shopdienthoai.sql`. **File này giữ lệnh DROP DATABASE từ bản SQL gốc và sẽ xóa database shopdienthoai hiện có.** Nếu cần giữ dữ liệu đang dùng, sao lưu và chỉ chạy `Database/02_Cap_nhat_chuc_nang.sql` trên database gốc đã có.
4. Mở `Shopdienthoai.sln` bằng Visual Studio có hỗ trợ .NET 8 và workload ASP.NET and web development.
5. Trong `Shopdienthoai/appsettings.json`, sửa `ConnectionStrings:DefaultConnection` đúng tên Server hiển thị ở SSMS. Bản sửa giữ nguyên cấu hình kết nối trong ZIP bạn gửi; khi chuyển máy, hãy đối chiếu lại tên Server trong SSMS.
6. Trong Solution Explorer, đặt **Shopdienthoai** làm Startup Project. Đợi NuGet Restore hoàn tất rồi chạy profile **https** bằng F5/Ctrl+F5.
7. Địa chỉ mặc định: `https://localhost:7217`. Nếu chứng chỉ phát triển chưa được tin cậy, chạy `dotnet dev-certs https --trust` trên máy của bạn.

Connection string mẫu (JSON dùng hai dấu gạch chéo để biểu diễn một dấu):

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=shopdienthoai;Trusted_Connection=True;TrustServerCertificate=True;"
```

Nếu dùng SQL Authentication, có thể đặt riêng connection string bằng User Secrets trong Visual Studio, không ghi mật khẩu thật vào mã nguồn.

Có thể chạy bằng terminal trong thư mục chứa solution:

```powershell
dotnet restore .\Shopdienthoai.sln
dotnet run --project .\Shopdienthoai\Shopdienthoai.csproj --launch-profile https
```

### Tài khoản demo từ SQL đã có

| Quyền | Tài khoản | Mật khẩu gốc |
| --- | --- | --- |
| Admin | admin | 123456 |
| Customer | Hoang016 | 123456 |
| Customer | thang | 123 |

Các mật khẩu mẫu cũ là MD5. Khi đăng nhập đúng mật khẩu lần đầu, ứng dụng tự chuyển riêng mật khẩu đó sang PasswordHasher. Tài khoản đăng ký mới luôn dùng PasswordHasher. Nên đổi mật khẩu demo sau khi đăng nhập. Email của tài khoản cũ giữ nguyên từ SQL bạn gửi; để kiểm thử gửi thư, đăng ký bằng email do bạn quản lý và kiểm tra hàng đợi trước khi bật SMTP.

## 2. Các chức năng đã triển khai

| Khu vực | Chức năng |
| --- | --- |
| Trang chủ | Banner, hãng, tìm tên, lọc khoảng giá, sắp xếp giá/đánh giá/mới nhất, phân trang 12 sản phẩm |
| Chi tiết sản phẩm | Ảnh, cấu hình, giá hiện hành, tồn kho, sản phẩm cùng hãng, đánh giá |
| Tài khoản | Đăng ký, đăng nhập bằng tên/email, đăng xuất, hồ sơ, đổi/tạo mật khẩu, khóa tài khoản |
| Google | Đăng nhập/đăng ký OAuth, bổ sung hồ sơ lần đầu, liên kết Google có cùng email với tài khoản đã đăng nhập |
| Giỏ hàng | Session cho khách; bảng GIOHANG lưu giỏ của tài khoản; thêm, đổi số lượng, xóa, khôi phục sau đăng nhập |
| Đặt hàng | Người nhận/địa chỉ, giá lấy lại từ database, khuyến mãi còn hạn, ưu đãi theo hạng, lưu đơn/chi tiết, trừ tồn trong transaction |
| Thanh toán | Trực tiếp có xác nhận thu tiền riêng; MoMo Sandbox có tạo yêu cầu, IPN, kiểm tra/đối soát |
| Đơn hàng | Lịch sử, chi tiết, in bằng trình duyệt, hủy đơn hợp lệ và hoàn tồn đúng một lần |
| Đánh giá | Chỉ người đã mua, nhận hàng và thanh toán; cập nhật nhận xét của mình; Admin xóa nhận xét |
| Admin | Quản lý sản phẩm và upload ảnh, hãng, khuyến mãi, người dùng/quyền, đơn hàng, đánh giá |
| Thành viên | Điều chỉnh ngưỡng và % giảm; tự tính khách đủ điều kiện; Admin duyệt nâng hạng hoặc gán thủ công |
| Báo cáo | Doanh thu đơn đã giao và thanh toán, số đơn, khách hàng, bán chạy và tồn ≤5 |
| Email | Chào mừng tài khoản Google mới; xác nhận đặt hàng; hàng đợi và tự thử lại khi SMTP lỗi |

## 3. Cấu trúc mã nguồn

| Vị trí | Vai trò |
| --- | --- |
| `Controllers/CustomerController.cs` | Một Controller chứa sản phẩm phía khách, tài khoản/Google, giỏ hàng, đơn hàng và GetData() như mẫu |
| `Controllers/AdminController.cs` | Báo cáo, đơn, người dùng, duyệt hạng, chính sách và hàng đợi email |
| `Controllers/AdminOnlyAttribute.cs` | Kiểm tra tài khoản còn hoạt động và quyền Admin từ CSDL trước khi chạy action quản trị |
| `Controllers/SanphamsController.cs` | Index, Details, GET/POST Create, GET/POST Edit, GET Delete, POST DeleteConfirmed; Upload() trong Controller |
| `Controllers/LoaisanphamsController.cs` | Đủ action CRUD theo mẫu, Upload() trong Controller, chặn xóa hãng đang có sản phẩm |
| `Controllers/KhuyenmaisController.cs` | Đủ action CRUD theo mẫu; kiểm tra khuyến mãi và chặn xóa bản ghi mặc định/đang dùng |
| `Controllers/PaymentController.cs` | MoMo Sandbox, IPN và đối soát, kế thừa trực tiếp Controller |
| `Data/ShopDbContext.cs` | Toàn bộ DbSet, quan hệ, computed column và index trong một file |
| `Models/` | Mỗi lớp một file; các thuộc tính Google/thanh toán/ngưỡng nằm trong entity tương ứng |
| `Views/Sanphams/`, `Views/Loaisanphams/`, `Views/Khuyenmais/` | Mỗi mục có Index, Details, Create, Edit và Delete riêng, thêm _ViewStart |
| `Views/Customer/_LayoutCustomer.cshtml` | Layout khách hàng đặt cạnh Views/Customer theo mẫu BTTH9 |
| `Views/Customer/_ViewStart.cshtml` | Chọn layout khách hàng |
| `Views/Admin/` | Các màn hình quản trị đã chốt, giữ giao diện xanh lá |
| `Views/Shared/` | Layout Admin và các partial thông báo, sản phẩm, hóa đơn dùng chung |
| `wwwroot/css/site.css` | Một file CSS tùy chỉnh, giữ nguyên bản bạn gửi |
| `wwwroot/js/site.js` | Một file JavaScript tùy chỉnh, giữ nguyên bản bạn gửi |
| `wwwroot/image/` | 22 ảnh sản phẩm và logo xanh lá, giữ nguyên bản bạn gửi |
| `wwwroot/lib/` | Bootstrap, jQuery và validation dùng cục bộ |
| `Database/` | Hai script SQL giữ nguyên; lần sửa cấu trúc này không cần chạy lại SQL |
| `KiemThu/` | Kiểm tra HTTP/EF riêng bằng SQLite; vẫn nằm ngoài Solution như bản bạn gửi |
| `CHINH_SUA_THEO_MAU.md` | Bảng trước/sau, cách thay bản cũ và danh sách từng file thay đổi |
| `KIEM_THU.md` | Kết quả kiểm tra bản sửa và phạm vi chưa xác minh với dịch vụ thật |

Các entity từ SQL giữ nguyên tên, kể cả `Danhgium` do Scaffold đặt. Khi Scaffold lại với `-Force`, cần giữ/sao lưu các thuộc tính bổ sung trong Nguoidung, Hoadon, Hangkhachhang và mapping bổ sung trong ShopDbContext vì hiện đã gộp vào các file này.


## 4. Quy tắc nghiệp vụ

### Giá, khuyến mãi và thành viên

- `GiamGia` và `GiaReOnline`: trừ `GiaTriKM` theo số tiền VND khi trong khoảng `NgayBD`–`NgayKT`. Giá tối thiểu bằng 0. Đây là quy tắc dùng cho dữ liệu mẫu này.
- `Nothing`: không giảm. `TraGop` và `MoiRaMat` không tự trừ tiền; dự án không có chức năng vay/trả góp.
- Giá dòng hóa đơn được chốt sau khuyến mãi sản phẩm. `TongTien` bằng tổng `ThanhTien` các dòng.
- SQL Server tự tính `TienGiam = ROUND(TongTien * PhanTramGiam / 100, 0)` và `TongThanhToan = TongTien - TienGiam`. Code không gán hai cột computed này.
- Chính sách mặc định: Thường 0%; VIP từ 20.000.000đ, giảm 5%; VVIP từ 50.000.000đ, giảm 10%.
- Chi tiêu hợp lệ là tổng `TongThanhToan` của các đơn **Đã giao hàng** và **Đã thanh toán**. Admin xem đề xuất và duyệt; hệ thống chưa tự nâng/hạ hạng.
- Đổi hạng/ngưỡng/tỷ lệ không thay đổi giá và ưu đãi của hóa đơn cũ.
- Thời gian nghiệp vụ trong code dùng giờ Việt Nam (UTC+7); dữ liệu `datetime` không kèm múi giờ.
- Các khuyến mãi gốc có ngày trong tháng 4/2026, vì vậy không còn hiệu lực vào tháng 11/2026. Admin có thể tạo chương trình mới hoặc chỉnh ngày để demo; ứng dụng không tự kéo dài ngày cũ.

### Thanh toán và tồn kho

- Trực tiếp: lúc đặt đơn là **Chưa thanh toán**. Admin chọn **Xác nhận đã thu tiền trực tiếp** sau khi thu đủ tiền. Không suy ra thanh toán từ việc chọn phương thức.
- MoMo: lưu đơn trước, khách mở chi tiết đơn và chọn **Thanh toán MoMo Sandbox**. Chỉ IPN có chữ ký hợp lệ hoặc kết quả query từ MoMo đã kiểm tra mới đánh dấu **Đã thanh toán**. URL quay về chỉ hiển thị kết quả, không ghi nhận trả tiền.
- Giới hạn MoMo theo tài liệu tích hợp: 1.000đ–50.000.000đ, số VND nguyên. Các đơn ngoài giới hạn dùng thanh toán trực tiếp.
- Tồn được trừ ngay khi tạo đơn, trong cùng transaction với hóa đơn/chi tiết/email. Đơn chưa thanh toán vẫn giữ số lượng này đến khi bị hủy. Bản này **không tự hết hạn** đơn MoMo chưa thanh toán.
- Chống gửi lại checkout bằng mã yêu cầu duy nhất. Hủy đơn kiểm tra trạng thái và chỉ hoàn tồn một lần.
- Khách chỉ tự hủy đơn **Chờ xử lý**, chưa trả tiền và không có giao dịch MoMo chưa rõ kết quả. Admin có thể hủy thêm ở bước **Đã xác nhận**, cùng điều kiện thanh toán. Luồng hoàn tiền MoMo không tự động; đơn đã trả tiền cần đối soát và xử lý riêng.
- Luồng xử lý: Chờ xử lý → Đã xác nhận → Đang giao hàng → Đã giao hàng. Đơn MoMo phải trả tiền trước khi chuyển sang giao hàng. Đơn trực tiếp có thể được thu tiền khi giao.
- Ngừng bán sản phẩm là đổi `TrangThai=0`, không xóa lịch sử hóa đơn. Hãng/khuyến mãi đang được dùng không bị xóa.

## 5. Cấu hình Google (khi bạn đã có thông tin tích hợp)

Hiện `Authentication:Google:Enabled=false`, nên nút Google có hiển thị nhưng chưa bật. Đăng ký/đăng nhập thường hoạt động độc lập.

1. Tạo project trên Google Cloud; cấu hình màn hình đồng ý OAuth/Google Auth Platform.
2. Tạo OAuth Client loại **Web application**.
3. Thêm Authorized redirect URI chính xác: `https://localhost:7217/signin-google`.
4. Nếu ứng dụng ở chế độ Testing, thêm email dùng thử vào danh sách test users khi cấu hình Google yêu cầu.
5. Chuột phải project **Shopdienthoai** → Manage User Secrets, hoặc trong thư mục project chạy:

```powershell
dotnet user-secrets set "Authentication:Google:Enabled" "true"
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

6. Khởi động lại ứng dụng và thử nút **Tiếp tục với Google**.
7. Lần đầu: xác thực Google, điền họ tên/số điện thoại/địa chỉ, hoàn tất tạo tài khoản. Khi đó email chào mừng được thêm vào hàng đợi một lần. Các lần đăng nhập sau không gửi lại thư chào mừng.
8. Nếu email Google đã có tài khoản thường: đăng nhập bằng mật khẩu trước, vào Hồ sơ → Liên kết Google với cùng email. Ứng dụng không tự ghép tài khoản chỉ dựa trên email chưa chứng minh quyền sở hữu.

Không cần gửi Client Secret vào cuộc trò chuyện. Khi dùng domain khác hoặc đường hầm HTTPS, đăng ký thêm URI `/signin-google` của chính domain đó trong Google Cloud.

Tài liệu: [Google OAuth trong ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-8.0), [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect).

## 6. Cấu hình MoMo Sandbox

Hiện MoMo tắt vì bạn chưa có tài khoản/khóa Test. Dự án không kèm khóa dùng chung và không giả lập “đã thanh toán” khi chưa gọi được dịch vụ.

1. Đăng ký tài khoản tích hợp MoMo theo hướng dẫn nhà cung cấp; lấy PartnerCode, AccessKey và SecretKey của môi trường Test.
2. Chuẩn bị một domain/đường hầm **HTTPS truy cập được từ Internet** đến website để MoMo gửi IPN. `localhost` không nhận được callback từ máy chủ MoMo.
3. Đặt User Secrets trong project:

```powershell
dotnet user-secrets set "Momo:Enabled" "true"
dotnet user-secrets set "Momo:PartnerCode" "YOUR_TEST_PARTNER_CODE"
dotnet user-secrets set "Momo:AccessKey" "YOUR_TEST_ACCESS_KEY"
dotnet user-secrets set "Momo:SecretKey" "YOUR_TEST_SECRET_KEY"
dotnet user-secrets set "Momo:PublicBaseUrl" "https://YOUR_PUBLIC_HTTPS_DOMAIN"
```

4. Giữ `Momo:BaseUrl=https://test-payment.momo.vn`. Bản đồ án chỉ cho phép Sandbox.
5. Khi kiểm thử qua đường hầm, mở shop bằng **chính domain PublicBaseUrl** từ lúc đăng nhập đến đặt hàng để cookie đăng nhập khớp domain. Nếu tunnel chuyển tiếp HTTP, cần bảo đảm X-Forwarded-Proto được chuyển đúng từ proxy tin cậy ở máy local; middleware đã bật hỗ trợ forwarded headers với danh sách proxy tin cậy mặc định.
6. Đăng nhập, chọn sản phẩm, đặt đơn với MoMo. Tại chi tiết đơn, chọn thanh toán để đến trang MoMo.
7. Dùng ứng dụng/tài khoản **MoMo Test** đúng hướng dẫn MoMo; không dùng ví thật cho luồng Sandbox.
8. Kiểm tra IPN `/Payment/Ipn` và trang chi tiết đơn. Nếu mất kết nối ở bước quay về, chọn **Kiểm tra kết quả**; không tạo thêm đơn để thử lại cùng giao dịch chưa rõ trạng thái.

Giao dịch lưu `OrderId`, `RequestId`, số tiền và `TransId`; xử lý IPN kiểm tra chữ ký, mã đơn/yêu cầu, số tiền và không ghi đè thành công bằng một thông báo lỗi đến trễ. Quản trị không thể dùng nút thu tiền trực tiếp để đánh dấu trả tiền MoMo.

Tài liệu: [Tích hợp](https://developers.momo.vn/v3/vi/docs/payment/onboarding/integration-process/), [Tạo giao dịch](https://developers.momo.vn/v3/vi/docs/payment/api/wallet/onetime/), [IPN](https://developers.momo.vn/v3/vi/docs/payment/api/result-handling/notification/), [Query](https://developers.momo.vn/v3/vi/docs/payment/api/payment-api/query/).

## 7. Cấu hình email gửi về Gmail của khách

Có thể dùng Gmail của shop để gửi SMTP. Mặc định tắt để không gửi nhầm email từ dữ liệu demo.

1. Chọn Gmail do shop quản lý, bật xác minh 2 bước.
2. Nếu tài khoản cho phép, tạo **App Password** dành cho ứng dụng này. Tài khoản tổ chức có thể bị quản trị viên giới hạn tính năng; làm theo chính sách tài khoản đó. Không dùng mật khẩu Gmail thông thường trong source.
3. Cấu hình User Secrets:

```powershell
dotnet user-secrets set "Email:Enabled" "true"
dotnet user-secrets set "Email:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:UserName" "YOUR_SHOP_GMAIL"
dotnet user-secrets set "Email:FromAddress" "YOUR_SHOP_GMAIL"
dotnet user-secrets set "Email:Password" "YOUR_GMAIL_APP_PASSWORD"
```

4. Khởi động lại website. Worker kiểm tra hàng đợi mỗi 30 giây. Xem Admin → Tích hợp & email để theo dõi hoặc đưa thư lỗi về hàng đợi.
5. Thử bằng email do bạn quản lý: đăng ký Google hoàn tất để nhận thư chào mừng; đặt đơn để nhận xác nhận mã đơn, sản phẩm, tiền, người nhận, địa chỉ và phương thức/trạng thái thanh toán tại thời điểm đặt.

Email xác nhận đơn không có nghĩa là đã trả tiền. Nếu chưa cấu hình SMTP, thư vẫn nằm trong `EMAILHANGDOI`. Worker thử tối đa 5 lần với khoảng chờ tăng dần. SMTP có thể tiếp nhận thư nhưng ngắt kết nối trước khi ứng dụng ghi nhận, nên việc thử lại có khả năng gửi trùng trong trường hợp này. Bản đồ án dành cho một tiến trình website/worker; chưa triển khai cơ chế nhiều worker cùng nhận việc.

Tài liệu: [SMTP Gmail](https://developers.google.com/workspace/gmail/imap/imap-smtp), [App Password](https://support.google.com/accounts/answer/185833).

## 8. SQL đã mở rộng những gì?

Giữ 10 bảng nghiệp vụ và 22 sản phẩm có ảnh từ SQL đã thống nhất, bổ sung:

| Vị trí | Nội dung |
| --- | --- |
| NGUOIDUNG | GoogleId, CoMatKhau; unique cho tên tài khoản/email và GoogleId có giá trị |
| HOADON | TrangThaiThanhToan; MaDatHang duy nhất để chống lưu trùng |
| HANGKHACHHANG | NguongChiTieu để Admin sửa ngưỡng xét hạng |
| GIAODICHTHANHTOAN | Theo dõi từng lần yêu cầu và đối soát MoMo |
| EMAILHANGDOI | Giữ email cần gửi, kết quả và số lần thử |

File cập nhật không tự suy ra các đơn lịch sử đã thanh toán. File tạo demo mới có xác nhận thanh toán cho các đơn mẫu đã giao, và tính lại tổng số/điểm đánh giá theo các đánh giá còn lại.

## 9. Kiểm tra và phạm vi đã xác minh

Có chương trình kiểm tra bằng HTTP qua pipeline MVC thật, dùng một database SQLite **riêng cho kiểm thử**. Ứng dụng chính vẫn cấu hình SQL Server; lớp TestContext chỉ điều chỉnh biểu thức computed/default và kiểu sắp xếp ngưỡng để chạy SQLite.

Chạy trong thư mục solution:

```powershell
dotnet run --project .\KiemThu\KiemThu.csproj -c Release
```

Kết quả cuối cùng của lần kiểm thử đính kèm nằm trong `KIEM_THU.md`.

Đã kiểm tra build .NET 8, render Razor cho trang khách/Admin, CSRF, phân quyền, quyền sở hữu đơn, số lượng/tồn kho, chống đơn trùng, hủy đơn, chốt giá/ưu đãi, duyệt VIP, đánh giá, upload ảnh, xác nhận thu tiền và callback MoMo có chữ ký mẫu.

**Chưa kiểm thử kết nối SQL Server thực tế trên máy bạn; chưa chạy OAuth Google thật, thanh toán MoMo Test thật hoặc gửi SMTP thật vì chưa có thông tin dịch vụ.** Những chức năng này đã có mã xử lý và cấu hình, cần hoàn tất các mục 5–7 để kiểm thử với tài khoản của bạn. Cũng cần mở website trên máy/điện thoại của bạn để duyệt giao diện thực tế; môi trường tạo dự án chưa kết nối được trình duyệt kiểm tra với máy chủ local.

Không đưa thư mục bin/obj/.vs, database kiểm thử hay khóa bí mật vào gói bàn giao. Nếu phát sinh lỗi, gửi nội dung lỗi và thao tác gây lỗi để đối chiếu, tránh gửi mật khẩu/khóa dịch vụ.
