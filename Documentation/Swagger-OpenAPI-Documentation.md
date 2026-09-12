# Campus Services Portal - REST API & Swagger / OpenAPI Specification

## Executive Overview
The **Campus Services Portal Backend** is built on **ASP.NET Core 8 Web API**. It provides a fully documented, RESTful, resource-oriented API utilizing standard HTTP verbs (`GET`, `POST`, `PUT`, `DELETE`), standard status codes, and JWT Bearer token authentication.

---

## Interactive Swagger UI Access

The backend provides live interactive OpenAPI documentation through Swashbuckle:
* **Swagger UI URL**: `http://localhost:5000/swagger` or `https://localhost:5001/swagger`
* **Raw OpenAPI Specification**: `http://localhost:5000/swagger/v1/swagger.json`

### Authentication in Swagger UI:
1. Call `POST /api/auth/login` with your credentials.
2. Copy the returned `data.token` string.
3. Click the green **Authorize 🔒** button at the top right of the Swagger UI.
4. Enter: `Bearer <your_token_here>` and click **Authorize**.
5. All authorized endpoints will now automatically receive the JWT in the `Authorization` header.

---

## BRD Section 4 Functional Modules - Complete CRUD Endpoints Traceability Matrix

This table provides direct 1-to-1 evidence that **every CRUD endpoint specified in Section 4 of the BRD is implemented, secured, and returning standard HTTP status codes**.

