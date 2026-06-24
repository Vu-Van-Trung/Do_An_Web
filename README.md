# NexusGear — Cửa hàng phụ kiện gaming cao cấp

Website thương mại điện tử chuyên bán phụ kiện gaming (chuột, bàn phím, tai nghe, ghế gaming…) xây dựng bằng **ASP.NET Core MVC (.NET 10)** kết hợp giao diện **Glassmorphism** phong cách eSports.

---

## Tech Stack

| Hạng mục | Công nghệ |
|---|---|
| **Backend** | ASP.NET Core MVC (.NET 10) |
| **ORM** | Entity Framework Core 10 — Code First, Migrations |
| **Database** | SQL Server LocalDB |
| **Auth** | ASP.NET Core Identity + Google OAuth 2.0 |
| **Real-time** | SignalR (thông báo đơn hàng mới cho Admin) |
| **Background** | `IHostedService` — OrderTimeoutWorker |
| **Email** | SMTP qua `IEmailService` (xác nhận đơn + reset mật khẩu) |
| **Thanh toán** | VNPay (cổng thanh toán), COD |
| **AI Chat** | OpenAI Streaming API (Server-Sent Events) |
| **Frontend** | Bootstrap 5, Vanilla JS, CSS3 Glassmorphism, CSS Animations |
| **Pattern** | Repository + Service Layer, Policy-based Authorization |

---

## Tính năng hệ thống

### Khách hàng (Storefront)

**Xác thực & Tài khoản**
- Đăng ký / Đăng nhập bằng **Email hoặc Số điện thoại**
- Đăng nhập bằng **Google OAuth** (tự động liên kết tài khoản nếu email đã tồn tại)
- Quên mật khẩu — gửi email reset (có link trực tiếp hiển thị màn hình để test)
- Quản lý hồ sơ cá nhân, đổi mật khẩu
- Xem & hủy đơn hàng cá nhân (chỉ hủy được khi trạng thái `Pending`)

**Sản phẩm & Danh mục**
- Trang chủ: Hero Slider, Ticker quảng cáo, lưới sản phẩm nổi bật theo danh mục
- Trang cửa hàng: phân trang, tìm kiếm full-text, lọc theo giá / danh mục / thương hiệu / specs (kết nối, switch type, DPI)
- Trang chi tiết sản phẩm: gallery ảnh phụ, thông số kỹ thuật, đánh giá khách hàng
- Trang danh mục: cây 2 tầng (cha → con), icon emoji, redirect đặc biệt cho slug `hot-deals`
- So sánh sản phẩm (tối đa 4 sản phẩm, chỉ trong cùng danh mục)
- Trang khuyến mãi: liệt kê toàn bộ voucher đang hoạt động

**Giỏ hàng (Hybrid Cart)**
- Khách vãng lai: lưu theo Session (GUID)
- Đã đăng nhập: lưu theo UserId trong DB
- Khi đăng nhập / Google OAuth: giỏ hàng Session tự động **merge** vào tài khoản
- Cập nhật số lượng real-time (AJAX), badge đếm trên Navbar

**Checkout — 2 bước**
- **Bước 1 — Địa chỉ giao hàng:** Tự điền sẵn từ profile, chọn tỉnh/thành + dịch vụ ship
- **Bước 2 — Thanh toán:** Xem tóm tắt đơn hàng, áp voucher, chọn COD / VNPay

**Tính phí vận chuyển**
- Dựa trên **vùng địa lý** (Nội thành HCM / Miền Nam / Miền Trung / Miền Bắc) và **loại hàng** (Nhỏ × 1.0 / Vừa × 1.5 / Cồng kềnh × 3.0)
- Hàng cồng kềnh nội thành HCM: **miễn phí ship**
- Hàng cồng kềnh liên tỉnh: **liên hệ báo phí**
- 3 dịch vụ: Tiết kiệm / Nhanh / Hỏa tốc

