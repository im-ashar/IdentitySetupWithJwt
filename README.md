# Identity Setup with JWT in .NET 8

This repository contains a template for setting up JWT token authentication with ASP.NET Core Identity using Entity Framework Core in a .NET 8 Web API project. The template is designed to be imported into Visual Studio 2022 for quick and easy project initialization.

## Features

- **JWT Authentication with Identity:** 
  Integrated JWT token authentication using ASP.NET Core Identity and Entity Framework Core.

- **Options and Result Pattern:**
  Implementation of the Options and Result pattern for cleaner, more maintainable code.

- **Global Exception Handling:**
  A Global Exception Handler is implemented to catch and handle exceptions throughout the API.

## Getting Started

### Prerequisites

- **.NET 8 SDK:** Ensure you have the .NET 8 SDK installed on your machine.
- **Visual Studio 2022:** The template is designed to be imported into Visual Studio 2022.

### Installation

1. **Clone the Repository:**
   ```
   git clone https://github.com/im-ashar/IdentitySetupWithJwt.git
   ```

2. **Import the Template:**
   - Download the `IdentitySetupWithJwt.zip` file from the repository.
   - Open Visual Studio 2022.
   - Go to **Project** > **Import Template**.
   - Select the downloaded `IdentitySetupWithJwt.zip` file and follow the prompts to import the template.

3. **Create a New Project:**
   - In Visual Studio 2022, create a new project using the imported template.
   - The project will be initialized with JWT authentication, Identity setup, and all other features.

### Project Structure

- **Controllers:** Contains the Web API controllers.
- **Services:** Business logic services.
- **Data:** Entity Framework Core setup and migrations.
- **Options:** Configuration options using the Options pattern.
- **GlobalExceptionHandler:** Middleware for handling exceptions globally.

### Usage

- **JWT Authentication:** 
  The project is pre-configured with JWT authentication using ASP.NET Core Identity. Customize as needed for your application's requirements.

- **Entity Framework Core:** 
  The template includes EF Core setup for Identity. Migrations and database context are already configured.

- **Global Exception Handling:** 
  Exceptions are caught and handled by the Global Exception Handler middleware.

### Contributing

If you want to contribute to this project, feel free to fork the repository and submit a pull request.

### License

This project is licensed under the MIT License. See the `LICENSE` file for details.
