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
- **ASP.NET Core Identity**: Quản lý xác thực, cấp quyền, phân quyền Admin/Manager/Customer.
- **SQL Server (LocalDB)**: Lưu trữ cơ sở dữ liệu.

---

## 🚀 Các tính năng chính của hệ thống (Đã cập nhật các cải tiến)

### 🌟 1. Tính năng Khách hàng (Storefront)
- **Trang chủ động (Dynamic Homepage):**
  - **Hero Slider:** Trượt ngang tự động 4 banner quảng cáo khổ lớn (Dùng Vanilla JS + CSS transform).
  - **Thanh Ticker:** Chữ chạy quảng cáo vô tận (Dùng CSS animation + lặp nội dung 2 lần).
  - **Lưới sản phẩm nổi bật:** Lọc trực tiếp trên giao diện Frontend (Dùng Vanilla JS so sánh `data-filter` và `data-category`).
- **Cửa hàng & Bộ lọc (Shop & Catalog):**
  - Phân trang, tìm kiếm sản phẩm.
  - Lọc sản phẩm theo khoảng giá, kết nối, switch (Backend Query).
- **Trang Danh mục Sản phẩm (Redesigned Category Page) — *[ĐÃ NÂNG CẤP]*:**
  - Được thiết kế lại toàn bộ theo phong cách Glassmorphism và Dark Gaming Gradient.
  - Tích hợp các micro-interactions khi hover lên các thẻ danh mục con: nâng chiều cao thẻ (`translateY`), viền đổi màu tím sáng, phát bóng đổ neon, icon phóng to nhẹ và tự động xoay 5 độ.
- **Trang chi tiết sản phẩm & Thư viện ảnh phụ — *[ĐÃ THÊM MỚI]*:**
  - Hiển thị danh sách ảnh phụ (thumbnails) bo góc mềm mại phía dưới ảnh chính. 
  - Người dùng có thể click chọn ảnh phụ để đổi ảnh chính tức thì kèm hiệu ứng mờ dần (opacity transition 150ms) và tô viền nổi bật cho thumbnail active.
  - Tự động sinh các ảnh phụ giả lập từ ảnh chính (`?v=1`, `?v=2`...) nếu cơ sở dữ liệu trống.
- **Giỏ hàng (Cart):**
  - Gộp Session Cart vào Database Cart khi người dùng đăng nhập thành công.
  - Hiển thị badge số lượng sản phẩm trên Navbar.
- **Tương tác sản phẩm:**
  - Thêm vào Wishlist (Yêu thích).
  - Đưa vào tính năng So sánh sản phẩm.
- **Quy trình thanh toán (Checkout) & Tính phí ship thông minh — *[ĐÃ THAY THẾ & NÂNG CẤP]*:**
  - **Bộ chọn địa chỉ liên kết (Cascade Selectors):** Thay thế ô nhập địa chỉ cũ bằng 3 dropdown động **Tỉnh/Thành phố → Quận/Huyện → Phường/Xã** kết nối trực tiếp với API `open-api.vn`.
  - **Tính số km tự động (Nominatim + OSRM):** Loại bỏ hoàn toàn gán cứng km. Hệ thống gọi API Nominatim lấy tọa độ địa chỉ khách và gọi OSRM để đo khoảng cách thực tế lái xe theo thời gian thực từ cửa hàng (`10/80c Song Hành Xa Lộ Hà Nội, Tăng Nhơn Phú, Thủ Đức, TP. Hồ Chí Minh`).
  - **Công thức phí ship tự động:**
    - *Nội thành (< 10km):* Cố định **40,000₫**. Mỗi km thêm cộng thêm **4,000₫/km**.
    - *Ngoại thành:* Cứ thêm mỗi **20km** hành trình thì cộng thêm **20,000₫**.
  - Tích hợp hộp cảnh báo đỏ và tự động gán fallback khoảng cách về `0.0 km` khi xảy ra lỗi định vị.
- **Tài khoản cá nhân:**
  - Quản lý hồ sơ, đổi mật khẩu.
  - Xem chi tiết Lịch sử đơn hàng, hỗ trợ hủy đơn hàng trực tiếp.
  - **Ràng buộc đánh giá sản phẩm — *[ĐÃ THÊM MỚI]*:** Chỉ những khách hàng đã mua sản phẩm đó thành công (đơn hàng ở trạng thái `Completed`) mới hiển thị form và được quyền gửi đánh giá & nhận xét sản phẩm. Khách hàng chưa mua sẽ thấy dòng cảnh báo đỏ.
