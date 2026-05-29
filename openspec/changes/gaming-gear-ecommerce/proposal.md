## Why

Building a modern Gaming Gear E-Commerce platform meets the growing demand for high-performance peripherals (keyboards, mice, headsets, chairs) among esports enthusiasts and casual gamers. Current solutions often suffer from poor visual aesthetics, slow filtering, and clunky user interfaces. This project establishes a visually stunning, fully functional e-commerce platform using ASP.NET Core and Entity Framework Core, optimized for accessibility, high performance, and premium aesthetics.

## What Changes

- **Product Catalog & Details**: High-fidelity UI to display gaming gear with powerful search and filters (Category, Brand, Switch Type, DPI, Price Range, and Connection).
- **Interactive Shopping Cart**: Dynamic cart with instant updates, persistence, and coupon support.
- **User Authentication**: Secure member login, registration, and user profiles using ASP.NET Core Identity.
- **Order Checkout**: Multi-step, secure checkout workflow with order tracking and history.
- **Admin Dashboard**: Comprehensive back-office to manage products, categories, brands, orders, and view sales metrics.
- **Database Architecture**: Structured SQL Database with Entity Framework Core, migrations, and seeding with realistic gaming gear products (brands like Razer, Logitech, Corsair, Keychron).

## Capabilities

### New Capabilities
- `product-catalog`: Product browsing, detailed product specs, advanced filtering, and search engine.
- `shopping-cart`: Session/database-backed shopping cart management with total calculation.
- `user-auth`: Account creation, authentication, role-based authorization (User/Admin), and profile dashboard.
- `order-checkout`: Seamless checkout funnel with shipping details, fake payment integration, and receipt generation.
- `admin-dashboard`: Admin control panel for CRUD operations on inventory, order status updates, and basic sales charts.

### Modified Capabilities

None.

## Impact

- **Database**: Creates a SQL Server database mapping tables for Users, Roles, Products, Categories, Brands, Orders, and OrderItems.
- **Architecture**: Implements the clean MVC (Model-View-Controller) architecture in ASP.NET Core with Repository pattern for EF Core.
- **Frontend**: Integrates premium modern CSS styling (glassmorphism, vibrant dark-mode gaming themes, Outfit/Inter typography, smooth CSS micro-animations).
- **Dependencies**: Introduces Entity Framework Core, SQL Server Provider, ASP.NET Core Identity, and EF Core Tools.
