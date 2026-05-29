## 1. Project Initialization & Setup

- [x] 1.1 Create a new ASP.NET Core MVC project (.NET 8)
- [x] 1.2 Install necessary NuGet packages (Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Tools, Microsoft.AspNetCore.Identity.EntityFrameworkCore)
- [x] 1.3 Setup base layout (`_Layout.cshtml`) and custom CSS structure (Vibrant Dark Theme, Glassmorphism, Google Fonts)
- [x] 1.4 Configure static files and common frontend assets

## 2. Database Models & DbContext

- [x] 2.1 Define core Domain Models (`Category`, `Brand`, `Product`, `ProductSpecification`)
- [x] 2.2 Define Cart, Order and User Models (`CartItem`, `Order`, `OrderItem`, `ApplicationUser` extending IdentityUser)
- [x] 2.3 Setup `ApplicationDbContext` and configure entity relationships
- [x] 2.4 Configure database connection string in `appsettings.json`
- [x] 2.5 Run initial EF Core Migration and update database
- [x] 2.6 Implement Database Seeder (populate initial categories, brands, admin user, and sample gaming products)

## 3. Architecture & Services

- [x] 3.1 Create Repository interfaces (`IRepository`, `IProductRepository`, etc.)
- [x] 3.2 Implement Repository classes using `ApplicationDbContext`
- [x] 3.3 Register repositories and services in `Program.cs` for Dependency Injection

## 4. Authentication & Identity

- [x] 4.1 Configure ASP.NET Core Identity in `Program.cs`
- [x] 4.2 Create `AccountController` for Registration and Login logic
- [x] 4.3 Build Login and Register Razor Views
- [x] 4.4 Set up Roles (Admin, Customer) and seed default roles

## 5. Product Catalog (`product-catalog`)

- [x] 5.1 Create `ProductController` for frontend browsing
- [x] 5.2 Implement Catalog View with pagination, search, and dynamic filtering by brand/category/specs
- [x] 5.3 Implement Product Details View showing complete product information and images

## 6. Shopping Cart (`shopping-cart`)

- [x] 6.1 Implement Cart Service to handle Session-based cart (anonymous users) and DB-based cart (logged-in users)
- [x] 6.2 Add logic to merge session cart into user account upon login
- [x] 6.3 Create Cart Controller API endpoints (Add, Remove, Update Quantity)
- [x] 6.4 Build Shopping Cart Razor View to display items and calculate totals dynamically

## 7. Checkout & Orders (`order-checkout`)

- [x] 7.1 Create `CheckoutController` with a multi-step checkout flow
- [x] 7.2 Build Shipping Details form view
- [x] 7.3 Implement Order processing logic (save to DB, clear cart, mock payment)
- [x] 7.4 Build Order Confirmation (Receipt) View
- [x] 7.5 Build Order History View for customer profile

## 8. Admin Dashboard (`admin-dashboard`)

- [x] 8.1 Setup `Admin` Area in ASP.NET Core MVC
- [x] 8.2 Secure `Admin` Area with `[Authorize(Roles = "Admin")]`
- [x] 8.3 Implement Admin Dashboard View (Home) showing key metrics (total revenue, order count)
- [x] 8.4 Implement Product Management (CRUD operations with image upload support)
- [x] 8.5 Implement Category and Brand Management (CRUD)
- [x] 8.6 Implement Order Management (View orders, update status to Shipped/Completed)