| BRD Module | BRD Endpoint & HTTP Method | ASP.NET Core Controller | Security & Authorization | Success Codes | Error Codes | BRD Business Rule Enforced |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| **Module 1: Student Profile** | `POST /api/students/register` | `StudentsController.Register` / `AuthController.Register` | Public | `201 Created` | `400, 409` | Format validation; unique email & index; automatic student profile initialization. |
| **Module 1: Student Profile** | `GET /api/students/{id}` | `StudentsController.GetById` | `[Authorize]` (Student own profile / Admin) | `200 OK` | `401, 404` | Returns complete student profile, contact numbers, addresses, and faculty ID. |
| **Module 1: Student Profile** | `PUT /api/students/{id}` | `StudentsController.Update` | `[Authorize]` (Student own profile / Admin) | `200 OK` | `400, 401, 404` | Updates contact details; mobile OTP verification check; FormArray persistence. |
| **Module 1: Student Profile** | `GET /api/students?search=&faculty=` | `StudentsController.GetAll` | `[Authorize(Roles="Admin,SuperAdmin")]` | `200 OK` | `401, 403` | Admin-only searchable and filterable directory with pagination metadata. |
| **Module 1: Student Profile** | `DELETE /api/students/{id}` | `StudentsController.Deactivate` | `[Authorize(Roles="SuperAdmin")]` | `200 OK` | `400, 401, 403, 404` | **Rule 11**: Rejects deactivation with `400` if active hostel, lab, event, certificate, or unpaid fees exist. |
| **Module 2: Hostel Accommodation** | `POST /api/hostel-applications` | `HostelApplicationsController.Create` | `[Authorize(Roles="Student")]` | `201 Created` | `400, 401, 409` | One active application per student; term end date must follow start date. |
| **Module 2: Hostel Accommodation** | `GET /api/hostel-applications/student/{id}` | `HostelApplicationsController.GetByStudent` | `[Authorize]` (Student self / Admin) | `200 OK` | `401, 404` | Returns authenticated student's current and past hostel applications. |
| **Module 2: Hostel Accommodation** | `GET /api/hostel-applications?status=` | `HostelApplicationsController.GetAll` | `[Authorize(Roles="Admin,SuperAdmin")]` | `200 OK` | `401, 403` | Admin review list with status filtering (Pending, Approved, Rejected). |
| **Module 2: Hostel Accommodation** | `PUT /api/hostel-applications/{id}/status` | `HostelApplicationsController.UpdateStatus` | `[Authorize(Roles="Admin,SuperAdmin")]` | `200 OK` | `400, 401, 403, 404` | Admin approval or rejection with reason; triggers automated notification. |
| **Module 2: Hostel Accommodation** | `PUT /api/hostel-applications/{id}/assign-room` | `HostelApplicationsController.AssignRoom` | `[Authorize(Roles="Admin,SuperAdmin")]` | `200 OK` | `400, 401, 403, 404` | Allocates specific room bed after approval; verifies room capacity ceiling. |
| **Module 3: Lab Reservation** | `GET /api/labs` | `LabsController.GetAll` | `[Authorize]` | `200 OK` | `401` | Lists Computer and Science laboratories with layout specifications. |
| **Module 3: Lab Reservation** | `GET /api/labs/{id}/slots?date=` | `LabsController.GetSlots` | `[Authorize]` | `200 OK` | `400, 401, 404` | Time slot list for chosen date; returns real-time slot and seat availability. |
| **Module 3: Lab Reservation** | `POST /api/lab-bookings` | `LabBookingsController.Create` | `[Authorize(Roles="Student")]` | `201 Created` | `400, 401, 409` | **Concurrency Guard**: Rejects with `409 Conflict` if workstation/slot is already taken. |
| **Module 3: Lab Reservation** | `DELETE /api/lab-bookings/{id}` | `LabBookingsController.Cancel` | `[Authorize]` (Owner student / Admin) | `200 OK` | `400, 401, 404` | Student reservation cancellation; immediately frees slot for other students. |
| **Module 3: Lab Reservation** | `GET /api/lab-bookings/student/{id}` | `LabBookingsController.GetByStudent` | `[Authorize]` (Owner student / Admin) | `200 OK` | `401, 404` | Returns student booking history and upcoming active reservations. |
| **Module 4: Event Registration** | `GET /api/events` | `EventsController.GetAll` | `[Authorize]` | `200 OK` | `401` | Lists university events with date, venue, capacity, and registration status. |
| **Module 4: Event Registration** | `POST /api/event-registrations` | `EventsController.Register` | `[Authorize(Roles="Student")]` | `201 Created` | `400, 401, 409` | **Rule**: Blocks duplicate registration (`409 Conflict`); blocks registration if capacity is full (`400`). |
| **Module 4: Event Registration** | `DELETE /api/event-registrations/{id}` | `EventsController.CancelRegistration` | `[Authorize]` (Owner student / Admin) | `200 OK` | `400, 401, 404` | Cancels event pass; decrements registered count and frees event seat. |
| **Module 4: Event Registration** | `GET /api/event-registrations/student/{id}`| `EventsController.GetMyRegistrations` | `[Authorize]` (Owner student / Admin) | `200 OK` | `401, 404` | Lists authenticated student's registered upcoming and past event passes. |
| **Module 5: Complaint Management** | `GET /api/complaint-categories` | `ComplaintCategoriesController.GetAll` | `[Authorize]` | `200 OK` | `401` | Master list of complaint categories (Maintenance, Hostel, Academic). |
| **Module 5: Complaint Management** | `POST /api/complaints` | `ComplaintsController.Create` | `[Authorize(Roles="Student")]` | `201 Created` | `400, 401` | Submits complaint ticket with category, urgency, and description. |
| **Module 5: Complaint Management** | `GET /api/complaints/student/{id}` | `ComplaintsController.GetByStudent` | `[Authorize]` (Owner student / Admin) | `200 OK` | `401, 404` | Lists student's lodged tickets and administrative status replies. |
| **Module 5: Complaint Management** | `GET /api/complaints?status=` | `ComplaintsController.GetAll` | `[Authorize(Roles="Admin,SuperAdmin")]` | `200 OK` | `401, 403` | Admin desk triage view filtered by status (Pending, In Progress, Resolved). |
| **Module 5: Complaint Management** | `PUT /api/complaints/{id}/status` | `ComplaintsController.UpdateStatus` | `[Authorize(Roles="Admin,SuperAdmin")]` | `200 OK` | `400, 401, 403, 404` | Status transition (Pending -> In Progress -> Resolved) with resolution notes. |
| **Module 6: Certificate Requests** | `POST /api/certificate-requests` | `CertificateRequestsController.Create` | `[Authorize(Roles="Student")]` | `201 Created` | `400, 401, 409` | Submits certificate request (Bonafide, Transcript, Completion Letter) with copies (1-5). |
| **Module 6: Certificate Requests** | `GET /api/certificate-requests/student/{id}`| `CertificateRequestsController.GetByStudent` | `[Authorize]` (Owner student / Admin) | `200 OK` | `401, 404` | Lists authenticated student's certificate requests and tracking notes. |
| **Module 6: Certificate Requests** | `GET /api/certificate-requests?status=` | `CertificateRequestsController.GetAll` | `[Authorize(Roles="Admin,SuperAdmin")]` | `200 OK` | `401, 403` | Admin queue of pending requests filterable by status. |
| **Module 6: Certificate Requests** | `PUT /api/certificate-requests/{id}/status` | `CertificateRequestsController.UpdateStatus` | `[Authorize(Roles="Admin,SuperAdmin")]` | `200 OK` | `400, 401, 403, 404` | Approves, issues, or rejects request; sets tracking reference and notifies student. |
| **Module 7: Fee Payment (Simulation)**| `GET /api/fee-payments/student/{id}` | `BillingController.GetMyFees` | `[Authorize]` (Owner student / Admin) | `200 OK` | `401, 404` | Retrieves student fee assignments, paid ledger, and outstanding balances. |
| **Module 7: Fee Payment (Simulation)**| `POST /api/fee-payments/{id}/pay` | `BillingController.PayFee` | `[Authorize(Roles="Student")]` | `200 OK` | `400, 401, 404, 409` | Simulates online payment; generates receipt number; blocks duplicate payments. |
| **Module 7: Fee Payment (Simulation)**| `GET /api/fee-payments/{id}/receipt` | `BillingController.GetReceipt` | `[Authorize]` (Owner student / Admin) | `200 OK` | `401, 404` | Generates official payment receipt verification metadata. |
| **Module 8: Notifications** | `GET /api/notifications/student/{id}` | `InternalNotificationsController.GetMy` | `[Authorize]` (Owner user) | `200 OK` | `401` | Centralized system notifications for status updates across all modules. |
| **Module 8: Notifications** | `PUT /api/notifications/{id}/read` | `InternalNotificationsController.MarkAsRead` | `[Authorize]` (Owner user) | `200 OK` | `401, 404` | Marks individual notification as read; updates unread count. |