- **Thông tin chân trang (Footer) — *[ĐÃ THÊM MỚI]*:** Bổ sung địa chỉ cố định, hotline và email hỗ trợ của cửa hàng ở dưới cùng mọi trang.

### ⚙️ 2. Tính năng Quản trị & Quản lý (Admin & Manager Panel)
- **Dashboard quản trị:** Báo cáo doanh thu, thống kê đơn hàng, biểu đồ trực quan, danh sách sản phẩm sắp hết hàng.
- **Quản lý Danh mục & Đơn hàng:** Quản lý cấu trúc danh mục, chuyển đổi trạng thái đơn hàng (Chờ xử lý → Đang giao → Hoàn thành).
- **Quản lý Sản phẩm — *[ĐÃ NÂNG CẤP]*:**
  - Tích hợp ô chọn file upload hàng loạt ảnh phụ (`SecondaryImageFiles` với thuộc tính `multiple` và `accept="image/*"`).
  - Cho phép xem trước danh sách ảnh phụ hiện tại của sản phẩm khi chỉnh sửa (Edit).
  - Tự động dọn dẹp và xóa các tệp ảnh phụ vật lý cũ trên máy chủ khi cập nhật bộ ảnh phụ mới hoặc khi xóa sản phẩm.
- **Tương tác trực tiếp tại Storefront — *[ĐÃ THÊM MỚI]*:**
  - Khi đăng nhập bằng tài khoản có vai trò **Admin** hoặc **Manager**, hệ thống sẽ ẩn nút *"Thêm vào giỏ hàng"* tại tất cả các trang (Trang chủ, Cửa hàng, Chi tiết) để tránh nhầm lẫn hành vi mua sắm.
  - Thay thế bằng các nút quản lý trực quan: **Sửa sản phẩm** (link về trang Edit của CMS), **Xóa sản phẩm** (POST action có yêu cầu xác nhận) và các nút **Thêm sản phẩm mới**.

---

## 🖥️ Hướng dẫn cài đặt & Chạy dự án

### Yêu cầu hệ thống
- [.NET SDK 10](https://dotnet.microsoft.com/download) trở lên
- [SQL Server LocalDB / Express](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) 
- Công cụ dòng lệnh EF Core (`dotnet tool install --global dotnet-ef --version 10.0.8`)

### Các bước khởi chạy

**Bước 1:** Di chuyển vào thư mục ứng dụng
```bash
cd WebApplication1/WebApplication1
```

**Bước 2:** Build dự án
```bash
dotnet restore
dotnet build
```

**Bước 3:** Tạo cơ sở dữ liệu (Database Update)
*Lưu ý: DbSeeder sẽ tự động chạy trong lần chạy đầu để tạo dữ liệu mẫu, tài khoản admin/manager, sliders, danh mục và các sản phẩm.*
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
| **Manager** | `manager@nexusgear.local` | `Manager@123` |

*(Để kiểm tra giao diện và tính năng Khách hàng (Customer), vui lòng nhấn nút Đăng ký trên giao diện hoặc tạo tài khoản mới).*

---

## 📁 Cấu trúc thư mục nổi bật

```text
DoAnLapTrinhWeb/
├── WebApplication1/WebApplication1/
│   ├── Areas/Admin/         # Khu vực CMS dành cho Quản trị viên (Product, Order, Category...)
│   ├── Controllers/         # Logic Backend (Home, Product, Cart, Account, Checkout...)
│   ├── Views/               # Razor Views cho Frontend (Giao diện Details, Index, Category...)
│   │   ├── Shared/          # Các layout dùng chung (_Layout.cshtml, _LayoutHome.cshtml)
│   │   └── Home/Index.cshtml# Trang chủ chứa logic Slider, Ticker, JS Lọc sản phẩm
│   ├── wwwroot/
│   │   ├── css/             # File CSS tuỳ chỉnh (gaming-theme.css quan trọng nhất)
│   │   ├── js/              # File JS (site.js, chatbot.js)
│   │   └── bootstrap/       # Thư viện Bootstrap local
│   └── Data/DbSeeder.cs     # Nơi nạp dữ liệu mẫu tự động và gán dữ liệu test ban đầu
└── README.md
```

## ⚠️ Lưu ý kỹ thuật

- Thanh toán bằng thẻ là mô phỏng.
- CSS của ứng dụng được thiết kế ưu tiên kỹ thuật **Breakout Container** (dùng `100vw`) để thanh trượt Slider ở trang chủ có thể kéo giãn toàn màn hình mà không bị bóp nghẹt bởi class `.container` của Bootstrap.
- Mọi logic Slide trượt và Lọc danh mục đều dùng DOM Javascript thuần không phụ thuộc jQuery để tối ưu hóa hiệu năng render.