**Hệ thống Voucher (5 loại)**
| PromotionType | Mô tả |
|---|---|
| `Coupon` | Mã giảm % hoặc số tiền cố định |
| `FlashSale` | Deal giới hạn thời gian |
| `FreeShipping` | Miễn phí vận chuyển |
| `FirstOrder` | Ưu đãi đơn hàng đầu tiên (tự động áp) |
| `BuyXGetY` | Mua X tặng Y (cơ sở hạ tầng sẵn sàng) |

**Công thức tính hóa đơn:**
```
Total = Subtotal − DiscountAmount + ShippingFee
```

**Xử lý đơn hàng**
- **COD:** Trừ kho → lưu đơn → gửi email xác nhận (async) → bắn SignalR thông báo Admin
- **VNPay:** Trừ kho → lưu đơn → redirect cổng VNPay → callback xác minh HMAC-SHA512
  - Thanh toán thành công: cập nhật `PaymentStatus = Paid`, gửi email + SignalR
  - Thanh toán thất bại: hoàn kho, cập nhật `PaymentStatus = Failed`
- **OrderTimeoutWorker:** background service quét mỗi 5 phút, tự động hủy & hoàn kho đơn VNPay chưa thanh toán quá 20 phút

**Tính năng khác**
- Wishlist (yêu thích)
- Đánh giá sản phẩm (chỉ khách đã mua — đơn `Completed` mới được review)
- AI Chatbot tư vấn gaming (OpenAI Streaming, SSE)

---

### Quản trị (Admin Panel)

**Phân quyền 12 Permission** — mỗi permission là 1 Policy riêng trong ASP.NET Core Authorization. Admin bypass tất cả; Manager/Staff/Customer có thể được gán từng quyền cụ thể qua ma trận quyền hạn:

| Permission | Mô tả |
|---|---|
| `XemDashboard` | Xem trang tổng quan |
| `QuanLySanPham` | CRUD sản phẩm + upload ảnh |
| `QuanLyDanhMuc` | CRUD danh mục |
| `QuanLyThuongHieu` | CRUD thương hiệu |
| `XemDonHang` | Xem danh sách đơn hàng |
| `CapNhatTrangThaiDon` | Đổi trạng thái đơn hàng |
| `QuanLyKhuyenMai` | CRUD voucher/discount |
| `XemBaoCaoDoanhThu` | Xem báo cáo + xuất CSV |
| `QuanLyNguoiDung` | Xem, khóa, đổi role, xóa user |
| `PhanQuyenVaiTro` | Chỉnh sửa ma trận quyền hạn |
| `DatHang` | Được phép đặt hàng (dùng cho Checkout) |
| `XemDonHangCuaMinh` | Xem đơn hàng cá nhân |

**Các module Admin:**
- **Dashboard:** Doanh thu, số đơn, biểu đồ 12 tháng, sản phẩm sắp hết hàng, đơn gần đây
- **Sản phẩm:** CRUD đầy đủ, upload ảnh chính + ảnh phụ (xóa file vật lý cũ khi cập nhật/xóa), quản lý thông số kỹ thuật (specs)
- **Danh mục:** CRUD danh mục phân cấp 2 tầng (cha → con)
- **Thương hiệu:** CRUD brand
- **Đơn hàng:** Xem chi tiết, cập nhật trạng thái, **xuất CSV** (UTF-8 BOM, hiển thị đúng tiếng Việt trên Excel)
- **Khuyến mãi:** CRUD voucher với tất cả 5 loại PromotionType
- **Báo cáo:** Doanh thu theo tháng/ngày, top sản phẩm bán chạy, lọc theo khoảng ngày, **xuất CSV**
- **Người dùng:** Xem danh sách, khóa/mở khóa tài khoản, thay đổi role nhanh, xem lịch sử đơn, xóa tài khoản
- **Phân quyền:** Ma trận quyền hạn (Admin/Manager/Staff/Customer × 12 permissions), gán role cho từng user