---

## Detailed API Endpoints Catalog by Functional Module

### 1. Authentication & Security Controller (`/api/auth`, `/api/password`)

| Method | Endpoint Path | Authorization | Request Body DTO | Response Status Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Public | `StudentRegisterDto` | `201 Created`, `400 Bad Request`, `409 Conflict` | Registers a new student account with index validation |
| `POST` | `/api/auth/login` | Public | `LoginRequestDto` | `200 OK`, `400 Bad Request`, `401 Unauthorized` | Authenticates user credentials and issues JWT token pair |
| `POST` | `/api/auth/refresh-token` | Public | `RefreshTokenRequestDto` | `200 OK`, `400 Bad Request`, `401 Unauthorized` | Issues a new access token using a valid refresh token |
| `POST` | `/api/auth/revoke-token` | Authenticated | `RevokeTokenRequestDto` | `200 OK`, `400 Bad Request` | Revokes the active refresh token on sign-out |
| `POST` | `/api/password/change-password` | Authenticated | `ChangePasswordRequestDto` | `200 OK`, `400 Bad Request` | Updates current user password with historical reuse prevention |
| `POST` | `/api/password/forgot-password` | Public | `ForgotPasswordRequestDto` | `200 OK`, `400 Bad Request` | Generates and emails a secure password reset token |
| `POST` | `/api/password/reset-password` | Public | `ResetPasswordRequestDto` | `200 OK`, `400 Bad Request` | Resets account password using token from email |

**Sample Request (`POST /api/auth/login`)**:
```json
{
  "email": "student@campus.edu",
  "password": "Password123!"
}
```

**Sample Response (`200 OK`)**:
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Authentication successful.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "expiresAt": "2026-09-12T18:00:00Z",
    "user": {
      "id": 14,
      "email": "student@campus.edu",
      "fullName": "Dihini Jayasekara",
      "role": "Student"
    }
  }
}
```

---

### 2. Student Master & Profile Controller (`/api/students`, `/api/studentmaster`)

| Method | Endpoint Path | Authorization | Request Body / Query Params | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/students` | Admin, SuperAdmin | `?search=&facultyId=&page=&pageSize=` | `200 OK` | Retrieves paginated student directory with contact counts |
| `GET` | `/api/students/{id}` | Student (self), Admin | Route parameter: `id` | `200 OK`, `404 Not Found` | Retrieves comprehensive profile including `FacultyId` |
| `PUT` | `/api/students/{id}` | Student (self), Admin | `UpdateStudentProfileDto` | `200 OK`, `400 Bad Request` | Updates contact numbers and addresses via FormArray |
| `DELETE` | `/api/students/{id}` | SuperAdmin | Route parameter: `id` | `200 OK`, `400 Bad Request` | Deactivates student if no active obligations exist (Rule 11) |
| `POST` | `/api/studentmaster/import-csv` | Admin, SuperAdmin | `multipart/form-data` (.csv file) | `200 OK`, `400 Bad Request` | Bulk student intake import with validation error report |

---

### 3. Hostel Management Controller (`/api/hostelmanagment`, `/api/hostel-applications`)

