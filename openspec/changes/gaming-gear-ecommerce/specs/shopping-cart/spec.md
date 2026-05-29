## ADDED Requirements

### Requirement: Add Items to Cart
The system SHALL support adding any in-stock product to the shopping cart from either the catalog page or product details page.

#### Scenario: Successful add to cart
- **WHEN** user clicks "Add to Cart" on a product details page
- **THEN** the system adds the item to the cart and updates the cart item count header badge

### Requirement: Modify Cart Quantities
The system SHALL allow users to increment, decrement, or manually edit the quantity of items in their cart, updating prices in real-time.

#### Scenario: Update item quantity
- **WHEN** user changes the quantity of an item to 3 on the cart page
- **THEN** the system calculates and displays the new line subtotal and the overall cart total instantly

### Requirement: Remove Items from Cart
The system SHALL allow users to remove items completely from their shopping cart.

#### Scenario: Delete cart item
- **WHEN** user clicks the "Remove" icon next to a product in the cart
- **THEN** the system removes that product and updates the total price

### Requirement: Persistent Cart Sessions
The system SHALL persist the shopping cart items across browser sessions for anonymous users, and merge them with a user's account when they log in.

#### Scenario: Merging cart on login
- **WHEN** anonymous user adds a product to the cart and subsequently logs into their account
- **THEN** the system merges the anonymous cart items into the registered user's cart database record
