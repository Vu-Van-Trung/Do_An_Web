## ADDED Requirements

### Requirement: Product Inventory Management (CRUD)
The system SHALL provide full CRUD (Create, Read, Update, Delete) capability for products to administrator accounts.

#### Scenario: Creating a new gaming mouse product
- **WHEN** Admin submits the product creation form with name "Logitech G Pro X Superlight", price 3000000, and stock 15
- **THEN** the system adds the product to the inventory database and lists it under the catalog management panel

### Requirement: Order Status Fulfillment
The system SHALL allow administrators to view all customer orders and update their fulfillment status (Pending, Shipping, Completed, Cancelled).

#### Scenario: Updating order to shipped
- **WHEN** Admin changes an order's status from "Pending" to "Shipping" and saves
- **THEN** the system persists the status change and reflects it in both admin and customer views

### Requirement: Store Metrics Dashboard
The system SHALL aggregate and present store analytics including total revenue, order count, registered customer count, and low stock warnings to admins.

#### Scenario: Loading admin dashboard home
- **WHEN** Admin logs into the admin panel home page
- **THEN** the system displays modern graphical cards summarizing revenue, orders, and products low in stock
