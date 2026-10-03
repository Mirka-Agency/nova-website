# CMS Features & Modules Roadmap

Track implementation one item at a time. Update the **Status** column when a row is finished (`Done` / `Not Done`).

Suggested order = dependency order (foundations first, then modules).

---

## Core platform

| Order | Feature | Module / Area | Notes | Status |
|------:|---------|---------------|-------|--------|
| 1 | Solution scaffold (Clean Architecture + modules) | Core | Projects, references, DI hooks | Done |
| 2 | Admin shell (RTL, Farsi, design system layout) | Admin | Dashboard + sidebar + admin CSS | Done |
| 3 | Feature localization (`fa-IR`) | Core | Culture wired in `Program.cs` | Done |
| 4 | Feature Management toggles | Core | `Blog` / `Shop` / `Forms` in `appsettings` | Done |
| 5 | Auth policies & roles (constants) | Core | Policy names defined; not enforced yet | Done |
| 6 | ASP.NET Core Identity | Users / Auth | Cookie auth, register/login, password hashing | Done |
| 7 | Admin `[Authorize]` + login UI (Farsi) | Admin / Auth | Protect Area; seed default Admin | Done |
| 8 | Users & roles management (Admin CRUD) | Users | Assign `Admin`, `Editor`, `ShopManager`, `Viewer` | Done |
| 9 | Shared EF Core hosting / connection string | Infrastructure | PostgreSQL single DB (`DefaultConnection`) for Identity + all modules | Done |
| 10 | Global exception middleware | Core | Domain / validation / not-found handling | Done |
| 11 | Structured logging (Serilog) | Core | Errors, important actions, security events | Done |
| 12 | Antiforgery + security hardening | Core | Rate limiting, HTTPS, input validation norms | Done |
| 13 | Resource-based localization (`.resx`) | Admin | Replace hardcoded Persian where needed | Done |
| 14 | Feature on/off Admin UI | Admin | Enable/disable Blog, Shop, Forms from panel | Done |

---

## Blog module

| Order | Feature | Module / Area | Notes | Status |
|------:|---------|---------------|-------|--------|
| 15 | Blog module vertical slice (DbContext, DI, Admin nav) | Blog | Replace stub `PostsController` | Done |
| 16 | Posts CRUD (draft / publish) | Blog | Title, slug, body, status, timestamps | Done |
| 17 | Categories & tags | Blog | Many-to-many tags optional | Done |
| 18 | Post media / cover image | Blog | Upload to S3 (`IObjectStorage`) or paste URL | Done |
| 19 | Blog public listing & detail | Public site | Theme later; API or MVC views when ready | Done |

---

## Forms module

| Order | Feature | Module / Area | Notes | Status |
|------:|---------|---------------|-------|--------|
| 20 | Forms module vertical slice | Forms | DbContext, Admin UI, feature gate | Done |
| 21 | Form builder (fields definition) | Forms | Text, email, phone, textarea, select, checkbox, radio, date, captcha, file + templates; DnD reorder | Done |
| 22 | Form submissions list & detail | Forms | Admin inbox; filters; read/archive; CSV export; email notify + auto-reply | Done |
| 23 | Public form render & submit | Public site | Validation, honeypot/captcha, success message/redirect, FormEmbed VC | Done |
| 23b | ExpandFormsModule | Forms | Status enum, settings columns, submission status, FieldId on values | Done |
| 23c | Form Engine Phase 1 | Forms | Form.Key, FormVersions + SchemaJson dual-write, Submission.FormVersionId; legacy columns kept | Done |
| 23d | Form Engine Phase 2 — Field type registry | Forms | Handler registry, expanded types, layout fields, DuplicateField, schema/admin/public wiring | Done |
| 23e | Form Engine Phase 3 — Submissions data/files/context | Forms | DataJson/ContextJson, SubmissionFiles, status New/Processed/Spam, public context capture | Done |
| 23f | Form Engine Phase 4 — Actions + submitBehavior | Forms | Action handlers (email/auto_reply/webhook), template vars, submitBehavior message/redirect/page | Done |
| 23g | Form Engine Phase 5 — AntiSpam | Forms | Provider pipeline (honeypot/simple_captcha/turnstile/recaptcha/hcaptcha), schema settings, admin+public wiring | Done |
| 23h | Form Engine Phase 6 — Admin tabs | Forms | Edit UI tabs: General/Fields/After submit/Actions/Security/Advanced/Responses | Done |
| 23i | Form Engine Phase 7 — Public schema contract | Forms | FormPublicContract JSON API, type-id rendering, theme-safe (no secrets) | Done |
| 23j | Form Engine Phase 8 — Cleanup/docs/tests | Forms | Schema-first writes; dead GetPublished removed; Values dual-write stopped; README | Done |
| 23k | Form Engine — Drop legacy Form settings columns | Forms | SchemaJson only for submit/actions/antiSpam/settings; FormFields + FormVersions kept | Done |
| 23l | Drop SubmissionValues table | Forms | DataJson-only field store; SQL backfill then drop `forms.SubmissionValues` | Done |
| 23m | Form field conditional visibility | Forms | Schema visibility evaluator; skip hidden required/persist; public JS + admin FieldForm | Done |
| 23n | Forms completion — page path, captcha widgets, multi visibility | Forms | `page` = relative path redirect; Turnstile/reCAPTCHA/hCaptcha widgets; multi-condition admin; `GET …/by-id/{id}/schema` | Done |
| 23o | Forms extras — webhook HMAC, version history, option value≠label | Forms | Rich webhook payload + HMAC; publish snapshots + restore draft; `value:label` options | Done |

