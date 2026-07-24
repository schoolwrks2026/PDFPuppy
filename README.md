# PDFPuppy Templates & AI Standards

Welcome to the **PDFPuppy** base repository! This repository acts as a blueprint and system standard template for building highly robust, enterprise-grade, secure, and performant web solutions.

## 🚀 Purpose of this Repository

This repository contains:
1. **`AGENTS.md`**: The master AI rulebook. Any AI coding agent (like Jules, Devin, or others) working on this or future derived projects will automatically read and strictly adhere to these top-tier engineering, architecture, database, security, and UI standards.
2. **Template Structure**: A solid foundation ready to scale under Clean Architecture, DDD (lightweight), SOLID principles, and complete modularity.

---

## 🛠️ How to Use This in New Projects

### The Short Way
When starting a new project, simply copy the **`AGENTS.md`** file from this repository into the root directory of your new project.

When invoking or instructing an AI agent to build features, you only need to tell it:
> *"Implement this following the standards in AGENTS.md"*

Or simply:
> *"Refer to AGENTS.md"*

The AI agent will automatically parse the file and apply all strict requirements (Clean Architecture, Secure by Default, Mobile First UI, Database Standards, Audit Trails, and more) to all produced code.

---

## 📌 Master Standards Overview (from `AGENTS.md`)

* **Engineering Philosophy**: Simplicity, Security, Scalability, Maintainability, Performance, and User Experience first. No hardcoded business rules.
* **Architecture**: Clean Architecture, SOLID, DRY, KISS, Service Layer, Repository Pattern, and lightweight Domain-Driven Design (DDD).
* **Database standard**: All entities contain standard metadata (UUID, TenantId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, Version). Support soft delete, optimistic locking, and full isolation.
* **Security standard**: Secure-by-default with RBAC, Tenant Isolation, JWT + Refresh tokens, CSRF & XSS prevention, Input Validation, Output Encoding, Audit Trails, and PII protection.
* **UI standard**: Mobile first, accessible (WCAG AA), responsive, minimal clicks, with standard views for Loading, Empty, Error, Validation, and Success.
* **API standard**: Documented endpoints with input/output validation, sorting, filtering, pagination, search, and idempotency.

---

## 📄 License

This standard template is private and configurable for production enterprise environments.
