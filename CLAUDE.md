# Headless CMS

## Overview

This is a headless CMS built with ASP.NET Core 8.0 and EF Core, following the Clean Architecture pattern.

## Tech Stack

- **Language:** C# 12
- **Framework:** ASP.NET Core 8.0
- **Architecture:** Clean Architecture
- **ORM:** Entity Framework Core
- **Database:** SQL Server
- **Authentication:** JWT and API key via middleware
- **Authorization:** Claim-based policies

## Project Structure

```text
.
├─── Common/                          # Shared utilities and base types
│
├─── HCms/                            # Nuget package for CMS content consumers
│    ├── Content/
│    │   └── Repo/                    # Content repositories (sql and rest based)
│    └── Dto/                         # DTOs for notification events
│
├─── HCms.Application/                # CMS core application services (document, fragment, media, etc management) and DTOs
│    ├── Dto/                         # DTO definitions
│    └── Services/                    # Services implementations
│
├─── HCms.Content/                    # Content providing service shared between Nuget package 'HCms' and CMS
│    └── Services/                    # Service implementation
│
├─── HCms.Content.ViewModels/         # Nuget package with view models definitions for CMS content consumers
│
├─── HCms.Domain/                     # Core domain entities and types used by CMS core application services
│    ├── Entities/                    # Domain entities (database models) definitions
│    └── Types/                       # Domain types definitions
│
├─── HCms.Infrastructure/             # External systems and cross‑cutting services
│    ├── Auth/                        # Authentication with Google, Microsoft, Github, Stackoverflow, and LDAP; authorization policies and handlers
│    ├── Media/                       # Low-level work with media storage; filesystem and S3 specific implementations
│    └── Notification/                # Event definition and notification service
│
├─── HCms.Infrastructure.Data/        # Database context and fragment schema repository
│
└─── HCms.Web/                        # Web API and UI layer
     ├── Api/                         # REST Controllers using CMS core application services
     ├── Assets/                      # Static assets and icons
     │    └── FileTypeIcons/          # Icon files for different file types
     ├── Infrastructure/              # Web app specific infrastructure
     │    ├── Auth/                   # Authentication handlers (apikey)
     │    ├── Filters/                # asp.net core filters (csrf protection)
     │    ├── MediaTypeFormatters/    # Custom input and output formatters (meesagepack)
     │    └── Middleware/             # Custom middleware
     ├── InitialData/                 # Seeding and initial data setup
     │    ├── Docs/                   # Demo pages with content
     │    ├── Media/                  # Demo images
     │    └── XmlSchemata/            # XML schema data
     │         └── Optional/          # Optional XML schemas
     ├── Pages/                       # Razor Pages for UI
     │    ├── Auth/                   # Authentication pages
     │    └── Shared/                 # Layout templates
     ├── Resources/                   # Localization resource files
     ├── Services/                    # Web app specific services (grpc and path mapping for remote consumers, file icon provider for media library, etc)
     └── wwwroot/                     # Static web files
          ├── css/                    # Stylesheets
          ├── images/                 # Static images
          └── js/                     # Client-side JavaScript
               ├── code-editor/       # Editor-related scripts
               └── localization/      # Localization scripts and constants
```

### Dependency summary

- `HCms.Domain`: Core domain entities and types. (No project dependencies).
- `HCms.Infrastructure.Data`: Database context and schema repository. Depends on `HCms.Domain`.
- `HCms.Infrastructure`: External concerns implementation. Depends on `HCms.Domain` and `HCms.Infrastructure.Data`.
- `HCms.Application`: Core application services. Depends on `HCms.Domain`, `HCms.Infrastructure`, and `HCms.Infrastructure.Data`.
- `HCms.Content.ViewModels`: View models for content consumers. (No project dependencies).
- `HCms.Content`: Content providing service. Depends on `HCms.Content.ViewModels`, `HCms.Domain`, and `HCms.Infrastructure.Data`.
- `HCms.Web`: The entry point (Web API and UI). Depends on `HCms.Application` and `HCms.Content`.
- `HCms`: A NuGet package containing content repositories. Depends on `HCms.Content`, `HCms.Domain`, and `HCms.Infrastructure.Data` (and references `HCms.Content.ViewModels` via a package reference).

Diagram

```text
HCms.Domain ───► (no dependencies)

HCms.Infrastructure.Data ───► HCms.Domain

HCms.Infrastructure ──►─────────────┬───► HCms.Domain
  └───► HCms.Infrastructure.Data ──►┘

HCms.Application ──►─────────────────────────┐
  ├──► HCms.Infrastructure ──►───────────────┼──► HCms.Domain
  │      └─►─┐                               │
  │          ├─► HCms.Infrastructure.Data ──►┘
  └──►───────┘

HCms.Content.ViewModels ───► (no dependencies)

HCms.Content ──►────────────────────┬───► HCms.Domain
  ├───► HCms.Infrastructure.Data ──►┘
  └───► HCms.Content.ViewModels

HCms.Web
  ├──► HCms.Application ──►──────────────────────────┐
  │      ├──► HCms.Infrastructure ──►────────────────┼──► HCms.Domain
  │      │      └─►─┐                                │
  │      └──►───────┼──► HCms.Infrastructure.Data ─►─┤
  │      ┌──►───────┘                                │
  └──► HCms.Content ──►──────────────────────────────┘
         └──► HCms.Content.ViewModels

HCms ──►───────────────────────────────────────┐
  ├───►───────┐                                │
  │           ├──► HCms.Infrastructure.Data ─►─┼──► HCms.Domain
  │       ┌─►─┘                                │
  └───► HCms.Content ──►───────────────────────┘
          └───► HCms.Content.ViewModels
```
