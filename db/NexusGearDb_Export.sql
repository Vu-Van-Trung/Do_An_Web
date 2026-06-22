-- ============================================================
-- NexusGear Database Export Script
-- Generated: 2026-06-21 21:31:57
-- Database: NexusGearDb (SQL Server)
-- Project: NexusGear Gaming E-Commerce (.NET 10 ASP.NET Core MVC)
-- ============================================================
-- Cách sử dụng:
--   1. Mở SQL Server Management Studio (SSMS)
--   2. Kết nối tới server của bạn
--   3. Tạo database mới tên NexusGearDb (nếu chưa có)
--   4. Chạy script này trong database NexusGearDb
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'NexusGearDb')
BEGIN
    CREATE DATABASE NexusGearDb;
    PRINT 'Created database NexusGearDb';
END
GO

USE NexusGearDb;
GO

SET NOCOUNT ON;

-- ============================================================
-- BRANDS
-- ============================================================
IF OBJECT_ID('Brands', 'U') IS NULL
BEGIN
    CREATE TABLE Brands (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL
    );
END

SET IDENTITY_INSERT Brands ON;
DELETE FROM Brands;

SET IDENTITY_INSERT Brands OFF;


-- ============================================================
-- CATEGORIES (32 danh mục)
-- ============================================================
IF OBJECT_ID('Categories', 'U') IS NULL
BEGIN
    CREATE TABLE Categories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(200) NULL,
        Description NVARCHAR(500) NULL,
        Icon NVARCHAR(10) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        ParentCategoryId INT NULL
    );
END

