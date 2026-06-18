# NexusGear — Cửa hàng phụ kiện gaming cao cấp

Website thương mại điện tử chuyên cung cấp phụ kiện gaming (chuột, bàn phím, tai nghe, ghế gaming…) được thiết kế hiện đại, đậm chất eSports. Dự án kết hợp sức mạnh của hệ sinh thái **ASP.NET Core** ở Backend và trải nghiệm giao diện người dùng sống động bằng **Vanilla JS, CSS Animations & Bootstrap 5** ở Frontend.

---

## 🛠️ Ngăn xếp công nghệ (Tech Stack) chi tiết

Dự án chú trọng đến tốc độ, sự tối ưu và hiệu ứng hình ảnh (Aesthetics). Dưới đây là phân bổ công nghệ cho từng hạng mục cụ thể:

### 1. Frontend: UI/UX & Client Logic
- **Bootstrap 5 (`bootstrap.min.css` & `bootstrap.bundle.min.js`)**:
  - Dùng để xây dựng **hệ thống lưới (Grid system)** responsive (container, row, col).
  - Tận dụng các **Utility classes** cơ bản (m-*, p-*, d-flex) để dàn trang nhanh.
- **Custom CSS3 (`gaming-theme.css` & `site.css`)**:
  - **CSS Variables (Biến CSS)**: Xây dựng cơ chế **Dark Mode** toàn diện (`html[data-theme="dark"]`). Các biến số `--bg`, `--bg-card`, `--text`, `--accent` tự động thay đổi theo theme.
  - **Glassmorphism**: Áp dụng hiệu ứng mờ kính (`backdrop-filter: blur`) cho thanh điều hướng Sticky Header, các thẻ số liệu và nút bấm.
  - **CSS Animations**: Sử dụng `@keyframes` xử lý chuyển động mượt mà cho: 
    - **Promo Ticker**: Thanh chữ chạy ngang liên tục (`translateX(-50%)`).
    - **Neon Pulse**: Hiệu ứng phát sáng liên tục cho khu vực Flash Sale.
- **Vanilla JavaScript (JS thuần, không dùng jQuery)**:
  - **Logic Lọc Sản Phẩm (Category Filter)**: Bắt sự kiện click vào các "pill" danh mục ở trang chủ (Bàn phím, Chuột...) để ẩn/hiện (`display: none`) các thẻ sản phẩm `product-card` tức thời mà không cần tải lại trang.
  - **Logic Custom Hero Slider**: Tạo thuật toán tính toán `translateX` đẩy các slide từ phải sang trái định kỳ mỗi 5s, tự động dừng (pause) khi hover, và đồng bộ với các nút Dot Indicators.
  - **Logic Theme Toggle**: Chuyển đổi qua lại giữa Dark/Light mode và lưu cấu hình.

### 2. Backend & Database
- **ASP.NET Core MVC (.NET 10)**: Xử lý routing, Controllers và render Razor Views.
- **Entity Framework Core 10 (Code First)**: Quản lý ORM, migration dữ liệu.
- **ASP.NET Core Identity**: Quản lý xác thực, cấp quyền, phân quyền Admin/Customer.
- **SQL Server (LocalDB)**: Lưu trữ cơ sở dữ liệu.

---

## 🚀 Tính năng chính và Logic xử lý

### 🌟 Tính năng Khách hàng (Storefront)
1. **Trang chủ động (Dynamic Homepage)**:
   - **Hero Slider**: Trượt ngang tự động 4 banner quảng cáo khổ lớn. (Dùng `Vanilla JS` + `CSS transform`).
   - **Thanh Ticker**: Chữ chạy quảng cáo vô tận (Dùng `CSS animation` + lặp nội dung 2 lần).
   - **Lưới sản phẩm nổi bật**: Lọc trực tiếp trên giao diện Frontend (Dùng `Vanilla JS` so sánh `data-filter` và `data-category`).
2. **Cửa hàng & Danh mục**: 
   - Phân trang, tìm kiếm sản phẩm.
   - Lọc sản phẩm theo khoảng giá, kết nối, switch (Backend Query).
