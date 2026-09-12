# Campus Services Portal - Project Documentation Index

This directory contains the formal documentation deliverables required by the **Business Requirements Document (BRD v2.0 - Angular, Section 12 Deliverables)** for the **Campus Services Portal**.

---

## Deliverables Catalog

### 1. [05-Angular-ModuleCoverage-Matrix.md](./05-Angular-ModuleCoverage-Matrix.md)
* **Purpose**: Formal evidence mapping every Angular framework concept and curriculum requirement directly to active implementation file paths in the workspace.
* **Key Contents**:
  - Standalone Component Architecture & Bootstrap (pp.config.ts)
  - Route Parameters, Query Parameters, Child/Nested Routes & Wildcard (**)
  - Functional Route Guards (AuthGuard, AdminGuard)
  - Lazy Loading (loadComponent code-splitting)
  - Navigation Progress Bar & Events
  - Root Singleton Services & provideHttpClient()
  - Reactive Forms, Dynamic FormArray, and Template-Driven Forms
  - Custom Validators (Index number, Campus email domain, Password match, Future dates)
  - Custom Directives (ppHighlight, ppStatusColor)
  - Custom & Built-in Angular Pipes (shortText, statusLabel, 	imeRange, currency, date)
  - Four Screen States Pattern (Loading, Error, Empty, Data)
  - Confirmation Dialogs & Destructive Action Interception
  - Angular Signals (signal, computed, effect) & Reactive State Management

### 2. [Frontend-Documentation.md](./Frontend-Documentation.md)
* **Purpose**: Comprehensive technical inventory of the single-page application frontend.
* **Key Contents**:
  - **Component Inventory**: Exhaustive catalog of all **104 standalone components** with CSS selectors, file paths, inputs, outputs, and functional roles.
  - **Routing Table**: Complete map of all 20 routing modules, paths, route parameters, guards, and lazy-loaded components.
  - **Service Inventory**: Full register of all **27 typed Angular services**, API endpoints called, methods, and managed state.
  - **Forms Register**: Complete audit of Reactive Forms (dynamic controls, FormArrays, validation rules) and Template-driven Forms.

### 3. [Swagger-OpenAPI-Documentation.md](./Swagger-OpenAPI-Documentation.md)
* **Purpose**: Full RESTful API and OpenAPI / Swagger technical specification for the ASP.NET Core 8 backend.
* **Key Contents**:
  - Interactive Swagger UI Guide (http://localhost:5000/swagger) with JWT Bearer security locking.
  - Complete endpoint catalog across all **28 Controllers** and **60+ API endpoints**.
  - Detailed Request DTO schemas, Query/Route parameters, and HTTP response codes (200, 201, 400, 401, 403, 404, 409).
  - Realistic example request and response JSON payloads for each module.

---

## Build & Quality Assurance Status
* **Frontend Angular Build**: 
px ng build --configuration=development &rarr; **0 Errors** (Application bundle verified)
* **Backend .NET Build**: dotnet build --no-restore /t:Compile &rarr; **0 Errors** (Build succeeded)
