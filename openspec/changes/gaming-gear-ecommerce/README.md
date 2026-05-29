# Gaming Gear E-Commerce Platform - OpenSpec Change Guide

Tài liệu này hướng dẫn chi tiết cách tiếp cận, cấu trúc và kế hoạch triển khai của tính năng/dự án **Nền tảng Thương mại Điện tử Phụ kiện Gaming (Gaming Gear E-commerce)** sử dụng **ASP.NET Core MVC + Entity Framework Core + SQL Server**.

Tài liệu OpenSpec này được thiết kế để bất kỳ lập trình viên hoặc AI Agent nào khi tiếp cận lần đầu đều có thể hiểu ngay lập tức và triển khai dự án một cách chính xác.

---

## 📂 1. Cấu trúc Tài liệu OpenSpec

Thư mục `openspec/changes/gaming-gear-ecommerce/` chứa toàn bộ đặc tả thiết kế và danh sách công việc:

1. **`proposal.md` (Đề xuất)**
   - Trình bày lý do tại sao xây dựng nền tảng này và các tính năng mới cần có.
   - Xác định 5 năng lực cốt lõi (Capabilities) của hệ thống.
2. **`design.md` (Bản thiết kế kỹ thuật)**
   - Giải thích kiến trúc hệ thống (MVC + Repository Pattern).
   - Chi tiết về cơ sở dữ liệu (Entity Framework Core Code-First) và cách thiết kế bảng cấu hình phần cứng động (`ProductSpecification`).
   - Định nghĩa phong cách giao diện chủ đạo: **Vibrant Gaming Dark Mode** (với các hiệu ứng Blur Glassmorphism, bóng đổ Neon phản chiếu, sử dụng font chữ Outfit & Inter).
3. **`specs/` (Đặc tả chi tiết tính năng)**
   - Chứa 5 thư mục con tương ứng với 5 năng lực của hệ thống. Mỗi thư mục chứa file `spec.md` mô tả các yêu cầu chức năng dưới dạng kịch bản kiểm thử (`WHEN... THEN...`):
     - `product-catalog/spec.md`: Xem danh sách, tìm kiếm, lọc theo thông số (DPI, Switch...) và xem chi tiết sản phẩm.
     - `shopping-cart/spec.md`: Quản lý giỏ hàng (thêm, cập nhật, xóa) và cơ chế gộp giỏ hàng ẩn danh khi đăng nhập.
     - `user-auth/spec.md`: Đăng ký, đăng nhập và phân quyền bảo mật (Admin và Customer) sử dụng ASP.NET Core Identity.
     - `order-checkout/spec.md`: Quy trình thanh toán, lưu hóa đơn và xem lịch sử đơn hàng.
     - `admin-dashboard/spec.md`: Giao diện quản trị CRUD sản phẩm, kho hàng, cập nhật trạng thái đơn hàng và biểu đồ thống kê.
4. **`tasks.md` (Danh sách công việc)**
   - Phân rã dự án thành 8 nhóm đầu việc cụ thể với các hộp kiểm `- [ ]`. Đây là file mà lệnh `/opsx-apply` sẽ đọc để triển khai code từng bước.

---

## 🎯 2. Mục tiêu Triển khai (Target Implementation)

Toàn bộ mã nguồn sẽ được xây dựng và tích hợp trực tiếp vào cấu trúc thư mục dự án đã có sẵn tại:
👉 **`WebApplication1/WebApplication1/`**

Chúng tôi sẽ không tạo dự án mới độc lập mà sẽ phát triển trực tiếp trên khung dự án có sẵn này để đảm bảo tính nhất quán.

---

## 🚀 3. Hướng dẫn Triển khai (How to Continue)

### Cách 1: Triển khai Tự động bằng AI (Khuyên dùng)
Hãy gõ lệnh:
```bash
/opsx-apply
```
Lệnh này sẽ tự động đọc file `tasks.md`, cài đặt các gói NuGet cần thiết vào `WebApplication1.csproj`, tạo các thư mục/lớp phù hợp và hoàn thành dự án theo đúng bản thiết kế.

### Cách 2: Triển khai Thủ công
1. Cài đặt các NuGet Packages được liệt kê trong `design.md`.
2. Tạo các Domain Models trong thư mục `WebApplication1/WebApplication1/Models/`.
3. Cấu hình DbContext trong `WebApplication1/WebApplication1/Data/` hoặc `Models/` và kết nối LocalDB SQL Server trong `appsettings.json`.
4. Viết các Controllers & Razor Views tương ứng vào các thư mục `Controllers/` và `Views/`.
5. Tạo `Admin` Area để chứa giao diện quản trị.
6. Chạy ứng dụng bằng lệnh: `dotnet run --project WebApplication1/WebApplication1`.