SET IDENTITY_INSERT Categories ON;
DELETE FROM Categories;
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (1, N'Thiết bị ngoại vi', 'thiet-bi-ngoai-vi', N'Bàn phím, chuột, tai nghe và phụ kiện gaming', N'🎮', 1, NULL);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (2, N'Linh kiện & Setup góc máy', 'linh-kien-setup', N'Ghế, bàn và phụ kiện trang trí góc máy', N'🖥️', 2, NULL);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (3, N'Phụ kiện Stream & Audio', 'stream-audio', N'Micro, webcam và thiết bị capture stream', N'🎧', 3, NULL);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (4, N'Vệ sinh & Bảo dưỡng', 've-sinh-bao-duong', N'Dụng cụ vệ sinh và bảo dưỡng thiết bị gaming', N'🧼', 4, NULL);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (5, N'Theo Hệ máy', 'theo-he-may', N'Tìm nhanh phụ kiện tương thích với thiết bị của bạn', N'⚡', 5, NULL);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (6, N'Theo Hệ màu / Chủ đề', 'theo-chu-de', N'Đồng bộ góc máy theo tone màu yêu thích', N'🎨', 6, NULL);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (7, N'Danh mục đặc biệt', 'danh-muc-dac-biet', N'Combo, sản phẩm mới và deal giá tốt', N'🔥', 7, NULL);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (8, N'Bàn phím gaming', 'ban-phim-gaming', N'Bàn phím cơ Full-size, TKL, 60% và bàn phím giả cơ', N'⌨️', 1, 1);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (9, N'Chuột gaming', 'chuot-gaming', N'Chuột có dây, không dây, siêu nhẹ và silent', N'🖱️', 2, 1);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (10, N'Tai nghe gaming', 'tai-nghe-gaming', N'Tai nghe chụp tai, nhét tai và soundcard', N'🎧', 3, 1);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (11, N'Lót chuột', 'lot-chuot', N'Lót chuột nhỏ, XXL Deskmat, lót cứng và LED RGB', N'🟦', 4, 1);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (12, N'Tay cầm chơi game', 'tay-cam-choi-game', N'Tay cầm PC/Xbox/PlayStation, vô lăng, cần lái', N'🕹️', 5, 1);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (13, N'Ghế gaming', 'ghe-gaming', N'Ghế công thái học, ghế da PU/Mesh và ghế LED', N'🪑', 1, 2);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (14, N'Bàn gaming', 'ban-gaming', N'Bàn chữ Z, chữ K và bàn nâng hạ thông minh', N'🗄️', 2, 2);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (15, N'Giá đỡ & Arm', 'gia-do-arm', N'Arm màn hình, giá đỡ tai nghe và điện thoại', N'📺', 3, 2);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (16, N'Ánh sáng & Trang trí', 'anh-sang-trang-tri', N'Đèn LED dây RGB, Screenbar và đèn tam giác', N'💡', 4, 2);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (17, N'Microphone', 'microphone', N'Mic thu âm chuyên nghiệp, pop filter, arm treo mic', N'🎙️', 1, 3);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (18, N'Webcam & Đèn', 'webcam-den', N'Webcam Full HD/4K và đèn LED ring light', N'📷', 2, 3);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (19, N'Thiết bị Capture', 'thiet-bi-capture', N'Capture card để stream từ console hoặc 2 PC', N'📡', 3, 3);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (20, N'Dụng cụ vệ sinh bàn phím', 'dung-cu-ve-sinh', N'Keycap puller, cọ quét, bóng thổi bụi', N'🧹', 1, 4);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (21, N'Gel vệ sinh & Dung dịch', 'gel-ve-sinh', N'Gel vệ sinh bụi và dung dịch lau màn hình', N'🧴', 2, 4);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (22, N'Bộ lube switch', 'bo-lube-switch', N'Mỡ Krytox, cọ lube và switch opener', N'🔧', 3, 4);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (23, N'PC Gaming', 'pc-gaming', N'Phụ kiện dành cho game thủ PC', N'🖥️', 1, 5);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (24, N'Console', 'console', N'Phụ kiện PS5, Xbox Series X/S, Nintendo Switch', N'🎮', 2, 5);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (25, N'Mobile Gaming', 'mobile-gaming', N'Nút chơi game, tản nhiệt và tay cầm điện thoại', N'📱', 3, 5);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (26, N'Pink Cyber', 'pink-cyber', N'Góc máy màu hồng/vàng dễ thương', N'🌸', 1, 6);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (27, N'Total Black', 'total-black', N'Phong cách tối giản, đen huyền bí', N'⬛', 2, 6);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (28, N'Snow White', 'snow-white', N'Góc máy trắng tinh khôi', N'⬜', 3, 6);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (29, N'RGB Minimalist', 'rgb-minimalist', N'Đơn giản nhưng phải có đèn đổi màu', N'🌈', 4, 6);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (30, N'Gợi ý góc máy', 'build-your-setup', N'Combo Chuột + Phím + Tai nghe giá ưu đãi', N'⚙️', 1, 7);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (31, N'Hàng mới về', 'new-arrivals', N'Sản phẩm công nghệ và trend mới nhất', N'✨', 2, 7);
INSERT INTO Categories (Id, Name, Slug, Description, Icon, SortOrder, ParentCategoryId) VALUES (32, N'Săn Deal hot', 'hot-deals', N'Hàng giảm giá, xả kho giá siêu tốt', N'🏷️', 3, 7);

SET IDENTITY_INSERT Categories OFF;


-- ============================================================
-- PRODUCTS (18 sản phẩm)
-- ============================================================
IF OBJECT_ID('Products', 'U') IS NULL
BEGIN
    CREATE TABLE Products (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(300) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Price DECIMAL(18,2) NOT NULL,
        Stock INT NOT NULL DEFAULT 0,
        ImageUrl NVARCHAR(500) NULL,
        SecondaryImageUrls NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        ShippingClass INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL,
        CategoryId INT NOT NULL,
        BrandId INT NOT NULL,
        FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
        FOREIGN KEY (BrandId) REFERENCES Brands(Id)
    );
END