---

### REST API (`/api/*`)

Song song với giao diện MVC, hệ thống cung cấp REST API JSON đầy đủ phục vụ mobile app hoặc SPA:

| Base URL | Mô tả |
|---|---|
| `GET/POST /api/auth/...` | Đăng nhập, đăng ký, thông tin user, đổi thông tin, đăng xuất |
| `GET /api/products` | Danh sách sản phẩm — tìm kiếm, lọc, phân trang |
| `GET /api/products/{id}` | Chi tiết sản phẩm + specs + reviews |
| `GET/POST /api/products/{id}/reviews` | Danh sách & thêm đánh giá |
| `GET/POST/PUT/DELETE /api/cart/...` | Quản lý giỏ hàng |
| `GET/POST /api/orders/...` | Đặt hàng, xem đơn, hủy đơn |
| `GET /api/categories/...` | Danh sách danh mục, sản phẩm theo danh mục |
| `GET /api/brands/...` | Danh sách thương hiệu |
| `POST /api/discounts/validate` | Kiểm tra & tính giá trị voucher |
| `GET /api/shipping/options` | Tính phí ship theo tỉnh/loại hàng |
| `GET/POST/DELETE /api/wishlist/...` | Quản lý wishlist |
| `POST /api/chat/stream` | AI chat streaming (SSE) |

---

## Cài đặt & Chạy dự án

### Yêu cầu

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (đi kèm Visual Studio) hoặc SQL Server Express
- EF Core CLI: `dotnet tool install --global dotnet-ef --version 10.0.8`

### Các bước

```bash
# 1. Vào thư mục dự án
cd WebApplication1/WebApplication1

# 2. Restore & build
dotnet restore
dotnet build

# 3. Áp migration (DbSeeder tự chạy khi khởi động, không cần bước riêng)
dotnet ef database update

# 4. Chạy ứng dụng
dotnet run
```

Truy cập: `https://localhost:7xxx` hoặc `http://localhost:5xxx` (xem console output)

### Cấu hình tuỳ chọn (`appsettings.json`)

```jsonc
{
  "Authentication": {
    "Google": {
      "ClientId": "...",        // Để trống nếu không dùng Google OAuth
      "ClientSecret": "..."
    }
  },
  "VnPay": {
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "TmnCode": "...",
    "HashSecret": "...",
    "ReturnUrl": ""             // Để trống → tự sinh từ Request.Scheme
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "...",
    "SenderPassword": "..."     // Để trống → email sẽ không gửi (có link test trực tiếp)
  },
  "OpenAI": {
    "ApiKey": "sk-...",         // Để trống → chatbot hiển thị lỗi cấu hình
    "Model": "gpt-4o-mini",
    "SystemPrompt": "..."
  }
}
```

> Tất cả tính năng chính (mua hàng, quản trị, phân quyền) hoạt động bình thường kể cả khi không cấu hình Google OAuth / VNPay / Email / OpenAI.

---

## Tài khoản mẫu (được seed tự động)

| Vai trò | Email | Mật khẩu |
|---|---|---|
| **Admin** | `admin@nexusgear.local` | `Admin@123` |
| **Manager** | `manager@nexusgear.local` | `Manager@123` |

Tài khoản **Customer**: nhấn **Đăng ký** trên giao diện hoặc gọi `POST /api/auth/register`.

---

## Cấu trúc thư mục

