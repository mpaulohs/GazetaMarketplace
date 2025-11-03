# Application Features Overview

Welcome to the NET10 Project Example! This comprehensive guide showcases all the powerful features available in both the MVC web application and the Minimal API, designed to help you understand what you can do with this modern .NET 10 platform.

## Table of Contents

1. [Introduction](#introduction)
2. [MVC Web Application Features](#mvc-web-application-features)
3. [Minimal API Features](#minimal-api-features)
4. [Testing and Reliability](#testing-and-reliability)
5. [Development Features](#development-features)
6. [Feature Comparison](#feature-comparison)
7. [Common Use Cases](#common-use-cases)

## Introduction

The NET10 Project Example demonstrates a complete, production-ready .NET 10 application stack with two complementary interfaces:

- **MVC Web Application** - A traditional web-based user interface with rich interactive pages
- **Minimal API** - A lightweight REST API for programmatic access and integration

Both applications showcase modern .NET 10 capabilities, including centralized package management, hot reload support, and comprehensive testing frameworks. Whether you're building web applications, APIs, or both, this project provides a solid foundation and clear examples.

![Project Architecture Diagram](../images/screenshots/architecture-overview.png)

## MVC Web Application Features

The MVC web application provides an intuitive, user-friendly interface for interacting with the system. Located at `src/Example.Web`, it demonstrates ASP.NET Core MVC best practices with Razor views and a responsive design.

### 1. Home Page Navigation

**What it does:** Provides a welcoming landing page with navigation to other features.

- Clean, professional homepage introducing the application
- Quick navigation menu for easy access to all features
- Responsive design that works on desktop, tablet, and mobile devices
- Modern styling with Bootstrap framework integration

![Home Page Screenshot](../images/screenshots/web-home-page.png)

**Try it:** Start the MVC application and visit the home page in your browser. See the [MVC Web Application Tutorial](../tutorials/mvc-web-app.md) for detailed instructions.

### 2. Privacy Page

**What it does:** Displays important information about how your data is handled.

- Dedicated privacy information page
- Easy-to-read format with clear sections
- Demonstrates content management and page routing
- Template-based approach for easy updates

**Try it:** Click the "Privacy" link in the footer navigation to view privacy details.

### 3. Weather Forecast View

**What it does:** Shows a live weather forecast with dynamically generated data.

- Real-time weather forecast display
- 5-day forecast with temperature and conditions
- Temperature displayed in both Celsius and Fahrenheit
- Auto-refresh capability for live updates
- Visual indicators for weather conditions

![Weather Forecast Screenshot](../images/screenshots/web-weather-forecast.png)

**Benefits:**
- Demonstrates data binding and dynamic content rendering
- Shows how to display real-time information
- Example of API integration within MVC views

**Try it:** The weather forecast is automatically displayed on the home page. Refresh the page to see different forecast data.

### 4. Error Handling

**What it does:** Gracefully handles and displays application errors to users.

- User-friendly error pages instead of technical stack traces
- Request tracking for debugging and support
- Detailed error information in development mode
- Production-safe error reporting in live environments

**Benefits:**
- Improves user experience when issues occur
- Helps with troubleshooting and error tracking
- Demonstrates ASP.NET Core error handling patterns

### 5. Responsive Design & Accessibility

**What it does:** Ensures the application works across all devices and follows accessibility standards.

- Mobile-first responsive design
- Touch-friendly interface for smartphones and tablets
- Keyboard navigation support
- Standards-compliant HTML and CSS

## Minimal API Features

The Minimal API application provides a lightweight, high-performance REST interface for programmatic access. Located at `src/Example.API`, it demonstrates modern API design patterns in .NET 10.

### 1. RESTful Endpoints

**What it does:** Exposes well-designed API endpoints for data access and operations.

- Clean, resource-oriented endpoint design
- HTTP verb semantics (GET, POST, PUT, DELETE)
- Proper HTTP status codes and responses
- Stateless, scalable architecture

**Available Endpoints:**

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/weatherforecast` | GET | Retrieve 5-day weather forecast |

**Example:**
```bash
curl https://localhost:7001/weatherforecast
```

### 2. Swagger/OpenAPI Documentation

**What it does:** Provides interactive API documentation that's always up-to-date.

- Auto-generated API documentation from code
- Interactive endpoint testing interface
- Request/response schema visualization
- Parameter and return type documentation

![Swagger UI Screenshot](../images/screenshots/api-swagger-ui.png)

**Try it:**
1. Start the API application
2. Navigate to `https://localhost:7001/openapi/v1.json` to view the OpenAPI specification
3. Use the interactive Swagger interface to explore and test endpoints

**Benefits:**
- Reduces documentation effort
- Developers can explore APIs without external tools
- Specification stays in sync with actual implementation

### 3. Weather Forecast API

**What it does:** Returns dynamically generated weather forecast data in JSON format.

**Endpoint:** `GET /weatherforecast`

**Response Example:**
```json
[
  {
    "date": "2025-11-04",
    "temperatureC": 25,
    "temperatureF": 77,
    "summary": "Mild"
  },
  {
    "date": "2025-11-05",
    "temperatureC": 18,
    "temperatureF": 64,
    "summary": "Cool"
  }
]
```

**Features:**
- Returns 5-day forecast
- Includes Celsius and Fahrenheit temperatures
- Provides human-readable weather summaries
- JSON format for easy parsing and integration

### 4. Interactive API Testing

**What it does:** Allows you to test API endpoints directly in your browser.

- Try-it-out functionality in Swagger UI
- No additional tools required (curl, Postman, etc.)
- Real-time request and response viewing
- Error feedback with detailed messages

**Try it:** In the Swagger UI, click the "Try it out" button on the `/weatherforecast` endpoint, then click "Execute" to see the live response.

### 5. HTTPS Security

**What it does:** Ensures all API communications are encrypted and secure.

- HTTPS enforced for all endpoints
- Secure communication channel
- Production-ready security configuration
- Browser trust indicators

## Testing and Reliability

The application includes comprehensive testing infrastructure to ensure reliability and catch bugs early.

### 1. Unit Tests

**What it does:** Tests individual components in isolation to verify correct behavior.

- MSTest framework with Microsoft.Testing.Platform
- Test coverage for controllers and services
- Parallel test execution for fast feedback
- Clear test names and assertions

**Coverage Areas:**
- Controller actions and responses
- Business logic validation
- Error handling scenarios
- Data transformation and formatting

**Try it:** Run all tests with:
```bash
dotnet test
```

See the [Testing Guide](../tutorials/testing-guide.md) for more details.

### 2. End-to-End Tests with Playwright

**What it does:** Tests the complete user workflow from browser automation perspective.

- Real browser automation testing
- User journey validation
- Visual regression detection
- Cross-browser testing capability

**Tests Include:**
- Web application navigation workflows
- Form submission and validation
- API integration scenarios
- Error condition handling

**Benefits:**
- Validates real user experience
- Catches integration issues missed by unit tests
- Regression prevention for future changes

## Development Features

Beyond user-facing capabilities, the application includes developer-focused features that improve the development experience.

### 1. Hot Reload Support

**What it does:** Automatically refreshes the application when you make code changes.

- Instant feedback during development
- No need to stop and restart the application
- Saves time and improves productivity
- Works with C# code and Razor views

**Try it:**
1. Start the application: `dotnet watch run`
2. Edit a view or controller
3. See changes instantly in your browser

**Benefits:**
- Faster development iteration
- Immediate visual feedback
- Reduced context switching

### 2. Centralized Package Management

**What it does:** Manages all NuGet package versions in one location.

- Single source of truth for dependencies
- Easier to update packages globally
- Prevents version mismatches across projects
- Simplified dependency management

**Location:** `Directory.Packages.props`

**Benefits:**
- Reduces maintenance burden
- Ensures consistency across projects
- Simplifies dependency auditing and updates

### 3. Modern .NET 10 Patterns

**What it does:** Demonstrates best practices and modern language features.

- Minimal APIs (lightweight, high-performance)
- Records and nullable reference types
- Pattern matching and expression-bodied members
- Async/await throughout
- Dependency injection built-in

**Examples in Codebase:**
- Minimal API endpoints with clear semantics
- Weather forecast record type for type-safe data
- Structured logging with dependency injection
- Modern exception handling patterns

**Benefits:**
- Learn industry best practices
- Leverage latest C# language features
- Build more maintainable applications
- Better performance characteristics

### 4. Docker Support

**What it does:** Containerizes both applications for easy deployment.

- Dockerfile included for both applications
- Run applications consistently anywhere
- Container orchestration ready
- Production-grade containerization

## Feature Comparison

### MVC Web Application vs. Minimal API

| Feature | MVC Web App | Minimal API |
|---------|:-----------:|:----------:|
| **User Interface** | Browser-based | Programmatic |
| **Home Page** | ✅ | ❌ |
| **Privacy Page** | ✅ | ❌ |
| **Weather Forecast View** | ✅ | ✅ |
| **Weather Forecast API** | ❌ | ✅ |
| **Interactive Testing** | ✅ (browser) | ✅ (Swagger) |
| **REST API** | ❌ | ✅ |
| **OpenAPI/Swagger** | ❌ | ✅ |
| **HTTPS** | ✅ | ✅ |
| **Responsive Design** | ✅ | N/A |
| **Page Navigation** | ✅ | N/A |
| **Error Handling** | ✅ | ✅ |
| **Unit Tests** | ✅ | ✅ |
| **E2E Tests** | ✅ | ✅ |

**Choose MVC Web App if you need:**
- Interactive user interface
- Human-friendly presentation
- Traditional web application model
- Rich visual interaction

**Choose Minimal API if you need:**
- Programmatic access
- Integration with other systems
- High-performance endpoints
- Machine-readable responses

**Best Practice:** Use both! Many applications use the API as the backend and the MVC web app as the frontend user interface.

## Common Use Cases

### Use Case 1: Viewing Weather Information

**Scenario:** You want to see the weather forecast for your day.

**Steps:**
1. Start the MVC web application
2. Navigate to the home page
3. View the weather forecast section
4. Weather is displayed in both Celsius and Fahrenheit
5. Refresh to get an updated forecast

**Related Feature:** Weather Forecast View

---

### Use Case 2: Building an Integration

**Scenario:** You want to integrate weather data into your own application.

**Steps:**
1. Start the Minimal API application
2. Make a GET request to `/weatherforecast`
3. Parse the JSON response in your application
4. Use the temperature and summary data as needed

**Example Code:**
```csharp
using HttpClient client = new();
var response = await client.GetAsync("https://localhost:7001/weatherforecast");
var json = await response.Content.ReadAsStringAsync();
// Parse and use the weather data
```

**Related Features:** RESTful Endpoints, Swagger/OpenAPI Documentation

---

### Use Case 3: Learning ASP.NET Core

**Scenario:** You want to learn modern ASP.NET Core development patterns.

**Steps:**
1. Examine the MVC application in `src/Example.Web`
2. Review the Minimal API implementation in `src/Example.API`
3. Check the test projects to understand testing patterns
4. Try the hot reload feature during development
5. Review error handling and security implementations

**Benefits:**
- Real-world code examples
- Best practices demonstrated
- Modern C# features showcased
- Testing infrastructure included

**Related Features:** Modern .NET 10 Patterns, Hot Reload Support, Testing

---

### Use Case 4: Deploying to Production

**Scenario:** You want to run the application in a production environment.

**Steps:**
1. Use the included Dockerfile for containerization
2. Build and publish the application
3. Deploy to your hosting environment
4. Configure HTTPS certificates
5. Monitor application health

**Related Features:** Docker Support, HTTPS Security, Error Handling

---

### Use Case 5: Testing Your Changes

**Scenario:** You modified the application and want to verify everything works.

**Steps:**
1. Run unit tests: `dotnet test`
2. Run Playwright E2E tests: `dotnet run --project tests/Example.Web.Tests.Playwright/Example.Web.Tests.Playwright.csproj`
3. Verify no code formatting issues: `dotnet format --verify-no-changes`
4. Review test results for failures
5. Fix any issues before committing

**Related Features:** Unit Tests, End-to-End Tests with Playwright

## Next Steps

Now that you understand the features, try these next steps:

1. **[Getting Started Guide](./getting-started.md)** - Install and run the applications
2. **[MVC Web Application Tutorial](../tutorials/mvc-web-app.md)** - Deep dive into the web app
3. **[API Tutorial](../tutorials/api-tutorial.md)** - Learn about the REST API
4. **[Testing Guide](../tutorials/testing-guide.md)** - Understand the testing infrastructure
5. **[Architecture Overview](./architecture.md)** - Learn how components work together

## Troubleshooting

**Q: The weather forecast shows different data each time I refresh. Is that normal?**

A: Yes! The application generates random weather data to simulate a real forecast service. This is intentional for demonstration purposes.

---

**Q: Can I use both the MVC web app and API at the same time?**

A: Absolutely! You can run both applications simultaneously on different ports. This allows you to test the API integration while using the web interface.

---

**Q: Where can I find more information about specific features?**

A: Check the [Tutorials](../tutorials/) section for detailed guides on each feature, or visit the [Architecture Overview](./architecture.md) to understand how components interact.

---

## Summary

The NET10 Project Example provides a comprehensive demonstration of modern .NET 10 development with both web and API interfaces. Whether you're building user-facing applications, APIs, or both, the features showcased here provide a solid foundation and clear examples of best practices.

Start with the [Getting Started Guide](./getting-started.md) to run the applications, then explore the tutorials to dive deeper into specific features that interest you.

Happy coding!
