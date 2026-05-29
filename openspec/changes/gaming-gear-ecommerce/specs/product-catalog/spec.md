## ADDED Requirements

### Requirement: Browse Products
The system SHALL display a list of all gaming gear products with their name, image, brand, price, and category.

#### Scenario: Browsing catalog
- **WHEN** user navigates to the shop catalog page
- **THEN** the system displays a grid of gaming gear products with pagination

### Requirement: Filter Products
The system SHALL allow users to filter products dynamically by Brand, Category, Price Range, and Technical Specifications (Switch Type, DPI, Connection).

#### Scenario: Dynamic product filtering
- **WHEN** user selects the brand "Razer" and connection type "Wireless"
- **THEN** the system displays only wireless products manufactured by Razer

### Requirement: Search Products
The system SHALL support keyword search across product names, categories, brands, and descriptions.

#### Scenario: Successful search
- **WHEN** user enters "DeathAdder" in the search bar and submits
- **THEN** the system displays matching products with the term "DeathAdder" in their title or description

### Requirement: View Product Details
The system SHALL show a dedicated product details page with complete specifications, description, high-resolution image, stock status, and add-to-cart controls.

#### Scenario: Product details viewing
- **WHEN** user clicks on a specific product card in the catalog
- **THEN** the system displays the complete product details page with detailed specifications
