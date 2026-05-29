## Context

The goal is to develop a premium, fully featured Gaming Gear E-Commerce platform using ASP.NET Core MVC, Entity Framework Core, and SQL Server. Gamers expect a high-fidelity, ultra-responsive website with a dark, immersive theme (neon accents, glassmorphism, smooth animations) that performs exceptionally fast. The system must support catalog browsing, dynamic specification-based filtering (e.g., DPI, connection type, switch type), shopping cart management, user authorization, and an administrative dashboard to manage inventory and view metrics.

## Goals / Non-Goals

**Goals:**
- Implement a clean Model-View-Controller (MVC) architecture with Repository Pattern.
- Use Entity Framework Core Code-First approach to manage the SQL Server schema.
- Implement secure, role-based user management (Admin and Customer) using ASP.NET Core Identity.
- Build a premium, high-performance user interface using custom Vanilla CSS (Vibrant Gaming Dark Mode, glassmorphism effects, dynamic CSS micro-animations, custom Google Fonts "Outfit" & "Inter").
- Support advanced search and specs-based filtering (Category, Brand, Price Range, and specs like Connection, Switch Type, DPI) to help users find gaming gear quickly.
- Persist shopping carts using a hybrid approach (browser session/cookie for guest users, SQL database records for registered users).
- Provide a robust back-office admin panel for complete inventory management (CRUD), order status updating, and store statistics display.
- Seed database with realistic gaming products from major brands (Logitech, Razer, Corsair, Keychron).

**Non-Goals:**
- Integrating actual production-grade payment gateways (e.g., Stripe, PayPal). The system will use Cash on Delivery (COD) and a mock credit card simulator.
- Real-time shipping carrier API integrations. A flat shipping fee or basic regional dropdown selection will be used instead.
- Automated email sending servers. Receipt confirmations will be displayed directly on screen.

## Decisions

### Decision 1: Architecture & Pattern
- **Choice**: ASP.NET Core 8.0 MVC with a generic/specific Repository Pattern.
- **Alternative considered**: Razor Pages.
- **Rationale**: MVC offers a cleaner separation of concerns for larger projects with distinct customer and administrative views (Areas). It facilitates clean API-like controllers for dynamic AJX requests (e.g., adding to cart without page reloads). The Repository pattern decouples controllers from direct EF Core DbContext calls, improving testability and code reuse.

### Decision 2: Database Technology & ORM
- **Choice**: Microsoft SQL Server (LocalDB for development) with Entity Framework Core Code-First.
- **Alternative considered**: PostgreSQL with Dapper.
- **Rationale**: EF Core offers seamless integration with ASP.NET Core Identity and excellent developer productivity through migrations. Code-first migrations ensure database schema is tracked in version control. MS SQL Server is the native and most optimized database engine for the .NET stack.

### Decision 3: Custom Premium Gaming Design System
- **Choice**: Custom Vanilla CSS with a Gaming Dark Theme and Glassmorphism.
- **Alternative considered**: Bootstrap or Tailwind CSS.
- **Rationale**: Standard frameworks like Bootstrap look generic and clinical. Gamers expect a custom, immersive, high-tech experience. Designing a custom CSS system using modern CSS Custom Properties (variables) allows fine control over neon glow shadows, glass backdrop filters, HSL custom colors (e.g., deep charcoal `#0a0d14`, vivid cyber-green `#00ff88`, electric violet `#8b5cf6`), and highly responsive flex/grid layouts without bloating the page size.

### Decision 4: Shopping Cart State Persistence
- **Choice**: Hybrid database-backed cart for logged-in users + Cookie/Session-backed cart for anonymous users.
- **Alternative considered**: Exclusively Session-based cart.
- **Rationale**: Database-backed cart persistence ensures registered users do not lose their selections across different devices. Merging anonymous session carts into the database record upon login provides a fluid checkout experience.

### Decision 5: Flexible Product Specifications Schema
- **Choice**: A dedicated `ProductSpecification` table linked to `Product` via one-to-many relationship.
- **Alternative considered**: Storing specs in JSON fields or simple text columns.
- **Rationale**: Storing specs in a separate structured table allows EF Core to execute fast index-driven JOIN queries for filtering (e.g., "Find all Keyboards where SwitchType = Cherry MX Blue"). Storing as raw text or JSON makes query-level filtering slow and fragile in SQL Server.

## Risks / Trade-offs

- **[Risk] Heavy graphics slowing down load speeds**  
  *Mitigation*: Use CSS gradients and modern web-safe images. Implement lazy loading for images and compress seeded assets.
- **[Risk] SQL Connection complexity on user machine**  
  *Mitigation*: Pre-configure connection string to use standard SQL Server LocalDB (`(localdb)\mssqllocaldb`) or a standard developer database name so it runs immediately on standard Windows environments.
- **[Risk] Identity user account lockout**  
  *Mitigation*: Configure relaxed password validation rules for testing (e.g., no strict non-alphanumeric requirement) to make evaluation simple and friendly for the user.