| Method | Endpoint Path | Authorization | Request Body DTO | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/hostelmanagment/hostels` | Authenticated | None | `200 OK` | Lists all hostel buildings with room and bed counts |
| `GET` | `/api/hostelmanagment/rooms` | Authenticated | `?hostelId=` | `200 OK` | Lists rooms with current capacity and vacancy stats |
| `GET` | `/api/hostel-applications` | Admin, SuperAdmin | `?status=&page=&pageSize=` | `200 OK` | Retrieves hostel applications across the campus |
| `GET` | `/api/hostel-applications/my` | Student | None | `200 OK` | Retrieves the authenticated student's hostel application |
| `POST` | `/api/hostel-applications` | Student | `CreateHostelApplicationDto` | `201 Created`, `400`, `409` | Submits a new hostel room allocation application |
| `PUT` | `/api/hostel-applications/{id}/approve` | Admin, SuperAdmin | `HostelAllocationApprovalDto` | `200 OK`, `400 Bad Request` | Allocates a specific room bed and approves application |
| `PUT` | `/api/hostel-applications/{id}/reject` | Admin, SuperAdmin | `RejectApplicationDto` | `200 OK`, `400 Bad Request` | Rejects application with reason and notifies student |

---

### 4. Computer & Science Laboratories Controller (`/api/labs`, `/api/labbookings`)

| Method | Endpoint Path | Authorization | Request Body / Params | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/labs` | Authenticated | None | `200 OK` | Lists laboratories (Computer & Science) |
| `GET` | `/api/labs/{id}/seats` | Authenticated | Route parameter: `id` | `200 OK` | Retrieves 2D matrix workstation seats `(Row, Col)` |
| `POST` | `/api/labs/{id}/seats` | Admin, SuperAdmin | `CreateLabSeatDto` | `201 Created`, `400`, `409` | Registers a physical workstation at cell address |
| `DELETE` | `/api/labs/{id}/seats/{seatId}` | Admin, SuperAdmin | Route params | `200 OK`, `400 Bad Request` | Deactivates a workstation seat |
| `GET` | `/api/labs/{id}/slots` | Authenticated | `?date=YYYY-MM-DD` | `200 OK` | Retrieves time slots and real-time seat availability |
| `POST` | `/api/labbookings` | Student | `CreateLabBookingDto` | `201 Created`, `409 Conflict` | Books or reserves a workstation seat with concurrency check |
| `GET` | `/api/labbookings/my` | Student | None | `200 OK` | Retrieves the authenticated student's lab bookings |
| `DELETE` | `/api/labbookings/{id}` | Student, Admin | Route parameter: `id` | `200 OK`, `400 Bad Request` | Cancels an upcoming lab reservation |

---

### 5. Events & Venues Controller (`/api/events`, `/api/venues`)

| Method | Endpoint Path | Authorization | Request Body DTO | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/events` | Authenticated | `?upcoming=true&search=` | `200 OK` | Lists campus events with capacity and venue info |
| `POST` | `/api/events` | Admin, SuperAdmin | `CreateEventDto` | `201 Created`, `400 Bad Request` | Creates a new campus event with capacity bounds |
| `POST` | `/api/events/{id}/register` | Student | None | `200 OK`, `400`, `409` | Registers student for an event (blocks duplicates & full capacity) |
| `DELETE` | `/api/events/{id}/register` | Student | None | `200 OK`, `400 Bad Request` | Cancels event registration ticket pass |
| `GET` | `/api/events/my-registrations` | Student | None | `200 OK` | Lists authenticated student's registered events |

---

### 6. Certificate Requests Controller (`/api/certificaterequests`, `/api/certificatetypes`)

| Method | Endpoint Path | Authorization | Request Body DTO | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/certificatetypes` | Authenticated | None | `200 OK` | Master list of certificate types (Bonafide, Transcript, etc.) |
| `POST` | `/api/certificaterequests` | Student | `CreateCertificateRequestDto`| `201 Created`, `400 Bad Request` | Submits official document request with purpose and copy count |
| `GET` | `/api/certificaterequests/my` | Student | None | `200 OK` | Lists authenticated student's certificate requests |
| `GET` | `/api/certificaterequests` | Admin, SuperAdmin | `?status=&page=&pageSize=` | `200 OK` | Triage list of pending student certificate requests |
| `PUT` | `/api/certificaterequests/{id}/status` | Admin, SuperAdmin | `UpdateCertificateStatusDto` | `200 OK`, `400 Bad Request` | Approves or issues certificate with optional tracking note |

---

