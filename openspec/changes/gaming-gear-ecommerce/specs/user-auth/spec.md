## ADDED Requirements

### Requirement: User Registration
The system SHALL allow guest users to create a customer account using a unique email, full name, phone number, and a strong password.

#### Scenario: Registering a new customer account
- **WHEN** guest fills in valid registration details and submits the form
- **THEN** the system creates their record, logs them in, and redirects to their dashboard

### Requirement: Customer Authentication
The system SHALL secure member-only areas by requiring users to authenticate using their registered email and password.

#### Scenario: Successful customer login
- **WHEN** user enters correct login credentials
- **THEN** the system logs the user in and redirects them to their previous page or dashboard

### Requirement: Role-Based Authorization
The system SHALL enforce Role-Based Access Control (RBAC), distinguishing between "Customer" and "Admin" roles.

#### Scenario: Customer attempts admin access
- **WHEN** authenticated Customer attempts to access the URL "/Admin"
- **THEN** the system blocks the request and redirects them to an Access Denied error page
