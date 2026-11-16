# Architecture Guide: ASPHotelM2631

## Clean Architecture Overview

**Clean Architecture** is a software design philosophy centered on **separation of concerns** and **independence from frameworks, databases, and UI**. Instead of letting database logic or web framework code bleed into core business logic, the system is divided into concentric layers governed by one strict rule:
**The Dependency Rule:** Source code dependencies must only point **inward**, toward higher-level business rules. Inner layers must never know about, reference, or depend on outer layers.

[ Frameworks & Drivers ]  ──►  ASPHotelM2631.Web (UI, HTTP, DI Composition Root)
          │
          ▼
[ Interface Adapters   ]  ──►  ASPHotelM2631.Infrastructure (EF Core, SQL Server, External APIs)
          │
          ▼
[ Application Rules    ]  ──►  ASPHotelM2631.Application (Use Cases, Interfaces/Ports, DTOs)
          │
          ▼
[ Enterprise Rules     ]  ──►  ASPHotelM2631.Domain (Core Entities, Value Objects, Pure C#)

*   Never inject AppDbContext into a Controller. Always go through an Application service.
*   Keep ViewModels in .Web/Models and Entities in .Domain/Entities. Do not pass Domain entities directly into Razor Views.

## Anatomy:
ASPHotelM2631/ (Solution Root)
├── ASPHotelM2631.slnx
├── ASPHotelM2631.Domain/          # Innermost Core (Class Library)
├── ASPHotelM2631.Application/     # Business Orchestration (Class Library)
├── ASPHotelM2631.Infrastructure/  # External Concerns & Persistence (Class Library)
└── ASPHotelM2631.Web/             # Presentation & Entry Point (ASP.NET Core MVC)

**ASPHotelM2631.Domain (Enterprise Business Rules)**
Template: Class Library (.NET 9.0)
Project References: None. (Zero external NuGet packages, zero EF Core, zero web dependencies).
*Responsibilities:*
    Core domain entities (e.g., Room, Booking, Guest) and value objects.
    Domain-specific exceptions and intrinsic business invariants (e.g., validating that a check-out date is after a check-in date).
    ──=──
**ASPHotelM2631.Application (Application Business Rules)**
Template: Class Library (.NET 9.0)
Project References: ASPHotelM2631.Domain
*Responsibilities:*
    Use Cases / Services: Orchestrates workflows (e.g., BookingService, RoomAvailabilityService).
    Interfaces (Ports): Defines contracts for external needs without caring how they are implemented (e.g., IBookingRepository, IEmailSender).
    DTOs / Commands / Queries: Plain data structures used to pass data into and out of use cases without exposing Domain entities directly to the UI.
    ──=──
**ASPHotelM2631.Infrastructure (Persistence & External Systems)**
Template: Class Library (.NET 9.0)
Project References: ASPHotelM2631.Application, ASPHotelM2631.Domain
*Responsibilities:*
    Database Access: Houses AppDbContext, Entity Framework Core configurations, and database migrations.
    Repository Implementations: Implements the interfaces defined in Application (e.g., BookingRepository : IBookingRepository).
    Third-Party Integrations: SMTP email clients, payment gateways, file storage, or external APIs.
    ──=──
**ASPHotelM2631.Web (Presentation & Composition Root)**
Template: ASP.NET Core Web App (Model-View-Controller)
Project References: ASPHotelM2631.Application, ASPHotelM2631.Infrastructure
*Responsibilities:*
    MVC UI Delivery: Razor Views (Views/), static assets (wwwroot/), and UI-specific ViewModels (Models/).
    Thin Controllers: Receive HTTP requests, validate UI ModelState, delegate work to Application services, and return Views or redirects. Controllers never inject AppDbContext.
    Composition Root (Program.cs): The single place where concrete Infrastructure classes are bound to Application interfaces in the Dependency Injection container.
    ──=──


## Current Nuget Usage

*   .Web/
Microsoft.AspNetCore.Identity.UI 9.0.23
    Razor Pages UI components for ASP.NET Core Identity belong directly in the Web layer.
Microsoft.VisualStudio.Web.CodeGeneration.Design 9.0.13
    Scaffolding tool used exclusively by Visual Studio to generate controllers and views.

*   .Infrastructure/
Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.23
    Ties ASP.NET Core Identity to EF Core storage providers (AspNetUsers, AspNetRoles etc). This belongs in Infrastructure because it deals directly with database mapping and persistence.
Microsoft.EntityFrameworkCore 9.0.23
    Core ORM engine.
Microsoft.EntityFrameworkCore.SqlServer 9.0.23
    The SQL Server database provider.
Microsoft.EntityFrameworkCore.Design 9.0.23
    Required at design-time for building migrations. (Even if used via CLI from the root, placing it in Infrastructure or keeping it as a development dependency here ensures migrations can scaffold correctly).
Microsoft.EntityFrameworkCore.Tools 9.0.23
    Package for Package Manager Console command kept in Infrastructure (or Web if preferred command execution, but Infrastructure is standard since it holds the AppDbContext).

*   .Domain/
None
*   .Application/
None