---

## Shop module

| Order | Feature | Module / Area | Notes | Status |
|------:|---------|---------------|-------|--------|
| 24 | Shop module vertical slice | Shop | DbContext, Admin UI, feature gate | Done |
| 25 | Shop mode: Catalog Only | Shop | Products without cart/checkout | Done |
| 26 | Categories & products CRUD | Shop | Pricing, stock flags as needed per mode | Done |
| 27 | Shop mode: Online Store | Shop | Cart, checkout, orders | Done |
| 28 | Shop mode: Hybrid | Shop | Catalog + selective ecommerce; keep modes separate | Done |
| 29 | Orders Admin | Shop | List, status changes | Done |
| 30 | Payment service (separate) | Payments | Gateway abstraction; not mixed into catalog | Done |
| 31 | Shop public catalog / product pages | Public site | Theme later | Done |
| 32 | Product brands, tags, attributes | Shop | Catalog metadata | Done |
| 33 | Product variations & gallery | Shop | Variable products, images | Done |
| 34 | Inventory (warehouses, stock, movements) | Shop | Multi-warehouse stock | Removed — product/variation stock fields only |
| 35 | Customer groups & price rules | Shop | Wholesale/retail pricing | Done |
| 36 | Wholesale requests (admin + public apply) | Shop | B2B onboarding | Done |
| 37 | Coupons & shipping methods | Shop | Cart discounts, shipping quotes | Done |
| 38 | Product reviews (public submit, admin moderate) | Shop | Ratings & moderation | Done |
| 39 | Commerce settings tab | Shop | Payment/shipping/reviews toggles | Done |
| 40 | Commerce reports | Shop | Sales, best sellers, low stock | Done |
| 41 | ExpandCommerceModule EF migration | Shop | Status remap, new tables/columns | Done |

---

## Cross-cutting / later

| Order | Feature | Module / Area | Notes | Status |
|------:|---------|---------------|-------|--------|
| 42 | Media library (shared) | Core / Media | Uses shared S3 `IObjectStorage`; Blog & Shop | Done |
| 43 | Settings / site configuration | Core | Brand, contact, SEO/OG, social, analytics, maintenance mode | Done |
| 44 | Public website template system | Public site | Default public skin (SiteSettings-driven); multi-theme later if customer needs it | Done |
| 45 | REST API versioning (if needed) | API | `api/v1/...` + ProblemDetails on Blog admin API; Asp.Versioning when a second client appears | Done |
| 46 | Application tests for critical use cases | Tests | Per module as features land | Done |
| 47 | Integration / smoke tests | Tests | Auth, feature flags, module boundaries | Done |

---

## Production hardening (post-roadmap)

| Order | Feature | Module / Area | Notes | Status |
|------:|---------|---------------|-------|--------|
| 38 | Admin dashboard stats + quick links | Admin / Core | Feature/policy-gated cards with counts | Done |
| 39 | Responsive admin nav (drawer) | Admin / Core | Mobile off-canvas sidebar + flash dismiss | Done |
| 40 | Feature toggle confirmation | Admin / Core | Confirm before enable/disable modules | Done |
| 41 | Admin list pagination + search | Core + modules | Shared `PagedResult`; Posts/Products/Orders/Media/Users | Done |
| 42 | Admin empty states + validation affordances | Admin / Core | Empty partial, invalid input borders, stronger active nav | Done |
| 43 | Cookie API CSRF / token auth strategy | Core + Blog API | Same-origin cookie + antiforgery on `api/v1` JSON endpoints | Done |
| 44 | Distributed cache + Data Protection keys | Core / Ops | Optional Redis via `Cache:RedisConnectionString` | Done |
| 45 | Upload magic-byte sniffing | Core storage | MIME spoof hardening for images and form files | Done |
| 46 | Viewer role read-only policies | Core Auth | View* policies + mutation filter; Viewer can browse admin lists | Done |
| 47 | SEO / menus / audit / search | Core CMS | Per-post SEO meta, configurable nav, audit log, admin search | Done |

---

## Suggested implementation sequence (summary)

1. **Identity + Admin lock-down** (rows 6–8)  
2. **EF + exceptions + logging + security + feature UI** (rows 9–14)  
3. **Blog** Admin + public listing/detail done (rows 15–19); full public theme later (34)  
4. **Forms** Admin + public submit + Form Engine done (rows 20–23o)  
5. **Shop** Catalog / Online Store / Hybrid + payments + public catalog done (rows 24–31)  
6. **Media library + site settings** done (rows 32–33); **default public skin** done (34)  
7. **Application / integration tests** done (36–37); **API v1** light versioning done (35). Roadmap rows 1–37 complete.  
8. **Admin UX critical** done (38–42). **Production hardening** done (43–47). Roadmap complete.

---

## How to use

- Implement **one row at a time**.  
- When finished, change `Not Done` → `Done`.  
- Prefer completing a whole vertical slice (Domain → Application → Infrastructure → Admin UI) before starting the next feature.
