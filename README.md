# Digital Library Management System

A microservices-based library management API: patrons browse a book catalog, reserve and check out books, and librarians process checkouts/returns. Built with ASP.NET Core, EF Core, and PostgreSQL, deployed on AWS Elastic Beanstalk + RDS.

## Architecture

Three independent services, each with its own PostgreSQL database:

| Service | Responsibility |
|---|---|
| UserService | Registration, login (JWT), profiles |
| CatalogService | Book catalog, search, availability |
| ReservationService | Reservations, checkout/return, waitlist, background expiry job |

## Live URLs

Base: `http://library-microservices-env-1.eba-wsmmnt5m.us-east-1.elasticbeanstalk.com`

| Service | Swagger | Health |
|---|---|---|
| UserService | `/swagger/index.html` | `/health` |
| CatalogService | `/catalog/swagger/index.html` | `/catalog/health` |
| ReservationService | `/reservations/swagger/index.html` | `/reservations/health` |

All API routes follow the same prefixing (e.g. `/catalog/api/catalog/books`, `/reservations/api/reservations`); UserService sits at the root with no prefix.

## Deviations from the original spec

- **`POST /api/catalog/books`** — added beyond the original 13-endpoint contract. The spec never defines a way to create a book, but one is needed to populate the catalog in production (no other write path exists). Public, unauthenticated, matching CatalogService's existing no-auth pattern.

## Seeded accounts

There is no API path to create a Librarian — registration always defaults to `PATRON`, by design. To make Librarian-only endpoints (checkout/return) testable at all, a Librarian account has to come from somewhere outside the normal registration flow:

Credentials for librarian user (for testing purposes):
user email: test@example.com
password: SecurePass123!

## Running the test suite

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