SET IDENTITY_INSERT Products ON;
DELETE FROM Products;
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (1, N'Razer DeathAdder V3 Pro', N'Chuột gaming không dây với cảm biến Focus Pro 30K.', 2890000.00, 25, N'/images/uploads/razer-deathadder-v3-pro.webp', N'/images/uploads/razer-deathadder-v3-pro.webp', 1, 0, '2026-06-18T13:24:36.438', 9, 1);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (2, N'Logitech G Pro X Superlight 2', N'Chuột không dây siêu nhẹ dành cho esport.', 3490000.00, 18, N'/images/uploads/logitech-gpx-superlight2.png', N'/images/uploads/logitech-gpx-superlight2.png', 1, 0, '2026-06-18T13:24:36.438', 9, 2);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (3, N'Corsair K70 RGB PRO', N'Bàn phím cơ gaming với switch Cherry MX.', 4290000.00, 12, N'/images/uploads/corsair-k70-rgb-pro.webp', N'/images/uploads/corsair-k70-rgb-pro.webp', 1, 0, '2026-06-18T13:24:36.438', 8, 3);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (4, N'Keychron Q1 Pro', N'Bàn phím cơ không dây tùy biến cao.', 3990000.00, 20, N'/images/uploads/keychron-q1-pro.jpg', N'/images/uploads/keychron-q1-pro.jpg', 1, 0, '2026-06-18T13:24:36.438', 8, 4);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (5, N'Razer BlackShark V2 Pro', N'Tai nghe gaming không dây chuẩn THX.', 5490000.00, 15, N'/images/uploads/razer-blackshark-v2-pro.webp', N'/images/uploads/razer-blackshark-v2-pro.webp', 1, 0, '2026-06-18T13:24:36.438', 10, 1);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (6, N'Logitech G733 Lightspeed', N'Tai nghe không dây RGB nhẹ nhàng.', 2990000.00, 22, N'/images/uploads/logitech-g733.png', N'/images/uploads/logitech-g733.png', 1, 0, '2026-06-18T13:24:36.438', 10, 2);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (7, N'Corsair T3 Rush', N'Ghế gaming vải cao cấp, tay ghế 4D.', 8990000.00, 8, N'/images/uploads/corsair-t3-rush.webp', N'/images/uploads/corsair-t3-rush.webp', 1, 2, '2026-06-18T13:24:36.438', 13, 3);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (8, N'Razer Huntsman V3 Pro', N'Bàn phím gaming optical analog.', 5990000.00, 9, N'/images/uploads/razer-huntsman-v3-pro.webp', N'/images/uploads/razer-huntsman-v3-pro.webp', 1, 0, '2026-06-18T13:24:36.438', 8, 1);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (9, N'SteelSeries QcK Heavy XXL', N'Lót chuột gaming XXL dày 6mm, vải micro-woven cao cấp, đế cao su chống trượt tuyệt đối. Kích thước 900x300mm phủ kín cả bàn phím và chuột.', 890000.00, 40, N'/images/uploads/steelseries-qck-heavy-xxl.png', N'/images/uploads/steelseries-qck-heavy-xxl.png', 1, 1, '2026-06-21T14:24:52.856', 11, 5);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (10, N'Razer Gigantus V2 XXL', N'Lót chuột gaming cỡ XXL với bề mặt vải vi sợi tối ưu tốc độ và kiểm soát. Đế cao su dày giữ cố định trên bàn. Size XXL 940x410mm.', 790000.00, 35, N'/images/uploads/placeholder.svg', N'/images/uploads/placeholder.svg', 1, 1, '2026-06-21T14:24:52.860', 11, 1);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (11, N'HyperX QuadCast S', N'Micro thu âm USB condenser chuyên nghiệp với đèn RGB đổi màu. 4 chế độ pickup pattern, chống rung tích hợp, nút tắt tiếng nhạy. Lý tưởng cho streaming và podcast.', 3290000.00, 15, N'/images/uploads/hyperx-quadcast-s.jpg', N'/images/uploads/hyperx-quadcast-s.jpg', 1, 0, '2026-06-21T14:24:52.860', 17, 6);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (12, N'Razer Seiren V3 Chroma', N'Micro gaming USB cao cấp với hệ thống đèn Chroma RGB phản ứng theo âm thanh (Stream Reactive). Capsule supercardioid 25mm, loại bỏ tiếng ồn nền xuất sắc.', 3490000.00, 12, N'/images/uploads/razer-seiren-v3-chroma.webp', N'/images/uploads/razer-seiren-v3-chroma.webp', 1, 0, '2026-06-21T14:24:52.863', 17, 1);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (13, N'Logitech C922 Pro Stream', N'Webcam stream chuyên nghiệp Full HD 1080p 30fps hoặc 720p 60fps. Tích hợp 2 micro stereo, tự động chỉnh sáng và hỗ trợ thay nền ảo RightLight 2. Lý tưởng cho Twitch và YouTube.', 2290000.00, 20, N'/images/uploads/logitech-c922.png', N'/images/uploads/logitech-c922.png', 1, 0, '2026-06-21T14:24:52.866', 18, 2);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (14, N'Sony DualSense Wireless Controller', N'Tay cầm không dây PS5 với Adaptive Triggers và Haptic Feedback thế hệ mới. Cảm giác phản hồi chân thực khi chơi game. Pin 12h, sạc qua USB-C. Tương thích PC và PS5.', 1890000.00, 30, N'/images/uploads/placeholder.svg', N'/images/uploads/placeholder.svg', 1, 0, '2026-06-21T14:25:22.306', 12, 7);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (15, N'Logitech G923 TRUEFORCE Racing Wheel', N'Vô lăng đua xe với công nghệ TRUEFORCE cho phản hồi lực tính đến 1000Hz. Hỗ trợ PC và PS4/PS5. Pedal 2 bàn đạp, góc quay 900°. Tương thích Gran Turismo và nhiều tựa game đua xe.', 9990000.00, 5, N'/images/uploads/placeholder.svg', N'/images/uploads/placeholder.svg', 1, 1, '2026-06-21T14:25:22.313', 12, 2);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (16, N'SteelSeries Arctis Nova Pro Wireless', N'Tai nghe gaming không dây cao cấp với dual wireless (2.4GHz + Bluetooth), 2 pin có thể thay nổi tức thì. Loa 40mm Neodymium, ANI chủ động khử tiếng ồn, màn hình OLED tích hợp.', 8990000.00, 8, N'/images/uploads/placeholder.svg', N'/images/uploads/placeholder.svg', 1, 0, '2026-06-21T14:25:22.316', 10, 5);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (17, N'Corsair MM350 Champion Series XL', N'Lót chuột gaming cỡ XL với bề mặt vải dệt cao cấp được tối ưu cho cả chuột quang và laser. Đế cao su dày 3mm chống trượt. In logo Corsair. Kích thước 450x400x3mm.', 990000.00, 25, N'/images/uploads/placeholder.svg', N'/images/uploads/placeholder.svg', 1, 1, '2026-06-21T14:25:22.316', 11, 3);
INSERT INTO Products (Id, Name, Description, Price, Stock, ImageUrl, SecondaryImageUrls, IsActive, ShippingClass, CreatedAt, CategoryId, BrandId) VALUES (18, N'Secretlab Titan Evo 2022', N'Ghế gaming cao cấp với hỗ trợ lưng 4 chiều (có thể điều chỉnh ra vào, lên xuống), gối đầu từ tính, da NEO Hybrid Leatherette siêu bền. Khung thép, nệm bọt thoáng khí. Tải trọng tối đa 130kg.', 14990000.00, 5, N'/images/uploads/placeholder.svg', N'/images/uploads/placeholder.svg', 1, 2, '2026-06-21T14:25:22.320', 13, 8);

