## ADDED Requirements

### Requirement: Shipping Details Collection
The system SHALL collect shipping information including full name, phone number, shipping address, and order notes during checkout.

#### Scenario: Submitting shipping information
- **WHEN** user submits valid shipping details during the checkout process
- **THEN** the system saves these details in the current checkout session and proceeds to payment choice

### Requirement: Place Order and Clear Cart
The system SHALL record a complete order with invoice details, product snapshots (price, quantity), clear the user's active shopping cart, and present a receipt page.

#### Scenario: Completing order placement
- **WHEN** user clicks "Place Order" under the selected Cash on Delivery or mock payment method
- **THEN** the system writes the order to the database, empties the shopping cart, and displays the order success page

### Requirement: View Order History
The system SHALL allow authenticated customers to view a historical list of all their completed and pending orders.

#### Scenario: Accessing order history
- **WHEN** customer navigates to their order history section in the account profile
- **THEN** the system lists their orders chronologically showing order ID, date, status, and total amount
