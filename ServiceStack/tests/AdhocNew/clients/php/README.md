# ServiceStack PHP Console App

A simple console application demonstrating how to use the ServiceStack PHP client.

## Setup

### 1. Install Dependencies

```bash
composer install
```

### 2. Run the Application

```bash
php app.php
```

Or with a custom name parameter:

```bash
php app.php "Alice"
```

## Project Structure

```
.
├── composer.json          # Composer configuration with dependencies
├── app.php               # Main console application
└── vendor/               # Dependencies (created after composer install)
```

## What This Does

The application:
1. Creates a ServiceStack JSON client
2. Sends a request to a test ServiceStack API endpoint
3. Receives and displays the response
4. Handles errors gracefully

## Key Components

- **JsonServiceClient**: The ServiceStack client for making API calls
- **DTOs**: Data Transfer Objects (Hello, HelloResponse) define the API contract
- **Request/Response**: The app sends a Hello request and receives a HelloResponse

## Requirements

- PHP 7.4 or higher
- Composer

## Next Steps

To use with your own ServiceStack API:
1. Change the base URL in `new JsonServiceClient("your-api-url")`
2. Define your own DTOs matching your API endpoints
3. Call the appropriate methods (get, post, put, delete) on the client