SET IDENTITY_INSERT Products OFF;


-- ============================================================

-- ============================================================
-- PRODUCT SPECIFICATIONS
-- ============================================================
IF OBJECT_ID('ProductSpecifications', 'U') IS NULL
BEGIN
    CREATE TABLE ProductSpecifications (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        [Key] NVARCHAR(100) NOT NULL,
        [Value] NVARCHAR(300) NOT NULL,
        FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
    );
END

SET IDENTITY_INSERT ProductSpecifications ON;
DELETE FROM ProductSpecifications;
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (1, 1, N'DPI', N'30000');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (2, 1, N'Connection', N'Wireless');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (3, 1, N'Weight', N'63g');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (4, 2, N'DPI', N'32000');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (5, 2, N'Connection', N'Wireless');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (6, 2, N'Weight', N'60g');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (7, 3, N'Switch Type', N'Cherry MX Red');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (8, 3, N'Connection', N'Wired');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (9, 3, N'Layout', N'Full-size');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (10, 4, N'Switch Type', N'Gateron Pro Brown');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (11, 4, N'Connection', N'Wireless');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (12, 4, N'Layout', N'75%');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (13, 5, N'Connection', N'Wireless');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (14, 5, N'Driver', N'50mm');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (15, 5, N'Mic', N'Detachable');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (16, 6, N'Connection', N'Wireless');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (17, 6, N'Driver', N'40mm');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (18, 6, N'Weight', N'278g');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (19, 7, N'Material', N'Fabric');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (20, 7, N'Max Load', N'120kg');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (21, 7, N'Recline', N'160°');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (22, 8, N'Switch Type', N'Analog Optical');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (23, 8, N'Connection', N'Wired');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (24, 8, N'Layout', N'Full-size');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (25, 9, N'Kích thước', N'900x300x6mm');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (26, 9, N'Chất liệu', N'Micro-woven cloth');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (27, 9, N'Đế', N'Cao su chống trượt');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (28, 10, N'Kích thước', N'940x410x4mm');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (29, 10, N'Chất liệu', N'Vi sợi mịn');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (30, 10, N'Đế', N'Cao su dày anti-slip');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (31, 11, N'Loại', N'USB Condenser');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (32, 11, N'Polar Pattern', N'4 chế độ');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (33, 11, N'Tần số đáp', N'20Hz-20kHz');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (34, 11, N'RGB', N'Có');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (35, 12, N'Loại', N'USB Condenser');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (36, 12, N'Polar Pattern', N'Supercardioid');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (37, 12, N'Chroma RGB', N'Stream Reactive');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (38, 12, N'Kết nối', N'USB-C');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (39, 13, N'Độ phân giải', N'1080p/30fps hoặc 720p/60fps');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (40, 13, N'Micro', N'2 micro stereo tích hợp');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (41, 13, N'Góc nhìn', N'78°');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (42, 13, N'Kết nối', N'USB-A');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (43, 14, N'Kết nối', N'Wireless Bluetooth / USB-C');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (44, 14, N'Adaptive Triggers', N'Có');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (45, 14, N'Haptic Feedback', N'Có');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (46, 14, N'Pin', N'Khoảng 12 giờ');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (47, 15, N'Góc quay', N'900°');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (48, 15, N'Kết nối', N'USB');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (49, 15, N'Tương thích', N'PC / PS4 / PS5');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (50, 15, N'Pedal', N'2 bàn đạp');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (51, 16, N'Kết nối', N'2.4GHz + Bluetooth 5.0');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (52, 16, N'Driver', N'40mm Neodymium');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (53, 16, N'Pin', N'Không giới hạn (2 pin thay)');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (54, 16, N'ANC', N'Có');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (55, 17, N'Kích thước', N'450x400x3mm');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (56, 17, N'Chất liệu', N'Micro-weave cloth');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (57, 17, N'Đế', N'Cao su 3mm');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (58, 17, N'Tối ưu cho', N'Quang + Laser');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (59, 18, N'Chất liệu', N'NEO Hybrid Leatherette');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (60, 18, N'Lumbar Support', N'4 chiều tích hợp');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (61, 18, N'Tải trọng', N'130kg');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (62, 18, N'Recline', N'165°');
INSERT INTO ProductSpecifications (Id, ProductId, [Key], [Value]) VALUES (63, 18, N'Armrest', N'4D');

SET IDENTITY_INSERT ProductSpecifications OFF;


-- ============================================================
-- DISCOUNTS / VOUCHERS
-- ============================================================

SET IDENTITY_INSERT Discounts ON;
DELETE FROM Discounts;

SET IDENTITY_INSERT Discounts OFF;


-- ============================================================
-- HOÀN THÀNH
-- ============================================================
-- Tổng kết:
--   Brands: 8 brands (Razer, Logitech, Corsair, Keychron, SteelSeries, HyperX, Sony, Secretlab)
--   Categories: 32 danh mục
--   Products: 18 sản phẩm với hình ảnh thực
--   Product Specs: 63 thông số kỹ thuật
--   Discounts: 2 voucher (NEWUSER200K, NEWUSERFREE)
--
-- Hình ảnh sản phẩm: wwwroot/images/uploads/ (12 ảnh thực + placeholder.svg)
-- Users/Roles/Orders: chạy ứng dụng để tự động seed qua DbSeeder.cs
-- ============================================================
PRINT 'NexusGear database data import completed successfully!';
GO
