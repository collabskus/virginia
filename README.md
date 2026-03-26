# Virginia — Address Book

> **LLM-Aided Development Disclaimer**
> This application was developed with significant assistance from **Claude** (Anthropic). Architecture decisions, code generation, bug identification, test authoring, CI pipeline configuration, and this README were produced collaboratively between a human developer and an AI assistant. All code has been reviewed, tested, and validated by the human developer before being committed. AI-generated code may contain patterns, idioms, or structural choices that reflect the model's training data rather than purely organic developer intent. Users and contributors should be aware of this when evaluating the codebase.

[![Build & Test](https://github.com/collabskus/virginia/actions/workflows/ci.yml/badge.svg)](https://github.com/collabskus/virginia/actions/workflows/ci.yml)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

**Live Demo:** [https://virginia.runasp.net/](https://virginia.runasp.net/)
**Source Code:** [https://github.com/collabskus/virginia](https://github.com/collabskus/virginia)

---

## Table of Contents

- [What Is Virginia?](#what-is-virginia)
- [Features](#features)
- [Screenshots](#screenshots)
- [Technology Stack](#technology-stack)
  - [.NET 10](#net-10)
  - [ASP.NET Core](#aspnet-core)
  - [Blazor](#blazor)
  - [.NET Aspire](#net-aspire)
  - [Entity Framework Core](#entity-framework-core)
  - [SQLite](#sqlite)
  - [OpenTelemetry](#opentelemetry)
- [Observability Deep Dive](#observability-deep-dive)
  - [Distributed Tracing and Spans](#distributed-tracing-and-spans)
  - [Metrics](#metrics)
  - [Structured Logging](#structured-logging)
  - [How They Work Together](#how-they-work-together)
- [Project Structure](#project-structure)
- [Architecture](#architecture)
  - [Solution Layout](#solution-layout)
  - [Data Model](#data-model)
  - [Service Layer](#service-layer)
  - [Blazor Components](#blazor-components)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Clone and Run](#clone-and-run)
  - [Running with Aspire](#running-with-aspire)
  - [Running Standalone](#running-standalone)
- [Testing](#testing)
  - [Test Categories](#test-categories)
  - [Test Infrastructure](#test-infrastructure)
  - [Running Tests](#running-tests)
- [CI/CD Pipeline](#cicd-pipeline)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Design Decisions](#design-decisions)
- [Known Limitations](#known-limitations)
- [Contributing](#contributing)
- [License](#license)

---

## What Is Virginia?

Virginia is a full-featured address book web application built with modern .NET technologies. It allows users to manage contacts with multiple email addresses, phone numbers, and mailing addresses per contact, along with profile picture support. The application is named after the Commonwealth of Virginia, where the developer is based.

Virginia serves as both a practical contact management tool and a reference implementation demonstrating how to build a production-quality application with .NET 10, Blazor Server, .NET Aspire, Entity Framework Core, and OpenTelemetry. Every layer of the stack — from the database schema to the UI components to the observability pipeline — is implemented with the same patterns and practices you would use in a real-world enterprise application.

---

## Features

- **Contact Management** — Create, read, update, and delete contacts with first and last names.
- **Multiple Emails** — Each contact can have any number of labeled email addresses (Work, Personal, etc.).
- **Multiple Phones** — Each contact can have any number of labeled phone numbers (Mobile, Home, Office, etc.).
- **Multiple Addresses** — Each contact can have any number of labeled mailing addresses with street, city, state, postal code, and country fields.
- **Profile Pictures** — Upload JPEG, PNG, or WebP images up to 2 MB per contact. Photos are stored directly in the database and served via a dedicated API endpoint with output caching.
- **Advanced Filtering** — Filter the contact list by name, email, phone number, city, state, and whether a photo exists. All text filters are case-insensitive.
- **Debounced Search** — Filter inputs use a 300ms debounce to avoid excessive database queries while typing.
- **Pagination** — The contact list is paginated with configurable page sizes (clamped to a maximum of 100 to prevent abuse).
- **Sorted Results** — Contacts are always sorted by last name, then first name.
- **Delete Confirmation** — Deleting a contact requires a two-step confirmation to prevent accidental data loss.
- **Responsive Design** — The UI adapts gracefully to mobile, tablet, and desktop screen widths. Columns collapse progressively on smaller screens.
- **Keyboard Accessibility** — Table rows are focusable and navigable with the Enter key.
- **Form Validation** — All forms use DataAnnotations-based validation with immediate feedback. Phone numbers, email addresses, and postal codes are validated with regular expressions.
- **Error Handling** — Both the list page and detail page handle errors gracefully with user-visible banners rather than crashing to the error boundary.
- **Full Observability** — Every service operation emits distributed traces (spans), custom metrics (counters and histograms), and structured log messages.
- **Health Checks** — Readiness (`/health`) and liveness (`/alive`) endpoints are exposed in development mode.
- **Reconnection UX** — If the Blazor Server SignalR connection drops, a modal guides the user through reconnection with retry and resume capabilities.

---

## Screenshots

The application has two primary views:

**Contact List** — The home page displays all contacts in a filterable, paginated table with avatar initials or profile pictures, primary email, primary phone, and primary city.

**Contact Detail** — Clicking a contact (or the "+ New Contact" button) opens a form with cards for basic info, emails, phones, addresses, and profile pictures. Child items can be added and removed dynamically.

---

## Technology Stack

### .NET 10

.NET is a free, open-source, cross-platform framework created by Microsoft for building applications of all kinds — web, desktop, mobile, cloud, gaming, IoT, and machine learning. .NET 10 is the latest Long-Term Support (LTS) release, which means it receives three years of security patches and bug fixes.

At its core, .NET provides a Common Language Runtime (CLR) that executes managed code written in languages like C#, F#, and Visual Basic. The CLR handles memory management through garbage collection, type safety through a Common Type System, and just-in-time (JIT) compilation that translates Intermediate Language (IL) bytecode into native machine code at runtime. Modern .NET also supports Ahead-of-Time (AOT) compilation for scenarios where startup time matters more than peak throughput.

The .NET SDK is the development toolkit that includes the compiler (`dotnet build`), package manager (NuGet), test runner (`dotnet test`), and project system. Projects are defined in `.csproj` files using MSBuild XML, and solutions (`.slnx` in the modern format) group multiple projects together.

Virginia targets `net10.0` as specified in `Directory.Build.props`, which is a shared MSBuild property file that applies settings to every project in the solution. This file also enables nullable reference types (`<Nullable>enable</Nullable>`), implicit `using` directives, warnings-as-errors, code style enforcement during builds, and centralized NuGet package version management.

### ASP.NET Core

ASP.NET Core is the web framework within .NET. It is a complete rewrite of the legacy ASP.NET Framework, designed from the ground up to be cross-platform, high-performance, and modular. ASP.NET Core uses a middleware pipeline architecture where each HTTP request flows through a chain of middleware components — each can inspect, modify, short-circuit, or pass the request to the next component.

In Virginia's `Program.cs`, you can see the middleware pipeline being configured:

- `UseExceptionHandler` catches unhandled exceptions and renders the Error page.
- `UseHsts` sends HTTP Strict Transport Security headers in production.
- `UseStatusCodePagesWithReExecute` handles 404s by rendering the NotFound page.
- `UseHttpsRedirection` redirects HTTP requests to HTTPS.
- `UseAntiforgery` protects form submissions against Cross-Site Request Forgery attacks.
- `MapStaticAssets` serves CSS, JavaScript, and other static files from `wwwroot`.
- `MapRazorComponents` maps Blazor component routes.

ASP.NET Core also provides Minimal APIs, which Virginia uses for the profile picture endpoint. Minimal APIs let you define HTTP endpoints as inline lambda functions without the overhead of controllers, making them perfect for small, focused endpoints.

### Blazor

Blazor is a UI framework within ASP.NET Core that lets developers build interactive web interfaces using C# instead of JavaScript. Blazor comes in several hosting models:

**Blazor Server** (used by Virginia) — The UI runs on the server. User interactions are sent to the server over a persistent SignalR (WebSocket) connection, the server executes the component logic and computes a UI diff, and the diff is sent back to the browser where a small JavaScript runtime applies the DOM changes. This model has several advantages: the application code never leaves the server (good for security), the initial page load is fast (no large WASM download), and the app has direct access to server-side resources like databases and file systems. The trade-off is that every interaction requires a network round-trip, and the server must maintain state (a "circuit") for each connected client.

**Blazor WebAssembly** — The entire .NET runtime and application are downloaded to the browser and executed client-side via WebAssembly. This model works offline and reduces server load, but has a larger initial download and cannot directly access server-side resources.

**Blazor Hybrid** — Blazor components are hosted inside native desktop or mobile applications using .NET MAUI or WPF.

Virginia uses Blazor Server with Interactive Server rendering mode (`@rendermode InteractiveServer`), which means components are pre-rendered on the server for the initial HTTP response, then "hydrated" into an interactive circuit when the SignalR connection is established.

Blazor components are defined in `.razor` files, which mix HTML markup with C# code. Each component can have parameters (inputs from parent components), inject services via dependency injection, and manage its own state. Components can also have isolated CSS files (`.razor.css`) that are automatically scoped to that component using CSS isolation — the build system rewrites selectors to include a unique attribute, preventing style conflicts.

Key Blazor concepts used in Virginia:

- **EditForm / DataAnnotationsValidator** — Blazor's form system that binds to model objects and validates them using `System.ComponentModel.DataAnnotations` attributes like `[Required]`, `[MaxLength]`, `[EmailAddress]`, and `[RegularExpression]`.
- **InputText / InputFile** — Built-in input components that integrate with the form validation system. `InputFile` handles file uploads through the SignalR connection.
- **@bind / @bind:event / @bind:after** — Two-way data binding directives. `@bind:event="oninput"` binds on every keystroke (not just on blur), and `@bind:after` runs a callback after the value changes (used for debounced filtering).
- **@key** — Tells Blazor's diffing algorithm to track elements by a stable key rather than by position, which improves performance and correctness when lists change.
- **@implements IDisposable** — Allows components to clean up resources (like cancellation tokens) when they are removed from the render tree.
- **NavigationManager** — A service for programmatic navigation between pages.
- **CascadingParameter** — A parameter that flows down through the component tree without explicit passing (used for `HttpContext` in the Error page).

### .NET Aspire

.NET Aspire is an opinionated framework for building cloud-ready, observable, distributed applications. It is not a runtime or a hosting platform — it is a set of NuGet packages, project templates, and tooling that make it easier to configure the cross-cutting concerns that every production application needs: health checks, service discovery, resilience, and telemetry.

Aspire introduces two key concepts:

**App Host** (`Virginia.AppHost`) — A special project that orchestrates the entire application during development. It defines which projects, containers, and external resources make up the application and how they connect to each other. When you run the App Host, it starts all the projects, sets up environment variables for service discovery, and launches the Aspire Dashboard — a web UI that shows real-time logs, traces, and metrics for all components.

**Service Defaults** (`Virginia.ServiceDefaults`) — A shared library that configures common infrastructure for all service projects. Virginia's `Extensions.cs` file wires up OpenTelemetry (logging, metrics, and tracing), health check endpoints, service discovery, and HTTP client resilience (retry policies, circuit breakers, timeouts) using `Microsoft.Extensions.Http.Resilience`.

The Aspire Dashboard is a powerful development tool. When you run `dotnet run --project Virginia.AppHost`, the dashboard launches automatically and gives you a real-time view of all telemetry data: structured logs with severity and scope, distributed traces showing the full request lifecycle with timing breakdowns, and metrics with counters and histograms.

Virginia's App Host is minimal — it just registers the main Virginia web project. In a more complex application, the App Host would also register databases, message queues, caches, and other backing services, and Aspire would handle connection string injection and health check wiring automatically.

### Entity Framework Core

Entity Framework Core (EF Core) is the official Object-Relational Mapper (ORM) for .NET. It lets you define your database schema as C# classes (entities), write queries using LINQ (Language Integrated Query), and have the framework translate everything to SQL at runtime.

Virginia's data layer consists of:

- **Entities** (`Entities.cs`) — Plain C# classes with properties that map to database columns. `Contact` is the aggregate root with navigation properties to `ContactEmail`, `ContactPhone`, and `ContactAddress`. Relationships are one-to-many with cascade delete.
- **DbContext** (`AppDbContext.cs`) — The main EF Core class that represents a session with the database. It exposes `DbSet<T>` properties for each entity and configures the schema in `OnModelCreating` — indexes, foreign keys, and delete behavior.
- **DTOs** (`Dtos.cs`) — Data Transfer Objects used to project query results into lightweight, read-only shapes. Using DTOs instead of returning entities directly prevents issues like lazy loading N+1 queries and over-exposing internal data structures.
- **Form Models** (`FormModels.cs`) — Mutable classes with validation attributes that represent the shape of user input. These are separate from entities because form validation rules and database constraints are different concerns.

EF Core translates LINQ queries into SQL. For example, the name filter:

```csharp
query = query.Where(c =>
    c.FirstName.ToLower().Contains(term)
    || c.LastName.ToLower().Contains(term));
```

Gets translated to something like:

```sql
WHERE lower("c"."FirstName") LIKE '%' || @term || '%'
   OR lower("c"."LastName") LIKE '%' || @term || '%'
```

The `.ToLower()` call is critical for SQLite, which performs case-sensitive string comparisons by default. Without it, searching for "linc" would not match "Lincoln".

Virginia uses `AsNoTracking()` for all read queries, which tells EF Core not to track the returned entities in its change tracker. This reduces memory allocation and improves performance for queries where you do not intend to modify and save the results.

### SQLite

SQLite is an embedded relational database engine. Unlike PostgreSQL, MySQL, or SQL Server, SQLite does not run as a separate server process — the database is a single file on disk, and the application accesses it directly through a library linked into the process. This makes SQLite perfect for applications that need a real SQL database without the operational complexity of managing a database server.

Virginia stores its SQLite database at the path configured in `appsettings.json` under `ConnectionStrings:DefaultConnection` (default: `Data Source=virginia.db`). The database is created automatically on startup via `db.Database.EnsureCreatedAsync()`.

SQLite has some important behavioral differences from other databases. The most relevant for Virginia is that the built-in `LIKE` operator and `instr()` function are case-sensitive for ASCII characters. EF Core's `.Contains()` method translates to `instr()`, so Virginia explicitly uses `.ToLower()` on both the column and the search term to achieve case-insensitive filtering.

### OpenTelemetry

OpenTelemetry (OTel) is a vendor-neutral, open standard for collecting telemetry data from applications — traces, metrics, and logs. It is maintained by the Cloud Native Computing Foundation (CNCF) and has become the industry standard for observability instrumentation.

The core idea of OpenTelemetry is to separate *instrumentation* (adding telemetry to your code) from *export* (sending that telemetry to a backend for storage and analysis). You instrument your code once using the OpenTelemetry API, and then configure exporters to send the data to whichever backends you use — Jaeger, Prometheus, Grafana, Datadog, New Relic, Azure Monitor, or the Aspire Dashboard.

Virginia configures OpenTelemetry in `Extensions.cs` (the Aspire Service Defaults):

- **Logging** — `AddOpenTelemetry()` on the logging builder sends structured log messages through the OTel pipeline. `IncludeFormattedMessage` includes the human-readable message, and `IncludeScopes` includes any logging scopes (like the current request context).
- **Metrics** — `AddAspNetCoreInstrumentation()` collects HTTP request metrics (request duration, active requests, etc.), `AddHttpClientInstrumentation()` collects outgoing HTTP call metrics, and `AddRuntimeInstrumentation()` collects .NET runtime metrics (GC pressure, thread pool usage, etc.).
- **Tracing** — `AddAspNetCoreInstrumentation()` creates spans for incoming HTTP requests, and `AddHttpClientInstrumentation()` creates spans for outgoing HTTP calls. The filter excludes health check endpoints from tracing to reduce noise.
- **OTLP Exporter** — If the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable is set (which Aspire sets automatically), all telemetry is exported over the OpenTelemetry Protocol (OTLP) to the Aspire Dashboard or any other OTLP-compatible backend.

Virginia also registers its own custom telemetry sources in `Program.cs`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(ContactTelemetry.ServiceName))
    .WithMetrics(metrics => metrics.AddMeter(ContactTelemetry.ServiceName));
```

This tells the OTel SDK to listen for spans from `ContactTelemetry.Source` and metrics from the `ContactTelemetry` meter, in addition to the auto-instrumented sources.

---

## Observability Deep Dive

### Distributed Tracing and Spans

Distributed tracing is a method for tracking a single request as it flows through a system. Each unit of work is represented as a **span** — a named, timed operation with a start time, duration, status, and arbitrary key-value **tags** (also called attributes).

Spans are organized in a tree structure. The root span represents the overall request (e.g., an incoming HTTP request), and child spans represent sub-operations within that request (e.g., a database query, a service call, a cache lookup). The parent-child relationships show you exactly where time is being spent.

A **trace** is the complete tree of spans for a single request. Each trace has a unique **trace ID**, and each span has a unique **span ID** plus a reference to its **parent span ID**. These IDs are propagated across service boundaries via HTTP headers (the W3C Trace Context standard), which is how distributed tracing works across multiple services.

In Virginia, the `ContactTelemetry` class creates an `ActivitySource` (the .NET equivalent of an OTel tracer):

```csharp
public static readonly ActivitySource Source = new(ServiceName);
```

Each service method starts a span:

```csharp
using var activity = ContactTelemetry.Source.StartActivity("ListContacts");
activity?.SetTag("filter.name", filter.Name);
activity?.SetTag("page", page);
```

The `using` keyword ensures the span is automatically stopped (and its duration recorded) when the method exits. Tags provide queryable metadata — you can search traces by filter values, contact IDs, or any other tag in your tracing backend.

The `activity?` null-conditional operator handles the case where no listener is registered for this source (e.g., in tests or when the OTLP exporter is not configured). In that case, `StartActivity` returns `null` and the tag calls are no-ops, adding zero overhead.

### Metrics

Metrics are numerical measurements collected over time. Unlike traces (which capture individual requests), metrics capture aggregate behavior — how many requests per second, what is the 95th percentile latency, how many errors occurred.

OpenTelemetry defines three fundamental metric instruments:

**Counter** — A monotonically increasing value. It only goes up. Examples: total requests served, total bytes sent, total contacts created. In Virginia:

```csharp
private readonly Counter<long> _created;
_created = meter.CreateCounter<long>("contacts.created", "contacts", "Contacts created");

// Usage:
public void RecordContactCreated() => _created.Add(1);
```

**Histogram** — Records the distribution of values. It automatically computes statistics like count, sum, min, max, and percentile buckets. Examples: request duration, response size, query execution time. In Virginia:

```csharp
private readonly Histogram<double> _queryDuration;
_queryDuration = meter.CreateHistogram<double>("contacts.query.duration", "ms", "Query duration");

// Usage:
public void RecordQueryDuration(double ms) => _queryDuration.Record(ms);
```

**Gauge** (not used in Virginia) — A value that can go up or down, representing a current measurement. Examples: current temperature, active connections, queue depth.

Virginia's `ContactTelemetry` class defines five custom metrics: three counters (`contacts.created`, `contacts.updated`, `contacts.deleted`) and two histograms (`contacts.query.duration`, `contacts.write.duration`). These are in addition to the auto-instrumented ASP.NET Core metrics and .NET runtime metrics.

The `IMeterFactory` parameter in `ContactTelemetry`'s constructor is important — it is the .NET 8+ way of creating meters that integrate with the dependency injection system and are properly disposed when the application shuts down. The meter name (`Virginia.Contacts`) must be registered with `AddMeter()` in the OTel configuration for the metrics to be exported.

### Structured Logging

Traditional logging produces unstructured text strings that are difficult to search and analyze. Structured logging produces log events with typed, named properties that can be indexed, filtered, and aggregated by log analysis tools.

Virginia uses .NET's source-generated logging pattern, which is the highest-performance approach:

```csharp
[LoggerMessage(Level = LogLevel.Information,
    Message = "Listed {Count}/{Total} contacts in {ElapsedMs:F1}ms (page {Page})")]
public static partial void ListedContacts(
    ILogger logger, int count, int total, double elapsedMs, int page);
```

The `[LoggerMessage]` attribute tells the C# source generator to create an optimized implementation at compile time. This avoids the boxing allocations and string interpolation overhead of traditional `logger.LogInformation($"...")` calls. The generated code checks `IsEnabled(LogLevel.Information)` before doing any work, so disabled log levels have near-zero cost.

The properties (`Count`, `Total`, `ElapsedMs`, `Page`) are preserved as structured data. When exported through OpenTelemetry to a backend like the Aspire Dashboard, you can filter logs by these properties — e.g., show me all log entries where `ElapsedMs > 100`.

### How They Work Together

The three pillars of observability — traces, metrics, and logs — are most powerful when correlated:

1. **Metrics** tell you *what* is happening at a high level. Your dashboard shows that `contacts.query.duration` p95 spiked from 5ms to 200ms.
2. **Traces** tell you *where* the problem is. You filter traces by duration > 100ms and see that the `ListContacts` span is slow, and drill down to see it is the database query that is taking time.
3. **Logs** tell you *why*. The structured log entry for that trace shows `filter.name = "a"` and `Total = 50000` — the filter was too broad and matched too many rows.

OpenTelemetry automatically correlates these three signals. Log entries include the current trace ID and span ID, so you can jump from a metric alert to the relevant traces to the specific log entries. This is the fundamental value proposition of OpenTelemetry: unified, correlated observability.

---

## Project Structure

```
Virginia/
├── .editorconfig                         # Code style rules for all editors/IDEs
├── .github/workflows/ci.yml             # GitHub Actions CI pipeline
├── .gitignore                            # Git ignore rules
├── Directory.Build.props                 # Shared MSBuild properties for all projects
├── Directory.Packages.props              # Central NuGet package version management
├── Export.ps1                            # PowerShell script to export project for LLM context
├── Virginia.slnx                         # Solution file (modern XML format)
│
├── Virginia.AppHost/                     # .NET Aspire orchestrator
│   ├── AppHost.cs                        # Application model definition
│   ├── appsettings.json                  # Aspire host configuration
│   └── Virginia.AppHost.csproj
│
├── Virginia.ServiceDefaults/             # Shared infrastructure (OTel, health, resilience)
│   ├── Extensions.cs                     # AddServiceDefaults / ConfigureOpenTelemetry
│   └── Virginia.ServiceDefaults.csproj
│
├── Virginia/                             # Main web application
│   ├── Components/
│   │   ├── App.razor                     # Root HTML document
│   │   ├── Routes.razor                  # Router configuration
│   │   ├── _Imports.razor                # Global using directives for Razor
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor          # App shell (header, main, footer)
│   │   │   ├── MainLayout.razor.css      # Scoped layout styles
│   │   │   ├── ReconnectModal.razor      # SignalR reconnection UI
│   │   │   ├── ReconnectModal.razor.css  # Reconnect modal styles
│   │   │   └── ReconnectModal.razor.js   # Reconnect/resume JavaScript
│   │   └── Pages/
│   │       ├── ContactList.razor         # Home page — filterable, paginated table
│   │       ├── ContactList.razor.css     # List page scoped styles
│   │       ├── ContactDetail.razor       # Create/edit contact form
│   │       ├── ContactDetail.razor.css   # Detail page scoped styles
│   │       ├── Error.razor               # Unhandled exception page
│   │       └── NotFound.razor            # 404 page
│   ├── Data/
│   │   ├── AppDbContext.cs               # EF Core DbContext with schema config
│   │   ├── Entities.cs                   # Contact, ContactEmail, ContactPhone, ContactAddress
│   │   ├── Dtos.cs                       # Read-only projections, paging, filters
│   │   └── FormModels.cs                 # Validated form input models
│   ├── Services/
│   │   ├── IContactService.cs            # Service interface (contract)
│   │   ├── ContactService.cs             # Service implementation (all CRUD logic)
│   │   └── ContactTelemetry.cs           # Custom ActivitySource + Meter
│   ├── Program.cs                        # Application entry point and DI configuration
│   ├── appsettings.json                  # Production configuration
│   ├── appsettings.Development.json      # Development configuration overrides
│   ├── wwwroot/app.css                   # Global styles
│   └── Virginia.csproj                   # Project file
│
├── Virginia.Tests/                       # Unit and integration tests
│   ├── ContactServiceTests.cs            # 35+ tests covering all service operations
│   ├── DtoMappingTests.cs                # DTO mapping and PagedResult tests
│   ├── FormValidationTests.cs            # DataAnnotations validation tests
│   ├── TestInfrastructure.cs             # TestHarness with in-memory SQLite
│   └── Virginia.Tests.csproj
│
└── docs/llm/
    ├── claude.md                         # LLM project instructions
    └── dump.txt                          # Full project export for LLM context
```

---

## Architecture

### Solution Layout

The solution follows the standard .NET Aspire structure with four projects:

| Project | Type | Purpose |
|---------|------|---------|
| `Virginia.AppHost` | Aspire App Host | Orchestrates the app during development. Starts all projects, configures service discovery, launches the Aspire Dashboard. |
| `Virginia.ServiceDefaults` | Class Library | Shared infrastructure: OpenTelemetry, health checks, HTTP resilience, service discovery. Referenced by all service projects. |
| `Virginia` | Blazor Web App | The main application. Contains all UI components, data access, and business logic. |
| `Virginia.Tests` | xUnit Test Project | Unit and integration tests. Uses in-memory SQLite for fast, isolated database testing. |

### Data Model

The database has four tables with a straightforward one-to-many relationship pattern:

```
Contact (1) ──── (*) ContactEmail
    │
    ├──────── (*) ContactPhone
    │
    └──────── (*) ContactAddress
```

The `Contact` entity is the aggregate root. All child entities have a foreign key (`ContactId`) back to their parent and are configured with cascade delete — when a contact is deleted, all its emails, phones, and addresses are automatically removed.

Profile pictures are stored as `byte[]` directly in the `Contact` table alongside the content type string. This is a deliberate simplicity choice — for a production application with many users and large images, you would typically store images in blob storage (Azure Blob Storage, S3, etc.) and keep only a reference in the database.

### Service Layer

All business logic lives in `ContactService`, which implements `IContactService`. The service is registered as scoped (one instance per request/circuit) because it depends on `AppDbContext`, which is also scoped.

Every public method follows the same pattern:

1. Start a tracing span with descriptive tags.
2. Start a stopwatch for timing.
3. Execute the database operation within a transaction (for writes).
4. Record metrics (counters for operations, histograms for durations).
5. Write a structured log message.
6. Return the result.

The `SyncChildren` helper method handles the complex logic of updating child collections (emails, phones, addresses). When the user submits an edited contact form, the method compares the submitted items against the existing database items by ID, and performs three operations: removes items that are no longer present, updates items that still exist, and adds new items that have no ID.

### Blazor Components

The UI consists of two main pages and a shared layout:

**MainLayout** — The app shell with a sticky header (logo + subtitle), main content area, and footer. The header uses a dark theme (`#1a1a2e`) with the Virginia brand color (`#e94560`).

**ContactList** — The home page. It loads contacts on initialization and re-loads whenever filters change (with 300ms debounce). The component implements `IDisposable` to clean up the debounce `CancellationTokenSource`. Each table row is clickable and keyboard-navigable, and uses `@key` for efficient list diffing.

**ContactDetail** — Serves both create (`/contacts/new`) and edit (`/contacts/{id}`) routes via an optional `Id` parameter. The form dynamically adds and removes child items (emails, phones, addresses) using list manipulation. Delete requires a two-step confirmation. Photo upload validates file size and content type client-side before streaming to the server.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (includes the runtime and CLI tools)
- A code editor: [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended), [VS Code](https://code.visualstudio.com/) with the C# Dev Kit extension, or [JetBrains Rider](https://www.jetbrains.com/rider/)
- Git

### Clone and Run

```bash
git clone https://github.com/collabskus/virginia.git
cd virginia
```

### Running with Aspire

This is the recommended way to run during development. The Aspire App Host starts the application and launches the Aspire Dashboard for real-time observability.

```bash
dotnet run --project Virginia.AppHost
```

The console output will show URLs for both the application and the Aspire Dashboard. The dashboard is typically available at `https://localhost:17205`.

### Running Standalone

If you prefer to run the web application directly without the Aspire orchestrator:

```bash
dotnet run --project Virginia
```

The application will be available at the URLs configured in `Virginia/Properties/launchSettings.json` (default: `https://virginia.dev.localhost:7140`).

The SQLite database file (`virginia.db`) is created automatically in the project directory on first run.

---

## Testing

### Test Categories

Virginia has three test classes covering different concerns:

**ContactServiceTests** (35+ tests) — Integration tests that exercise the full `ContactService` through a real SQLite database (in-memory). These tests cover every CRUD operation, all six filter types, case-insensitive searching, pagination, sort ordering, page size clamping, profile picture lifecycle, edge cases (empty database, page beyond range, whitespace-only filters), and error conditions (updating/deleting non-existent contacts).

**FormValidationTests** (15 tests) — Unit tests that verify DataAnnotations validation on all form models. These test required fields, max lengths, email format validation, phone number regex patterns, postal code regex patterns, and optional fields.

**DtoMappingTests** (3 tests) — Unit tests for the `ContactFormModel.FromDetail()` mapping method and the `PagedResult<T>` computed properties (`TotalPages`, `HasPrevious`, `HasNext`).

### Test Infrastructure

The `TestHarness` class creates an isolated test environment for each test:

1. Opens a new in-memory SQLite connection (each connection gets its own empty database).
2. Creates a fresh `AppDbContext` and applies the schema via `EnsureCreatedAsync`.
3. Creates a `ContactTelemetry` instance with a no-op `IMeterFactory`.
4. Creates a `ContactService` with a null logger.
5. Returns the harness, which exposes `Db` (for direct database assertions) and `Service` (for testing the service layer).

The harness implements `IAsyncDisposable` and is used with `await using`, so the database connection and context are properly disposed after each test. Because each test gets its own in-memory database, tests can run in parallel without interfering with each other.

### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run a specific test class
dotnet test --filter "FullyQualifiedName~ContactServiceTests"

# Run a specific test
dotnet test --filter "FullyQualifiedName~List_FilterByName_CaseInsensitive"
```

---

## CI/CD Pipeline

Virginia uses GitHub Actions for continuous integration. The pipeline is defined in `.github/workflows/ci.yml` and runs on every push to `main` and every pull request targeting `main`.

The pipeline performs four steps:

1. **Checkout** — Clones the repository using `actions/checkout@v4`.
2. **Setup .NET** — Installs the .NET 10 SDK using `actions/setup-dotnet@v4`.
3. **Restore** — Downloads NuGet packages: `dotnet restore Virginia.slnx`.
4. **Build** — Compiles the solution in Release configuration: `dotnet build Virginia.slnx --no-restore --configuration Release`. Because `TreatWarningsAsErrors` is enabled, any compiler warning fails the build.
5. **Test** — Runs all tests: `dotnet test Virginia.slnx --no-build --configuration Release`. Test results are saved in `.trx` format.
6. **Upload Results** — Uploads `.trx` test result files as a build artifact using `actions/upload-artifact@v4`. The `if: always()` condition ensures results are uploaded even when tests fail, so you can inspect failures from the GitHub Actions UI.

---

## Configuration

Virginia uses the standard ASP.NET Core configuration system, which merges settings from multiple sources in priority order: `appsettings.json` → `appsettings.{Environment}.json` → environment variables → command-line arguments.

**`appsettings.json`** (production defaults):

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings:DefaultConnection` | `Data Source=virginia.db` | SQLite database file path |
| `ProfilePicture:MaxSizeBytes` | `2097152` (2 MB) | Maximum upload size for profile pictures |
| `ProfilePicture:AllowedContentTypes` | `["image/jpeg", "image/png", "image/webp"]` | Accepted image MIME types |

**`appsettings.Development.json`** (development overrides):

| Key | Default | Description |
|-----|---------|-------------|
| `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command` | `Information` | Logs every SQL query to the console during development |

---

## API Endpoints

Virginia is primarily a Blazor Server application where all UI interaction happens over SignalR. However, it exposes one Minimal API endpoint:

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/contacts/{id:int}/photo` | Returns the profile picture for a contact as a binary file response. Returns 404 if the contact does not exist or has no photo. Responses are cached for 5 minutes using output caching, with cache variation by route value (`id`). |

Health check endpoints (development only):

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Readiness check — returns 200 if the application is healthy |
| `GET` | `/alive` | Liveness check — returns 200 if the application is running (subset of health checks tagged with "live") |

---

## Design Decisions

**SQLite over PostgreSQL/SQL Server** — Virginia is a personal address book, not a multi-tenant SaaS application. SQLite eliminates the need for a separate database server, simplifies deployment (the entire application is a single process plus a single file), and is more than fast enough for the expected workload. If Virginia needed concurrent write-heavy workloads or multi-server deployment, PostgreSQL would be the right choice.

**Profile pictures in the database** — Storing images as BLOBs in SQLite keeps the deployment model simple (one file = the entire database). The 2 MB size limit keeps row sizes reasonable. For a production application with thousands of users, you would move images to blob storage and store only a URL in the database.

**Blazor Server over Blazor WebAssembly** — Server-side rendering gives direct database access without needing a web API layer, faster initial page loads, and simpler deployment. The trade-off is the per-user server memory for SignalR circuits, which is acceptable for a personal/small-team application.

**Scoped CSS over a CSS framework** — Virginia uses hand-written CSS with Blazor's built-in CSS isolation rather than Tailwind, Bootstrap, or another framework. This keeps the application self-contained with zero JavaScript dependencies (beyond Blazor's own runtime) and gives full control over the design.

**Source-generated logging over string interpolation** — The `[LoggerMessage]` attribute generates zero-allocation logging code at compile time. This matters because logging is called on every service operation, and the accumulated allocation savings are significant under load.

**Central package management** — `Directory.Packages.props` defines all NuGet package versions in one place. This prevents version drift between projects and makes upgrades a single-file change.

**Warnings as errors** — `TreatWarningsAsErrors` in `Directory.Build.props` ensures that code quality issues (nullable reference warnings, unused variables, etc.) cannot accumulate silently. The CI pipeline enforces this — if it compiles locally, it compiles in CI.

---

## Known Limitations

- **No authentication or authorization.** The application is open to anyone who can reach it. Adding ASP.NET Core Identity with cookie authentication is the natural next step.
- **No database migrations.** The database is created via `EnsureCreatedAsync`, which creates the schema from the current entity model but does not support incremental schema changes. For a production application, you would use EF Core Migrations (`dotnet ef migrations add`).
- **No full-text search.** Filtering uses `LIKE '%term%'` which cannot use indexes and performs a full table scan. For large contact databases, you would add SQLite FTS5 (Full-Text Search) or switch to a database with built-in full-text search.
- **No import/export.** There is no way to bulk import contacts from CSV/vCard or export them.
- **No contact groups or tags.** Contacts cannot be categorized or organized beyond the flat alphabetical list.
- **Single-user model.** There is no concept of separate users with separate contact databases.
- **No undo for delete.** Once a contact is deleted (after confirmation), it is permanently removed. A soft-delete pattern (setting a `DeletedAtUtc` timestamp) would allow recovery.
- **Profile pictures are served from the database.** Every photo request hits the database. The output cache mitigates this for repeated requests, but a file-system or CDN-based approach would be better at scale.

---

## Contributing

Contributions are welcome. To contribute:

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/your-feature`.
3. Make your changes. Ensure all tests pass: `dotnet test`.
4. Ensure the build succeeds with no warnings: `dotnet build --configuration Release`.
5. Commit with a descriptive message.
6. Push to your fork and open a Pull Request.

Please follow the existing code style as defined in `.editorconfig`. The project uses file-scoped namespaces, primary constructors, expression-bodied members where appropriate, and collection expressions (`[.. items]`).

---

## License

This project is licensed under the **GNU Affero General Public License v3.0** (AGPL-3.0).

This means you are free to use, modify, and distribute this software, but if you run a modified version on a server and let others interact with it over a network, you must make the complete source code of the modified version available to those users under the same AGPL-3.0 license.

See the [LICENSE](LICENSE) file for the full license text, or visit [https://www.gnu.org/licenses/agpl-3.0.html](https://www.gnu.org/licenses/agpl-3.0.html).

---

Built with care in Newport News, Virginia.