### 7. Complaint Management Controller (`/api/complaints`, `/api/complaintcategories`)

| Method | Endpoint Path | Authorization | Request Body DTO | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/complaintcategories` | Authenticated | None | `200 OK` | Master list of complaint categories |
| `POST` | `/api/complaints` | Student | `CreateComplaintDto` | `201 Created`, `400 Bad Request` | Lodges a support complaint with category and urgency |
| `GET` | `/api/complaints/my` | Student | None | `200 OK` | Lists authenticated student's logged complaints |
| `GET` | `/api/complaints` | Admin, SuperAdmin | `?status=&priority=&page=` | `200 OK` | Admin triage desk for complaints across faculties |
| `PUT` | `/api/complaints/{id}/status` | Admin, SuperAdmin | `UpdateComplaintStatusDto` | `200 OK`, `400 Bad Request` | Updates ticket status (In Progress, Resolved) with feedback |

---

### 8. Billing & Tuition Fee Controller (`/api/billing`, `/api/feetypes`)

| Method | Endpoint Path | Authorization | Request Body DTO | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/feetypes` | Authenticated | None | `200 OK` | Lists active fee types (Tuition, Lab Fine, Hostel Fee) |
| `POST` | `/api/feetypes` | Admin, SuperAdmin | `CreateFeeTypeDto` | `201 Created`, `400 Bad Request` | Creates a new system fee type definition |
| `GET` | `/api/billing/ledger` | Admin, SuperAdmin | `?page=1&pageSize=10` | `200 OK` | Retrieves central fee payment assignment ledger |
| `POST` | `/api/billing/assign` | Admin, SuperAdmin | `AssignFeeRequestDto` | `201 Created`, `200 OK`, `400` | Assigns fee to a single student or executes faculty bulk run |
| `GET` | `/api/billing/my-fees` | Student | None | `200 OK` | Lists fees due and paid for authenticated student |
| `POST` | `/api/billing/pay/{id}` | Student | `PaymentSubmissionDto` | `200 OK`, `400 Bad Request` | Submits online payment and generates receipt |

**Sample Request (`POST /api/billing/assign` - Faculty Bulk Run)**:
```json
{
  "studentId": null,
  "facultyId": 1,
  "feeTypeId": 2,
  "amount": 150.00,
  "billingPeriod": "2026 Semester 1",
  "description": "Semester 1 Computer Lab Maintenance Fee",
  "dueDate": "2026-10-15T00:00:00Z"
}
```

**Sample Response (`201 Created` / `200 OK`)**:
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Bulk assignment run completed successfully. Assigned to 4 student(s).",
  "data": {
    "assignedCount": 4,
    "skippedCount": 0
  }
}
```

---

### 9. Notifications Controller (`/api/internalnotifications`, `/api/notifications`)

| Method | Endpoint Path | Authorization | Request Body / Params | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/internalnotifications/my` | Authenticated | None | `200 OK` | Retrieves real-time notifications for current user |
| `GET` | `/api/internalnotifications/unread-count`| Authenticated | None | `200 OK` | Quick endpoint for navbar badge unread counter |
| `PUT` | `/api/internalnotifications/{id}/read` | Authenticated | Route parameter: `id` | `200 OK` | Marks notification as read |
| `PUT` | `/api/internalnotifications/read-all` | Authenticated | None | `200 OK` | Marks all notifications read for current user |

---

### 10. Audit Logs & System Administration (`/api/auditlogs`, `/api/systemsettings`, `/api/adminmanagement`)

| Method | Endpoint Path | Authorization | Request Body DTO | Response Status | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/auditlogs` | Admin, SuperAdmin | `?entityName=&action=&from=&to=` | `200 OK` | Searchable immutable audit trail with before/after state |
| `GET` | `/api/systemsettings` | Authenticated | None | `200 OK` | Retrieves global portal configuration settings |
| `PUT` | `/api/systemsettings` | SuperAdmin | `UpdateSystemSettingsDto` | `200 OK`, `400 Bad Request` | Updates global pagination, academic year, and flags |
| `GET` | `/api/adminmanagement` | SuperAdmin | None | `200 OK` | Lists administrator accounts with active/locked status |
| `POST` | `/api/adminmanagement` | SuperAdmin | `CreateAdminRequestDto` | `201 Created`, `400 Bad Request` | Provisions a new Administrator or SuperAdmin user |
| `PUT` | `/api/adminmanagement/{id}/status` | SuperAdmin | `ToggleAdminStatusDto` | `200 OK`, `400 Bad Request` | Activates or deactivates administrator credentials |