3. **Giỏ hàng (Cart)**:
   - Gộp Session Cart vào Database Cart khi người dùng đăng nhập thành công.
   - Hiển thị badge số lượng sản phẩm trên Navbar.
4. **Tương tác sản phẩm**:
   - Thêm vào Wishlist (Yêu thích).
   - Đưa vào tính năng So sánh sản phẩm.
5. **Thanh toán (Checkout)**: 
   - Quy trình Checkout 2 bước. Hỗ trợ mô phỏng thanh toán VNPay/Thẻ.
6. **Tài khoản cá nhân**: 
   - Quản lý hồ sơ, đổi mật khẩu.
   - Xem chi tiết Lịch sử đơn hàng, có khả năng hủy đơn.

### ⚙️ Tính năng Quản trị (Admin Panel)
1. **Dashboard**: Báo cáo tổng doanh thu, biểu đồ, danh sách sản phẩm sắp hết hàng.
2. **Quản lý Sản phẩm & Danh mục**: Thêm/Sửa/Xóa, tải ảnh bằng File Upload.
3. **Quản lý Đơn hàng**: Chuyển trạng thái đơn (Chờ xử lý → Đang giao → Hoàn thành).

---

## 🖥️ Hướng dẫn cài đặt & Chạy dự án

### Yêu cầu hệ thống
- [.NET SDK 10](https://dotnet.microsoft.com/download) trở lên
- [SQL Server LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) (Thường đi kèm Visual Studio)
- *Tùy chọn*: Công cụ dòng lệnh EF Core (`dotnet tool install --global dotnet-ef --version 10.0.8`)

### Các bước khởi chạy

**Bước 1:** Di chuyển vào thư mục ứng dụng
```bash
cd DoAnLapTrinhWeb\WebApplication1\WebApplication1
```

**Bước 2:** Build dự án
```bash
dotnet restore
dotnet build
```

**Bước 3:** Tạo cơ sở dữ liệu (Database Update)
*Lưu ý: Lần chạy đầu tiên, DbSeeder sẽ tự động tạo cấu trúc và nạp hàng loạt dữ liệu mẫu (Sản phẩm, User, Slider).*
```bash
dotnet ef database update
```

**Bước 4:** Chạy ứng dụng
```bash
dotnet run
```
Truy cập: `http://localhost:5224`

---

## 👤 Tài khoản mẫu (Seeded Accounts)

| Vai trò | Email đăng nhập | Mật khẩu |
|---------|-------|----------|
| **Admin** | `admin@nexusgear.local` | `Admin@123` |

*(Để test quyền Khách hàng (Customer), vui lòng nhấn nút Đăng ký trên giao diện).*

---

## 📁 Cấu trúc thư mục nổi bật

```text
DoAnLapTrinhWeb/
├── WebApplication1/WebApplication1/
│   ├── Areas/Admin/         # Khu vực CMS dành cho Quản trị viên
│   ├── Controllers/         # Logic Backend (Home, Product, Cart, Account...)
│   ├── Views/               # Razor Views cho Frontend
│   │   ├── Shared/          # Các layout dùng chung (_Layout.cshtml, _LayoutHome.cshtml)
│   │   └── Home/Index.cshtml# Trang chủ chứa logic Slider, Ticker, JS Lọc sản phẩm
│   ├── wwwroot/
│   │   ├── css/             # File CSS tuỳ chỉnh (gaming-theme.css quan trọng nhất)
│   │   ├── js/              # File JS (site.js, chatbot.js)
│   │   └── bootstrap/       # Thư viện Bootstrap local
│   └── Data/DbSeeder.cs     # Nơi nạp dữ liệu mẫu tự động
└── README.md
```

## ⚠️ Lưu ý kỹ thuật

- Thanh toán bằng thẻ là mô phỏng.
- CSS của ứng dụng được thiết kế ưu tiên kỹ thuật **Breakout Container** (dùng `100vw`) để thanh trượt Slider ở trang chủ có thể kéo giãn toàn màn hình mà không bị bóp nghẹt bởi class `.container` của Bootstrap.
- Mọi logic Slide trượt và Lọc danh mục đều dùng DOM Javascript thuần không phụ thuộc jQuery để tối ưu hóa hiệu năng render.