```
WebApplication1/WebApplication1/
├── Areas/Admin/
│   ├── Controllers/            # BrandsController, CategoriesController, DiscountsController,
│   │                           # HomeController, OrdersController, ProductsController,
│   │                           # ReportsController, RolesController, UsersController
│   └── Views/                  # Razor Views của Admin Panel
├── Controllers/
│   ├── Api/                    # REST API Controllers (JSON) — AuthController, CartApiController,
│   │                           # CategoriesController, DiscountsController, OrdersController,
│   │                           # ProductsController, ShippingController, WishlistApiController
│   ├── AccountController.cs    # Đăng ký, đăng nhập, Google OAuth, hồ sơ, reset mật khẩu
│   ├── CartController.cs       # Giỏ hàng (MVC)
│   ├── CategoryController.cs   # Trang danh mục khách hàng
│   ├── ChatController.cs       # AI Chatbot streaming endpoint
│   ├── CheckoutController.cs   # Checkout 2-bước + VNPay callback
│   ├── ComparisonController.cs # So sánh sản phẩm
│   ├── HomeController.cs       # Trang chủ
│   ├── ProductController.cs    # Danh sách, chi tiết, đánh giá, deals
│   └── WishlistController.cs   # Wishlist (MVC)
├── Data/
│   ├── ApplicationDbContext.cs # EF Core DbContext + model configuration
│   └── DbSeeder.cs             # Seed roles, admin account, permissions, vouchers
├── Models/                     # ApplicationUser, Product, Category, Brand, Order, OrderItem,
│                               # CartItem, Discount, ProductReview, WishlistItem, ...
├── Repositories/               # Generic Repository<T> + IProductRepository, IOrderRepository,
│                               # ICategoryRepository, IBrandRepository, IDiscountRepository
├── Services/
│   ├── CartService.cs          # Hybrid cart (Session/DB), merge khi đăng nhập
│   ├── DiscountService.cs      # Logic ApplyDiscount (BuyXGetY, Coupon, ...)
│   ├── EmailService.cs         # Gửi email xác nhận đơn + reset mật khẩu
│   ├── OrderHub.cs             # SignalR Hub — thông báo đơn mới real-time
│   ├── OrderTimeoutWorker.cs   # Background: hủy đơn VNPay quá 20 phút, hoàn kho
│   ├── ShippingCalculator.cs   # Tính phí ship theo vùng + loại hàng
│   └── VnPayService.cs         # Tạo payment URL + xác minh callback HMAC-SHA512
├── ViewModels/                 # CartViewModel, CheckoutViewModels, CatalogViewModel, ...
├── Views/                      # Razor Views phía khách hàng
├── Migrations/                 # EF Core Migrations
├── wwwroot/
│   ├── css/
│   │   ├── gaming-theme.css    # CSS Variables, Dark/Light Mode, Glassmorphism, Animations
│   │   ├── chatbot.css         # Giao diện AI Chatbot
│   │   └── site.css            # Style chung
│   ├── js/
│   │   ├── site.js             # Hero Slider, Ticker, Category Filter, Theme Toggle
│   │   └── chatbot.js          # AI Chat — gửi SSE request, render streaming token
│   └── images/                 # Ảnh sản phẩm, placeholder, uploads
└── appsettings.json
```

---

## Ghi chú kỹ thuật

- **Snapshot giá:** `OrderItem` lưu `ProductName` + `UnitPrice` tại thời điểm đặt hàng — lịch sử đơn không bị ảnh hưởng khi giá sản phẩm thay đổi sau đó.
- **Phân quyền:** `Admin` mặc định có tất cả quyền (bypass assertion). Các role khác hoạt động theo claim `Permission` được gán qua ma trận quyền hạn.
- **Upload ảnh (DEBUG):** Trong môi trường Debug, ảnh được lưu vào `wwwroot/images/uploads` của source code và đồng bộ sang thư mục `bin` để tránh mất file khi rebuild.
- **API vs MVC:** Cả hai dùng chung `CartService`, `OrderRepository`, `DiscountRepository` — không trùng lặp business logic. API phù hợp để xây thêm mobile app mà không cần viết lại backend.
- **SignalR endpoint:** `/orderHub` — Admin panel lắng nghe event `ReceiveNewOrderNotification` để hiển thị toast thông báo đơn mới ngay lập tức.
