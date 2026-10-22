# DentalClinic — Business Review & Improvement Plan

**From a strong clinic back-office to a sellable clinic operating system**

| Field | Value |
|---|---|
| Date | 2026-10-22 |
| Baseline | `main` @ `95e6613` + in-progress `Dockerfile` / `docker-compose.yml` changes |
| Scope | Product, business model, architecture, delivery plan |
| Status | Proposal — decisions marked in §20 are required before Phase 0 starts |
| Companion docs | [`AUDIT.md`](./AUDIT.md) (technical defect inventory), [`docs/product/PRD.md`](./docs/product/PRD.md) (older aspirational PRD), [`docs/product/MVP-ROADMAP.md`](./docs/product/MVP-ROADMAP.md) |

> **How to read this document.** Part I is the business-owner view: who buys this, what they
> actually pay for, what is missing, and what not to build. Part II is the solution-architect
> view: the target architecture and a step-by-step, testable feature plan. Where this plan and
> the older PRD disagree, **the code and this plan win** — the PRD was written against commit
> `1369f0a` and several of its findings have since been fixed.

---

## 0. Executive verdict

**The engineering is ahead of the product.** This is not a beginner's CRUD app: it has a real
domain model, CQRS slices, a `Result<T>` flow, two-layer authorization, serializable
transactions for the two operations that most often corrupt dental data (double-booking and
double-payment), hashed rotating refresh tokens, soft delete, audit fields, Testcontainers
integration tests, and working Docker/observability for the API. That foundation is genuinely
valuable and should **not** be rewritten.

**But it is not yet sellable to a clinic without hand-holding.** A paying clinic judges the
product on five things, and the current build only fully covers one of them:

| What the clinic judges | Current state | Verdict |
|---|---|---|
| Can the receptionist run the day faster than paper/WhatsApp? | Table + modals, no calendar, no reminders, no waitlist | **Partial** |
| Can the dentist record real clinical work? | Treatment log with FDI tooth, but no odontogram, plans, images, or clinical status | **Partial** |
| Can the owner trust the money? | Strong invoice/payment core, but no invoice number, print, patient ledger, payment method, or trustworthy dashboard | **Partial** |
| Can the owner trust the system with patient data? | Good auth baseline, but no user administration, audit log, or backup/restore | **Weak** |
| Can a second clinic be onboarded without a rewrite? | Single-clinic schema; no tenant seam; no import | **Missing** |

**The gap is product completeness and operational trust — not code quality.** That is a much
better problem to have.

### The five things that matter most

1. **Trust before features.** User administration, audit trail, backup/restore, and a real
   deployment path are what let a clinic hand over its patient and financial data. They are
   not "nice to have".
2. **The front desk is the adoption gate.** If booking, rescheduling, check-in and checkout are
   slower than the current notebook/WhatsApp routine, the clinic will abandon the software no
   matter how good the architecture is.
3. **No-shows and recalls are the biggest revenue levers.** A reminder and recall system is
   cheaper to build than an imaging module and pays for itself faster.
4. **The owner is the buyer.** The owner needs daily cash, outstanding debt, revenue by doctor,
   and commission — not a dashboard that sums the first 100 rows on the client.
5. **The clinical record is the retention moat.** Odontogram + treatment plans + images are what
   make the system hard to leave after six months. Build them after the front desk and money
   basics, not before.

### The next 90 days (recommended)

1. Make the Web app deployable and keep users signed in across a refresh.
2. Add staff administration and the doctor–user link (today a UI-created doctor can silently
   see nothing).
3. Add clinic settings, invoice numbering, printable invoice/receipt, and audit log.
4. Add the day/week schedule board and WhatsApp confirmation/reminder links.
5. Rehearse a backup restore, onboard one pilot clinic, and measure no-show and collection rates.

### What not to do

- Do not build AI, insurance, inventory, a patient mobile app, or full accounting before the
  first clinic pays and renews.
- Do not add microservices, Redis, Kubernetes, or a separate reporting database. The current
  modular monolith is the right shape for the next 12–18 months.
- Do not build self-service multi-tenant onboarding until at least three clinics are paying.
  Add the tenancy **seam** now; complete the platform later.

> **If the goal is a portfolio/showcase project rather than a commercial product,** do Phase 0
> items F-01, F-02, F-05, F-06, F-10, F-11, F-19 and F-20, add realistic demo data (F-09), and
> skip the tenancy/FX/commission work. The result will demo far better than a half-built SaaS.

---

# Part I — Business Owner's Analysis

## 1. Where the product stands

### 1.1 What is genuinely good

- **The core visit-to-cash flow exists.** Appointment → complete with treatments → draft invoice
  → issue → partial payment is implemented end-to-end, including idempotent draft generation and
  a serializable payment path with a row-version guard. Most small-clinic systems get this wrong.
- **The domain model is real.** Money is `decimal(18,2)`, invoices have lifecycle and payment
  state, treatments use FDI numbering, and business failures return `Result<T>` instead of
  exceptions.
- **Authorization is taken seriously.** Controllers carry policies and handlers re-check
  ownership; invoice mutations were hardened after the audit. Cache keys include scope, which is
  the correct (and commonly missed) design.
- **The tests are meaningful.** 483 test attributes across five projects; 174 pure unit tests
  pass locally (113 domain + 61 application) without Docker. Integration suites use Testcontainers
  and a migration-replay test.
- **Arabic/RTL is a first-class UI concern**, not a bolt-on. That is a real differentiator in the
  target market.
- **Observability exists.** Serilog/Seq, OpenTelemetry/Prometheus, health endpoints, and a
  no-show background job are wired.

### 1.2 The honest gap

The product currently behaves like a **clinic back-office v1**:

- It records what happened, but it does not **drive the day** (no calendar, confirmations,
  reminders, waitlist, recall).
- It stores clinical notes, but it does not **support clinical decisions** (no odontogram state,
  treatment plans, images, structured alerts).
- It can produce an invoice, but it cannot **hand the patient a numbered, printed invoice** or
  tell the owner **who owes what across all invoices**.
- It has roles, but no way to **create or deactivate a staff account**.
- It has audit fields on rows, but no **audit trail of sensitive actions**, and no tested backup.
- Its dashboard **looks** like an owner dashboard but is computed client-side over the first
  page of data, so it is wrong as soon as the clinic has real volume.

That last point is important: the current dashboard is more dangerous than no dashboard, because
it produces plausible-looking numbers that the owner may act on.

### 1.3 Capability scorecard

Ratings are relative to what a 1–5 chair clinic needs, not to enterprise software.

| Capability | Score | What exists | What is missing |
|---|---:|---|---|
| Patient registry | 4/5 | CRUD, search, medical flags, soft delete | Duplicate detection, merge, recall, alerts everywhere, referral source |
| Scheduling | 3/5 | Create/reschedule/cancel/no-show, overlap guard | Calendar/day view, working hours, doctor availability, confirmations, reminders, waitlist |
| Clinical records | 2/5 | Treatment log, FDI tooth, free text, cost | Odontogram state, treatment plans, performed/planned status, surfaces, images, perio |
| Billing | 3/5 | Draft → issue → payment, line items, concurrency | Invoice number, print, void/credit, discounts/tax hooks, service traceability |
| Payments | 2.5/5 | Partial payments, row-version concurrency | Payment method, receipt, refund, installment plans |
| Reporting | 1.5/5 | Dashboard cards, client-side sums | Server-side aggregates, date ranges, revenue by doctor/service, no-show rate, cash close |
| Administration | 3/5 | Roles, policies, ownership checks | User CRUD, role management, doctor–user link, audit log, session lock |
| Operations | 2.5/5 | API Docker, health checks, Seq/Prometheus | Web deployment, HTTPS, backups/restore, migration job, production secrets, alerts |
| Localization/UX | 3.5/5 | Arabic/English, RTL, dark mode, custom CSS | Resource files, persisted preferences, localized dates/numbers, print styles |
| Architecture/tests | 4.5/5 | Clean layers, CQRS, Testcontainers, migration test | Web tests, E2E, authz matrix, no tenant seam, some dead config |

**Weighted product score: ~2.7/5. Engineering score: ~4.4/5.** Closing that gap is the whole job.

### 1.4 Evidence note

This review inspected the code, not just the README. The two pure unit-test projects pass
(113 + 61). The Docker-dependent suites were not run in this session. A full `dotnet build` could
not finish because the running API/Web processes held their output DLLs; no compile errors were
reported before the file-lock failures. The baseline is **not warning-clean** (93 warnings,
including a high-severity `System.Security.Cryptography.Pkcs` 9.0.11 advisory), and the README's
"380/380 tests" and port claims are stale. These are hygiene issues, not architectural ones.

---

## 2. The customer and the money

### 2.1 Ideal customer profile (ICP)

Start narrower than the PRD suggests:

- **1–5 chair independent dental clinic**, one location, 1–4 dentists, 1–3 receptionists.
- **Arabic-speaking market**, cash-heavy, WhatsApp-first, USD and local currency both in use.
- Owner-dentist is involved in daily operations and signs the cheque.
- Currently uses paper appointment books, Excel, WhatsApp, and a legacy desktop billing tool.
- Pain: no-shows, forgotten recalls, unclear patient debt, slow checkout, no reliable daily
  revenue view, fear of losing patient records.

Do **not** start with hospitals, multi-branch groups, or insurance-heavy practices. Their
requirements (claims, pre-authorization, general ledger, HL7/FHIR) will pull the product apart.

### 2.2 Buyer vs. users

| Role | What they care about | What makes them reject the product |
|---|---|---|
| **Owner-dentist (buyer)** | Daily cash, outstanding debt, revenue per doctor, control, less admin work | "I can't see if I made money today"; "the numbers don't match" |
| **Receptionist (daily user)** | Speed, fewer clicks, reminders, easy rescheduling, no double-booking | "This is slower than the notebook" |
| **Dentist (clinical user)** | Fast charting, tooth history, treatment plans, images, alerts | "I have to type everything twice" |
| **Accountant/bookkeeper (occasional)** | Exports, invoice numbers, payment methods, reconciliations | "I can't reconcile this with the bank/cash" |
| **Patient (indirect)** | Clear estimate, reminder, receipt, easy rescheduling | Confusing messages, surprise bills |

The receptionist is the adoption gate. The owner is the buying gate. The dentist is the
retention gate. Design for all three, but ship for the receptionist first.

### 2.3 Jobs to be done

1. **"Fill the chair."** Book, confirm, remind, fill cancellations, recall overdue patients.
2. **"Get the patient from the door to paid without re-entering data."** One visit record drives
   the clinical note, the invoice, and the payment.
3. **"Show me the money."** Today's cash, outstanding balances, revenue by doctor/service,
   commission, expenses.
4. **"Show the patient a plan and get a yes."** Clear estimate, accepted plan, scheduled sessions.
5. **"Keep the record safe and complete."** Tooth history, images, alerts, audit trail, backup.
6. **"Onboard a new receptionist in a day."** Simple screens, Arabic labels, obvious next action.

### 2.4 What the owner actually pays for

Not Clean Architecture. The owner pays for:

- **Recovered revenue** — fewer no-shows, more recalls, more accepted treatment plans.
- **Collected revenue** — a ledger that shows who owes what and makes it easy to collect.
- **Time saved** — receptionist hours, less re-typing, faster checkout.
- **Control** — roles, audit, reports, and the ability to fire a staff member without losing data.
- **Peace of mind** — backups, security, and a support number that answers.

Price against those outcomes, not against feature count.

### 2.5 Illustrative ROI (validate with the pilot, do not promise)

Assume one clinic with 8 appointments/day, 24 working days/month, average ticket $30, and a 15%
no-show rate:

- Missed slots: ~1.2/day → ~29/month → **~$870/month of lost chair time**.
- A reminder + confirmation workflow that cuts no-shows by one third recovers **~$290/month**.
- A recall list that brings back 10 patients/month at $50 recovers **~$500/month**.
- Better collection of existing debt is pure cash recovery, with no extra clinical work.

At a $149/month subscription, the product only needs to recover **one extra appointment per
week** to pay for itself. That is the sales story; the pilot must prove it with before/after data.

---

## 3. What to add, ranked by business value

### 3.1 P0 — Trust and pilot blockers (do first)

These are not differentiators. They are the minimum bar for a clinic to trust the system with
real patients and money.

| # | Add | Why it is P0 |
|---|---|---|
| 1 | **Staff & user administration** + doctor–user link | Without it, the owner cannot onboard a receptionist or link a new dentist; a UI-created doctor currently gets no `doctor_id` claim and silently sees nothing |
| 2 | **Session resilience** | A browser refresh currently logs the user out (in-memory Blazor session); unacceptable at a front desk |
| 3 | **Invoice numbering + printable invoice/receipt** | A clinic cannot hand a patient a `#A1B2C3D4`; numbering and print are table stakes |
| 4 | **Audit log** | Owner needs to know who issued, voided, discounted, or deleted what |
| 5 | **Backup + tested restore** | A single lost database ends the business relationship |
| 6 | **Clinic profile/settings** | Branding, timezone, currency, logo, invoice prefix; also removes hardcoded "Al Noor Dental" |
| 7 | **Tenancy seam** | Add `ClinicId` and filters before real data accumulates; full multi-tenant UI can wait |
| 8 | **One-command deployment** | The Web app is not containerized correctly; support cost explodes without a repeatable install |

### 3.2 P1 — Revenue protection and front-desk speed

| # | Add | Business outcome |
|---|---|---|
| 9 | **Day/week schedule board** | Receptionist runs the day from one screen; fewer mistakes |
| 10 | **Working hours, holidays, doctor availability** | Prevents bookings outside real availability; enables slot suggestions |
| 11 | **Confirmation + WhatsApp reminder links** | Direct no-show reduction; the highest-ROI feature in the list |
| 12 | **Recall / reactivation list** | Brings back overdue patients; recurring revenue with no acquisition cost |
| 13 | **Waitlist / cancellation fill** | Turns a cancelled slot into a booked one |
| 14 | **Fast patient intake + duplicate detection/merge** | Prevents duplicate records, the most common data-quality failure |
| 15 | **Service catalog enrichment** (duration, category, tooth-specific, lab) | Better booking, better reporting, fewer manual price edits |
| 16 | **Persisted preferences + resource-based localization** | The UI stops resetting on refresh; Arabic becomes maintainable |

### 3.3 P2 — Clinical differentiation

| # | Add | Business outcome |
|---|---|---|
| 17 | **Odontogram** (per-tooth state + history + primary teeth) | The centrepiece of a dental record; makes the system hard to leave |
| 18 | **Treatment plans + printable estimates** | Increases case acceptance and average treatment value |
| 19 | **Performed vs. planned treatment status** | Correct billing; only performed work is invoiced |
| 20 | **Medical alerts everywhere** | Safety and professionalism; the data already exists but is hidden on one tab |
| 21 | **Clinical images / X-rays** | Completes the clinical file; differentiator against text-only systems |

### 3.4 P3 — Owner money and scale

| # | Add | Business outcome |
|---|---|---|
| 22 | **Patient ledger, aging, statement** | Collect outstanding debt; stop client-side balance math |
| 23 | **Payment methods, receipts, refunds, void** | Reconcile cash/card/bank; handle mistakes without corrupting history |
| 24 | **Installment plans** | Matches how many clinics actually collect for larger treatments |
| 25 | **Dual currency + historical FX** | Essential in a volatile-currency market; keeps old invoices reproducible |
| 26 | **Owner dashboard & reports** | Server-side truth for daily cash, revenue, no-shows, doctor production |
| 27 | **Doctor commission & payout report** | Removes a monthly spreadsheet ritual; a real owner pain |
| 28 | **Daily cash close & expenses** | The receptionist can close the drawer; the owner sees net cash |
| 29 | **Lab case tracking** | Crowns/dentures are a real workflow with expected-return dates and costs |
| 30 | **Multi-clinic platform + import/export** | Only after the single-clinic product is proven |

### 3.5 What not to build yet (and why)

| Not now | Reason |
|---|---|
| **AI X-ray diagnosis** | Regulatory, liability, and data-volume heavy; not a pilot need |
| **Insurance claims / pre-authorization** | Market-specific and complex; validate demand first |
| **Full accounting / general ledger / payroll** | Integrate/export to an accountant instead; don't become an ERP |
| **Inventory / sterilization logs** | Real but separate workflow; revisit after 10 paying clinics |
| **Patient mobile app / portal** | Web booking link is enough for the first year; a native app is a cost sink |
| **Online self-service signup** | Sell and onboard manually until the playbook is proven |
| **Microservices / Redis / Kubernetes** | The modular monolith is correct for this scale; complexity would slow delivery |
| **Multi-branch analytics** | Requires tenant + branch modeling; defer until a customer asks and pays |
| **E-invoicing / tax engine** | Legal requirements vary; design schema hooks, implement after legal review |
| **SMS/email marketing automation** | WhatsApp click-to-chat covers the pilot; add providers when volume justifies |

---

## 4. Business model & go-to-market

### 4.1 Packaging and pricing hypothesis

Validate with 5–10 clinics before publishing. The shape matters more than the exact numbers.

| Package | Target | Includes | Indicative price |
|---|---|---|---|
| **Starter** | 1–2 chair clinic | Patients, appointments, billing, payments, basic reports, 3 users | $99–$149 / month |
| **Practice** | 2–5 chair clinic | Starter + odontogram, treatment plans, reminders, patient ledger, 6 users | $199–$249 / month |
| **Owner** | Multi-doctor / multi-branch | Practice + commission, advanced reports, cash close, lab, unlimited users | $349+ / month |
| **Onboarding** | All | Data import, clinic setup, 2-hour training, 30-day support | $300–$800 one-time |
| **WhatsApp/SMS** | Optional | Message delivery at cost + small margin | Pass-through |

Rules:

- Charge **per clinic**, not per appointment or per invoice. Transaction fees make clinics
  distrust the system.
- Include onboarding in the first-year annual plan; it improves activation.
- Offer annual prepay at a 10–15% discount to improve cash flow.
- Do not compete on being the cheapest. Compete on Arabic-first fit, owner control, and support.

### 4.2 Pilot offer

- 2–3 clinics, 60 days free, then 50% off for 6 months in exchange for a case study and a
  testimonial.
- One champion per clinic (usually the owner or senior receptionist).
- Baseline the clinic's current no-show rate, daily patient count, and collection rate for two
  weeks **before** go-live, so the pilot can prove impact.
- Run parallel with paper for one week, then switch. Do not run parallel for a month; it kills
  adoption.

### 4.3 Sales and onboarding

**Sales motion (founder-led):**

1. 20-minute demo using a realistic demo clinic, not an empty database.
2. Import 20–50 of their real patients during the demo; show their own names and appointments.
3. Show three screens only: today's schedule, a patient's tooth history/plan, and the owner's
   daily cash. Do not tour the settings page.
4. Close with a fixed onboarding fee and a start date.

**Onboarding (target: 48 hours):**

- Day 1: clinic profile, users/roles, services/prices, working hours, import patients.
- Day 2: 2-hour receptionist training, 1-hour dentist training, first live appointment, backup
  restore proof.
- Week 1: daily 15-minute check-in; fix friction immediately.
- Week 4: review metrics; decide whether to continue.

**Support model for the pilot:**

- WhatsApp group with the owner and receptionist; 4-business-hour response target.
- Remote session for anything that cannot be solved in chat.
- In-app "Help" link with a short Arabic guide and a support phone number.
- Weekly office hours for all pilot clinics.

### 4.4 Pilot success metrics and go/no-go

Track weekly; the owner should see the same numbers.

| Metric | Target | Why |
|---|---|---|
| Appointments recorded in the system | ≥ 80% of all appointments | Adoption |
| Invoices issued through the system | ≥ 90% of billable visits | Money flows through the product |
| No-show rate | ≥ 20% relative reduction vs. baseline | The core ROI story |
| Collection rate (collected / invoiced) | ≥ 95% | The ledger is working |
| Average checkout time | ≤ 5 minutes | Receptionist speed |
| Owner dashboard usage | ≥ 3 sessions/week | The buyer sees value |
| Critical data-loss incidents | 0 | Trust |
| Would recommend (informal NPS) | ≥ 8/10 | Willingness to pay/refer |

**Go/no-go after 8–12 weeks:** if a clinic is not recording ≥ 80% of appointments or the
receptionist has reverted to paper, stop adding features and fix the workflow. More features will
not save a product that is slower than the status quo.

### 4.5 Competitive position

| Competitor type | Their strength | Their weakness | Our angle |
|---|---|---|---|
| Paper / Excel / WhatsApp (status quo) | Free, familiar, fast for small volume | No history, no reports, easy to lose, no reminders | Arabic-first, faster than paper, one-click WhatsApp, owner reports |
| Legacy local desktop systems | Cheap, familiar, sometimes Arabic | Old UX, weak reporting, hard to back up, single PC | Modern UX, web access, backup, reports, no install |
| International cloud suites (Dentrix, Curve, Open Dental) | Deep features, imaging, billing | English-first, US-centric, expensive, poor local support | Local fit, dual currency, WhatsApp, Arabic, price, on-prem option |
| Local cloud startups | Modern UX, Arabic | Thin clinical depth, weak billing controls | Stronger money integrity + clinical record + owner analytics |

**Moat over time:** clinic-specific workflows, imported history, treatment plans/images that are
painful to migrate, Arabic/RTL quality, WhatsApp-native communication, and local support. Not AI.

---

## 5. Business risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Receptionist rejects the product as slower than paper | High | Fatal | Schedule board, keyboard-first flows, quick intake, 48-hour onboarding, daily check-ins |
| Data loss / breach destroys trust | Low–Medium | Fatal | Backup + restore drill, audit log, least privilege, encryption, secret management |
| Scope creep into imaging/AI/insurance before core is done | High | High | This roadmap; explicit parking lot; pilot go/no-go gates |
| Clinic will not pay enough to sustain the product | Medium | High | Validate pricing with 5 clinics; prove ROI; annual prepay; keep costs low |
| Blazor Server + poor connectivity causes disconnects | Medium | Medium | On-prem/regional hosting, reconnect UX, consider WASM later |
| Currency volatility makes reports meaningless | High (in target market) | High | Base-currency ledger + historical FX snapshots; never recompute old invoices |
| Multi-tenant leak when the second clinic arrives | Medium | Fatal | Tenancy seam now, isolation tests, complete platform work before onboarding clinic #2 |
| Key-person dependency | Medium | High | Documented runbook, migrations, tests, CI, ADRs |
| Regulatory/PHI non-compliance | Medium | High | Legal review per market; retention policy; encryption; audit; no compliance claims until reviewed |
| Support cost exceeds subscription revenue | Medium | High | One-command deploy, diagnostics, in-app help, limited customization, tiered support |

---

# Part II — Solution Architect's Plan

## 6. Architecture assessment

### 6.1 Strengths to preserve

- **Strict dependency direction:** `Api → Application → Domain`, `Infrastructure` implements
  interfaces; `Application` never references EF Core directly.
- **Slice-per-feature CQRS:** one folder per use case, validators beside requests, MediatR pipeline
  for validation/performance/caching. New features have an obvious home.
- **Result flow, not exceptions:** `Result<T>` + `ErrorKind` → ProblemDetails; controllers stay
  thin.
- **Two-layer authorization:** controller policy + handler ownership check. Keep this. Never let a
  new endpoint rely on the UI hiding a button.
- **Scope in cache keys:** `DoctorAccessScope`/`PatientAccessScope` are part of `CacheKey`. Preserve
  this rule for every new cached query.
- **Concurrency correctness:** serializable transactions for appointment create/reschedule and
  invoice payments; `RowVersion` on `Invoice`; `DbUpdateConcurrencyException` → 409.
- **Interceptors:** auditing fields, soft delete, domain events. The plumbing is there; the
  domain-event pipeline just has no subscribers yet.
- **Testing infrastructure:** Testcontainers, Respawn, `WebApplicationFactory`, migration replay.
  This is the right base for the new modules.

### 6.2 Structural gaps

| Gap | Consequence | Fix in |
|---|---|---|
| No `ClinicId` / tenant seam | Second clinic impossible; retrofitting later is a risky migration | F-03 |
| Authorization is role/boolean-based, not permission-based | New roles/edge cases produce ad-hoc `if`s; hard to audit | F-04, F-32 |
| No audit log | Owner cannot answer "who changed/voided/deleted this?" | F-06 |
| Domain events have no subscribers | Dead pipeline; reminders/notifications cannot hang off events | F-12, F-18 |
| Background jobs are in-process and unsynchronized | Duplicate/no-op runs if the API scales beyond one replica | F-12, cross-cutting |
| No outbox | Notifications/reminders can be lost if the process dies after commit | F-12 |
| No file storage abstraction | Images would end up in `wwwroot` (PHI leak) or local disk (no scale) | F-23 |
| Reporting is client-side | Dashboard is wrong beyond one page; owner cannot trust it | F-28 |
| No invoice numbering/print | Legally/operationally incomplete billing | F-05 |
| No payment methods/receipts | Cash cannot be reconciled; patient cannot prove payment | F-25 |
| Blazor session is in-memory | Refresh logs the user out | F-02 |
| Localization is inline string pairs | ~150 call sites; no resource workflow; easy to miss | F-17 |
| No Web tests | UI regressions are invisible to CI | cross-cutting |
| Deployment is API-only (Web WIP is incorrect) | No repeatable pilot install | F-01 |
| Dead config / stale docs | Wastes debugging time; misleads contributors | F-08 |

### 6.3 Target architecture

Keep the modular monolith. Add capability at the edges, not a new architecture.

```text
                        ┌──────────────────────────────────────────┐
                        │  Blazor Server Web (same origin, TLS)    │
                        │  session rehydration + permission-aware  │
                        └──────────────────┬───────────────────────┘
                                           │ HTTP/JSON
                        ┌──────────────────▼───────────────────────┐
                        │  DentalClinic.Api                        │
                        │  controllers, auth, rate limit, health   │
                        └──────────────────┬───────────────────────┘
                                           │ MediatR
                        ┌──────────────────▼───────────────────────┐
                        │  DentalClinic.Application                │
                        │  CQRS handlers, validators, permissions, │
                        │  audit, reporting queries, notifications │
                        └──────────────────┬───────────────────────┘
                                           │ interfaces
            ┌──────────────────────────────┼──────────────────────────────┐
            │                              │                              │
   ┌────────▼────────┐           ┌─────────▼─────────┐          ┌─────────▼─────────┐
   │ Domain          │           │ Infrastructure     │          │ External          │
   │ aggregates,     │           │ EF Core, Identity, │          │ WhatsApp/SMS,     │
   │ policies, money │           │ storage, outbox,   │          │ object storage,   │
   │ rules           │           │ jobs, clock        │          │ backups           │
   └─────────────────┘           └─────────┬─────────┘          └───────────────────┘
                                           │
                              ┌────────────▼────────────┐
                              │ SQL Server 2022         │
                              │ + object storage (PHI)  │
                              │ + Seq / Prometheus      │
                              └─────────────────────────┘
```

New cross-cutting interfaces:

| Interface | Responsibility |
|---|---|
| `IClinicContext` | Current clinic from the token; used by query filters and write stamping |
| `IClock` | UTC + clinic timezone conversions; testable "today" |
| `IPermissionService` | `Can(permission)` from role bundles, not scattered role checks |
| `IAuditWriter` | Append-only audit events for sensitive actions |
| `IInvoiceNumberGenerator` | Concurrency-safe per-clinic/year sequence |
| `INotificationSender` | WhatsApp/SMS/email abstraction; outbox dispatcher calls it |
| `IFileStorage` | Local (pilot) or S3/Azure (scale) for images; never `wwwroot` |
| `IOutbox` | Reliable side effects after commit (reminders, notifications, exports) |

### 6.4 Architecture decisions (ADRs)

| ID | Decision | Rationale | Consequence |
|---|---|---|---|
| ADR-01 | **Modular monolith, not microservices** | One team, one database, clinic-scale load; CQRS slices already isolate features | Deploy as one API + one Web process; split only when a real bottleneck appears |
| ADR-02 | **Tenancy seam now, full platform later** | Adding `ClinicId` after real data is painful; self-service SaaS is not needed for the pilot | `Clinic` + `ClinicId` + filters now; platform UI in Phase 4 |
| ADR-03 | **Permission-based authorization** | Roles alone cannot express "receptionist can pay but not issue" cleanly | `Permission` enum + role bundles; policies and handlers call `Can(permission)` |
| ADR-04 | **Server-side reporting only** | Client-side sums over one page are wrong and erode trust | Dedicated query handlers, SQL aggregates, short cache TTL |
| ADR-05 | **Outbox for reminders/notifications** | A reminder must not be lost if the process dies after commit | `OutboxMessage` table + dispatcher; idempotent handlers |
| ADR-06 | **PHI images behind an authorized endpoint** | `wwwroot` is public; patient images are not | `IFileStorage` + `GET /patients/{id}/images/{imageId}/content` with auth |
| ADR-07 | **Invoice numbers allocated in the issue transaction** | Gaps/duplicates are unacceptable; concurrency is real | Counter row/sequence + unique `(ClinicId, InvoiceNumber)`; gaps allowed unless legal says otherwise |
| ADR-08 | **Transaction currency + base-currency snapshot** | Clinics transact in cash/local currency but report in a stable base | Store currency, FX rate, and base amount on invoice/payment; never recompute history |
| ADR-09 | **Web session: protected local storage now, BFF cookie later** | Refresh-loss is a pilot blocker; a full BFF is a larger change | Store encrypted refresh token; rehydrate circuit; revisit HttpOnly cookie before scale |
| ADR-10 | **Blazor Server + same-origin deployment** | Lowest complexity for the current team; works well on a clinic LAN/VPS | Add a reverse proxy for TLS; keep reconnect UX; consider WASM only if connectivity demands |

---

## 7. Delivery rules

Every feature must satisfy these before it is called done:

1. **Server-authoritative.** Never trust a client-supplied total, doctor id, patient id, clinic id,
   or role.
2. **Two-layer authorization.** Controller policy + handler ownership/permission check. New
   endpoints get an entry in the authz matrix test.
3. **Tenant-scoped.** Every new query/write goes through `IClinicContext`; cache keys include clinic
   and scope.
4. **Cache invalidation by tag.** Mutating handlers end with `RemoveByTagAsync("<entity>")`.
5. **Audited if sensitive.** Issue/void/payment/refund/discount/delete/export/image upload write an
   `AuditLog` entry.
6. **Migration + migration test.** Additive migration; `MigrationTests` must keep passing.
7. **Arabic/RTL smoke check.** Every new screen is usable in Arabic and prints correctly.
8. **Tests.** Domain unit tests for rules; application tests for handlers; integration for API +
   DB; a Web component/E2E test for critical flows.
9. **Observability.** New background jobs and external calls log with correlation; add a metric
   and an alert where failure matters.
10. **Docs.** Update `AGENTS.md`/README/runbook when a command, port, or operational step changes.

---

## 8. Roadmap at a glance

| Phase | Theme | Features | Effort (dev-days, 1 senior) | Exit criterion |
|---|---|---|---:|---|
| **0** | Pilot-ready trust | F-01 … F-09 | 36–54 | One-command deploy; staff onboarded; numbered invoice printed; audit + restore proven |
| **1** | Front desk & revenue protection | F-10 … F-18 | 37–56 | Receptionist runs a full day from the schedule board; reminders/recall in use |
| **2** | Clinical differentiation | F-19 … F-23 | 30–44 | Odontogram + plans + performed-only billing + alerts + images live |
| **3** | Money the owner trusts | F-24 … F-31 | 51–68 | Ledger, receipts, FX, reports, commission, cash close, lab live |
| **4** | Scale & moat | F-32 … F-35 | 38–62 | Second clinic onboarded; import/export; platform admin |

**Pilot-ready subset (Phase 0 + 1): ~73–110 dev-days.** With two developers, that is roughly
8–11 weeks including stabilization. **Commercial v1 (through Phase 3): ~154–222 dev-days**, or
5–7 months with two developers. Estimates assume one senior full-stack developer familiar with
the codebase; add 20–30% for part-time/context-switching.

---

## 9. Phase 0 — Pilot-ready trust

**Goal:** a clinic can be onboarded, use the system daily, and trust it with money and patient
data — without a developer in the room.

### F-01 — One-command deployment and environment correctness

**Outcome:** `docker compose up` produces a working API, Web UI, SQL Server, and logs on a fresh
machine; no secrets in git; no accidental Development mode in production.

**Scope**

- Fix the in-progress Web container. The current compose service sets `ASSEMBLY_NAME` while the
  Dockerfile reads `APP_PATH`, and sets `ApiSettings__BaseUrl` while `Web/Program.cs` reads
  `ApiBaseUrl`. As written, the "web" container starts the API again.
- Add a reverse proxy (Caddy/Traefik) with TLS, or document a same-origin hosting layout.
- Production profile: `ASPNETCORE_ENVIRONMENT=Production`, migrations as a separate one-shot job,
  no demo seeding, secrets from env/user-secrets, SQL password not committed.
- Health checks for API and Web; Prometheus target corrected (`mechanicshop-api` → the real
  service); Seq/Grafana passwords secured.
- CI: build API + Web, run unit tests, run Testcontainers suites, run the migration test.
- README/AGENTS: correct ports, test counts, deployment steps, and the demo-credentials location.

**Acceptance:** a clean machine can run the stack; `GET /health/ready` is healthy; the Web login
works against the containerized API; no secret literal in tracked files; the login page does not
show demo accounts in Production; CI is green.

**Effort:** 3–5 dev-days. **Depends on:** none. **Risk:** the Web container and reverse proxy are
easy to get subtly wrong; test from a clean clone, not the dev machine.

### F-02 — Session resilience, logout revocation, and screen lock

**Outcome:** refreshing the browser does not log the user out; signing out actually revokes the
refresh token; an unattended front-desk PC locks itself.

**Scope**

- Persist the refresh token with `ProtectedLocalStorage` (or a secure HttpOnly cookie/BFF later);
  rehydrate `ClinicSessionState` on circuit start; show a loading state.
- On sign-out and on refresh-token rotation failure, clear the stored token and redirect to login.
- Add an idle timeout (e.g., 20–30 minutes) and a lock screen requiring the password; do not lose
  the current page.
- Revoke the refresh token server-side on logout (`POST /identity/logout` or equivalent).
- Keep the raw token out of logs; the redactor already exists — extend it to the new paths.
- Add a "signed in on another device" / "sign out all sessions" action for the owner.

**Acceptance:** F5 refresh keeps the user signed in; logout makes the refresh token unusable;
idle timeout locks; no token appears in Seq; tests cover rehydration and revocation.

**Effort:** 3–5 dev-days. **Depends on:** F-01 for the deployed test path. **Risk:** browser
storage is a security trade-off; document it and plan the BFF cookie upgrade before scale.

### F-03 — Clinic foundation: tenancy seam, profile, and settings

**Outcome:** the app has a real clinic identity, branding, timezone, and currency; every row is
stamped with a `ClinicId`; a second clinic can be added later without a schema rewrite.

**Scope**

- `Clinic`: `NameEn`, `NameAr`, `LogoPath`, `Phone`, `Address`, `TimeZoneId`, `BaseCurrency`,
  `DisplayCurrency`, `InvoicePrefix`, `TaxNumber?`, `IsActive`, timestamps.
- `ClinicUser` (clinic membership; role remains in Identity for the pilot).
- `ClinicId` on `Patient`, `Doctor`, `DentalService`, `Appointment`, `TreatmentRecord`, `Invoice`,
  `InvoiceLine`, `Payment`; indexes; backfill for existing rows; seed a default clinic.
- `IClinicContext` resolved from the `clinic_id` claim (fallback to the seeded clinic only in
  Development); global query filters; write stamping in `SaveChanges`.
- Replace hardcoded "Al Noor Dental" in the layout/login/titles with clinic settings.
- Settings page: profile, logo upload (small, validated), timezone, currency, invoice prefix;
  Admin-only; audited.
- Use the clinic timezone for "today", calendar boundaries, report buckets, and print dates —
  store UTC, convert at the edges.

**Acceptance:** existing data is backfilled; queries cannot see another clinic's rows (test with
a second seeded clinic); branding changes without a rebuild; an appointment at 23:30 local lands
in the correct report day; `MigrationTests` passes.

**Effort:** 8–12 dev-days. **Depends on:** F-01. **Risk:** this is the highest-blast-radius
Phase 0 item; do it before importing real data, and write the isolation tests first.

### F-04 — Staff administration and doctor–user link

**Outcome:** the owner can create, role, deactivate, and reset staff without a developer; a
doctor created in the UI sees their own appointments and patients.

**Scope**

- `StaffUserController`: list, create with temporary password, deactivate/reactivate, reset
  password, assign/remove roles.
- `AppUser` additions: `IsActive`, `MustChangePassword`, `DeactivatedAtUtc` (or equivalent
  Identity lockout usage).
- `POST /doctors/{id}/link-user` and `DELETE /doctors/{id}/link-user`; enforce the unique
  `Doctor.UserId` index; prevent linking one user to two doctors.
- Permission model: define `Permission` values (`patient.read/write`, `appointment.read/write`,
  `treatment.read/write`, `invoice.read/create/issue/pay/void`, `staff.manage`,
  `clinic.settings.manage`, `reports.view`, `audit.view`) and role bundles; keep
  `ClinicAuthorization` as the thin wrapper.
- Admin-only; audit every change. Force a password change on first login; no email dependency
  for the pilot.
- Frontend: Staff page, role-aware nav, route guards, disabled actions with tooltips (defense in
  depth, never the only check).

**Acceptance:** a new doctor can log in and sees only their own appointments/invoices; a
deactivated user cannot refresh or sign in; a receptionist cannot issue/void an invoice; the
authorization matrix test covers every endpoint × role; doctor-link uniqueness is enforced.

**Effort:** 6–8 dev-days. **Depends on:** F-03 (`ClinicUser`). **Risk:** existing tests may assume
"any authenticated user can pay"; update them deliberately.

### F-05 — Invoice numbering, printable invoice, and receipt

**Outcome:** every issued invoice has a human number; the patient walks out with a branded,
Arabic/English printable invoice or payment receipt.

**Scope**

- `InvoiceNumber` (e.g., `INV-2026-000123`), `IssuedAtUtc`, `IssuedByUserId`, unique
  `(ClinicId, InvoiceNumber)`; per-clinic/year counter allocated inside the issue transaction.
- Assign numbers to standalone invoices at creation (they are issued immediately) and to drafts
  at issue; re-issue is idempotent.
- Void an unpaid invoice with a reason (paid invoices go through credit/refund in F-25); audit
  both.
- Print views: A4 invoice and 80mm receipt; clinic logo/name/address/phone; patient; date;
  service/treatment lines; subtotal/discount/total/paid/balance; payment history; "thank you"
  footer; Arabic RTL and English LTR.
- Remove the `#A1B2C3D4` id-prefix from the UI.
- Add a print stylesheet and test with Arabic text; no client-side total calculation.
- Add `ServiceId?` and `TreatmentRecordId?` to `InvoiceLine` now, so reports can group by service
  later without another schema change.

**Acceptance:** concurrent issue requests produce unique numbers; the printed total equals the
server total; the patient name and clinic logo appear correctly in both languages; a voided
invoice is visibly void and excluded from balances; the invoice number is searchable.

**Effort:** 5–7 dev-days. **Depends on:** F-03 (clinic settings/prefix). **Risk:** legal
requirements for gapless numbering vary; confirm before promising "no gaps".

### F-06 — Audit log

**Outcome:** the owner can answer "who issued, voided, discounted, deleted, or exported what,
and when?" without reading database logs.

**Scope**

- `AuditLog`: `ClinicId`, `ActorUserId`, `Action`, `EntityType`, `EntityId`, `Summary`,
  `OccurredAtUtc`, `IpAddress?`, `UserAgent?`; append-only; no updates/deletes.
- `IAuditWriter` + explicit calls in sensitive handlers: login success/failure, user/role changes,
  invoice issue/void, payment add/refund, discount/manual line, patient delete/merge, settings
  change, image upload/delete, data export/import.
- Do not store clinical payloads or secrets in the summary; reference ids only.
- API: `GET /audit?from&to&actor&entity&action` (Admin-only, paginated).
- UI: Audit page with filters and a link to the entity; add "history" tabs on invoice and patient
  detail.
- Retention: 2 years for pilot; document the policy.

**Acceptance:** every sensitive action produces exactly one entry; entries survive entity deletion;
a non-admin cannot read the audit; no PHI/password/token appears in the entry; the invoice history
shows issue/void/payment events.

**Effort:** 4–6 dev-days. **Depends on:** F-03 (clinic scope). **Risk:** logging too much is as bad
as too little; define the action list and stick to it.

### F-07 — Backup, restore, and operational runbook

**Outcome:** a database loss is recoverable within a documented RTO, and the restore has actually
been rehearsed.

**Scope**

- SQL Server native `BACKUP DATABASE` on a schedule (SQL Agent job or a sidecar cron); full daily,
  differential/hourly if volume justifies; transaction log backups for point-in-time recovery.
- Copy backups off the host (S3/Backblaze/another machine), encrypted; retention 30 daily + 12
  monthly.
- Back up the image/object storage bucket too; the database alone is not a full restore.
- Alert on backup failure/staleness (Seq/Grafana or the backup tool's own notification).
- Write `docs/runbook/backup-restore.md`: RPO/RTO, commands, verification query, restore-to-staging
  procedure, and who to call.
- Rehearse a restore into a clean environment before the first pilot; record the elapsed time.
- Do **not** implement backups as an in-process `BackgroundService`; the app process is the wrong
  failure domain.

**Acceptance:** a restore from the previous night produces a working app with the expected row
counts; the drill is documented with timings; a failed backup triggers an alert; the backup target
is off-host.

**Effort:** 2–3 dev-days (ops). **Depends on:** F-01. **Risk:** silent failure; alert on staleness,
not just on job errors.

### F-08 — Correctness and security debt cleanup

**Outcome:** known defects that could corrupt money or leak data are closed before real clinics
use the system.

**Scope**

- Fix the `CreateInvoice` check-then-insert race: wrap in a serializable transaction and/or add a
  unique filtered index on `Invoices.AppointmentId` (non-null); return the existing invoice or a
  conflict.
- Fix `IdentityService.GetUserByIdAsync` throwing `InvalidOperationException`; return a
  `Result` error so the refresh path can handle a deleted user.
- Decide and document the `InvoiceLine` soft-delete policy; if it stays hard-deleted, remove the
  stale comment; if it changes, add the filter and tests.
- Map `ErrorKind.Unauthorized` to 401 (currently 403) and review mixed-kind error lists (only the
  first error is surfaced).
- Cap `GetPatientsQuery.SearchTerm` length and normalize phone search; protect the cache key from
  unbounded distinct values.
- Remove/correct dead configuration: `AppSettings` properties with no consumers, the unused
  `DoctorOnly` policy (apply it or delete it), missing `RateLimiting` config, stale CORS origins.
- Upgrade `System.Security.Cryptography.Pkcs` (9.0.11 → patched version) and re-run `dotnet list
  package --vulnerable`.
- Fix the Prometheus scrape target and any stale README/AGENTS claims.
- Add a short "known limitations" section to the README instead of implying production readiness.

**Acceptance:** concurrent `CreateInvoice` requests produce one invoice; a deleted user during
refresh returns a clean 401; the vulnerable package is gone; the build has no new warnings; the
known-limitations section is accurate.

**Effort:** 3–5 dev-days. **Depends on:** none. **Risk:** touching invoice creation can break
existing tests; add the regression test first.

### F-09 — Realistic demo data and onboarding checklist

**Outcome:** sales demos and pilot onboarding are not done on an empty database; the first
impression shows a living clinic.

**Scope**

- Seed 20–30 patients, 4–6 months of appointments (mixed statuses), treatments across teeth,
  invoices in draft/issued/partial/paid states, payments, and at least one no-show pattern.
- Keep demo data Development-only; never seed Production.
- Add a one-page Arabic/English onboarding checklist: clinic profile, users, services, hours,
  import, first appointment, first invoice, backup proof.
- Add a demo script for sales: today's schedule → patient tooth history → treatment plan → invoice
  → payment → owner cash view.
- Add screenshots/GIFs to the README.

**Acceptance:** a fresh `docker compose up` shows a believable clinic; the demo script runs in
under 10 minutes; no demo account is active in Production.

**Effort:** 2–3 dev-days. **Depends on:** F-01, F-03. **Risk:** demo data can hide real UX
problems; keep it realistic, not perfect.

---

## 10. Phase 1 — Front desk and revenue protection

**Goal:** the receptionist can run the day from one screen, and the clinic can reduce no-shows
and bring back overdue patients.

### F-10 — Day/week schedule board

**Outcome:** the front desk sees the whole day by doctor, with status, gaps, and quick actions —
no more scrolling a table.

**Scope**

- Day and week views; columns per doctor (chairs optional later); time grid with configurable
  slot size; current-time indicator.
- Status colors: scheduled, confirmed, completed, cancelled, no-show; visual gap/overbook markers.
- Quick actions: create, reschedule (drag-and-drop or click), confirm, complete, cancel, no-show,
  WhatsApp.
- Filters: doctor, status, service; "today" and "unconfirmed" shortcuts.
- API: `GET /appointments/calendar?from&to&doctorIds&statuses` returning a compact calendar DTO
  in one round trip; indexes on `(DoctorId, ScheduledDateTime)` and `(ScheduledDateTime, Status)`.
- Print day sheet (Arabic/English) for the back office.
- Performance budget: a week view loads in < 1 second on the pilot dataset.

**Acceptance:** a receptionist can book, move, confirm, and check out a patient without leaving
the board; overlapping bookings are rejected with a clear message; the board reflects changes
without a manual refresh; the day sheet prints correctly.

**Effort:** 7–10 dev-days. **Depends on:** F-03 (timezone), F-11 (availability). **Risk:** drag-and-
drop is a time sink; ship click-to-reschedule first, drag later.

### F-11 — Working hours, holidays, and doctor availability

**Outcome:** the system only offers slots the clinic can actually serve; the calendar shows real
capacity.

**Scope**

- `ClinicWorkingHours` per weekday (open/close, break), `ClinicHoliday` dates, `DoctorAvailability`
  per weekday (start/end, slot duration, buffer).
- Validation on appointment create/reschedule: inside clinic hours, inside doctor availability,
  not on a holiday; clear error codes.
- `GET /appointments/availability?doctorId&date&duration` returns free slots.
- Calendar uses availability to render closed/blocked time.
- Seed sensible defaults for the pilot; Admin can edit.
- Use the clinic timezone; store UTC.

**Acceptance:** booking outside hours/holiday is rejected; the availability endpoint matches the
calendar; DST/timezone tests pass; the doctor can block a day without editing every appointment.

**Effort:** 4–6 dev-days. **Depends on:** F-03. **Risk:** availability rules can become
over-engineered; start with weekday hours + holidays, not recurrence rules.

### F-12 — Appointment confirmation and WhatsApp reminders

**Outcome:** the clinic sends a professional confirmation/reminder in one click (and later
automatically), reducing no-shows.

**Scope**

- Appointment fields: `ConfirmationStatus` (Unconfirmed/Confirmed/Declined), `ConfirmedAtUtc`,
  `ReminderSentAtUtc`, `ReminderChannel`; UI badges and a "today's unconfirmed" list.
- **Pilot:** client-side `wa.me` link with a templated Arabic/English message (clinic name, patient
  name, date/time, doctor, address, reschedule instructions); normalize the phone number; open in
  a new tab; log the communication as an activity.
- **Scale:** `INotificationSender` + `OutboxMessage` + a scheduled dispatcher for 24-hour reminders;
  WhatsApp Cloud API provider; idempotency key per appointment/reminder window; opt-out flag.
- Templates editable in clinic settings; preview before send.
- Manual "mark confirmed" and "declined/reschedule" actions.
- No PHI in the message beyond what the patient needs; no diagnosis/treatment details.

**Acceptance:** one click opens WhatsApp with the correct number and message; the link works for
`+963…` numbers; a second reminder in the same window is blocked; the appointment shows
"reminded"; the message renders correctly in Arabic; no duplicate sends after a retry.

**Effort:** 5–8 dev-days for click-to-chat; +5–8 for the automated provider. **Depends on:** F-04
(permissions), F-10 (schedule board). **Risk:** `wa.me` is a convenience, not a delivery guarantee;
do not sell it as an automated reminder system until the API path exists.

### F-13 — Recall and reactivation list

**Outcome:** the clinic systematically brings back patients who are due or overdue, instead of
waiting for them to call.

**Scope**

- `RecallType` (6-month checkup, perio, ortho, custom), `PatientRecall` (patient, type, due date,
  status Due/Contacted/Booked/Declined, notes, last contact).
- Auto-create a recall when a treatment/appointment of a configured type is completed; allow
  manual creation.
- List: due now, overdue, upcoming; filter by doctor; sort by overdue days/value.
- Actions: WhatsApp/call, mark contacted, book appointment, snooze, decline; log the activity.
- Dashboard card: "recalls due this week".
- Print/export the call list.

**Acceptance:** completing a checkup creates the next recall; the list shows the right patients;
booking from the list links the appointment; contact attempts are logged; no duplicate recalls per
patient/type/window.

**Effort:** 4–6 dev-days. **Depends on:** F-12 (messaging), F-10 (booking). **Risk:** recall rules
vary by clinic; make the interval configurable and start with one type.

### F-14 — Waitlist and cancellation fill

**Outcome:** a cancelled slot is offered to a waiting patient instead of staying empty.

**Scope**

- `WaitlistEntry`: patient, doctor (optional), preferred date range, preferred time of day,
  priority, notes, status Waiting/Offered/Booked/Expired.
- When an appointment is cancelled, show matching waitlist entries on the cancellation screen;
  one-click offer via WhatsApp; mark offered/booked.
- List view with filters; expiry after N days.
- No automatic messaging in the first cut; the receptionist sends the offer.

**Acceptance:** cancelling an appointment surfaces matching waitlist patients; booking from the
waitlist removes the entry and links the appointment; no duplicate entries for the same patient/
window; the list is printable.

**Effort:** 4–6 dev-days. **Depends on:** F-10, F-12. **Risk:** low; keep it manual until the
clinic trusts the flow.

### F-15 — Fast patient intake, duplicate detection, and merge

**Outcome:** a new patient is added in under a minute, duplicate records are caught before they
spread, and existing duplicates can be cleaned up safely.

**Scope**

- Quick-add from the schedule board and appointment form; only the essentials (name, phone, DOB,
  gender, referral source); complete the rest later.
- Search by phone/name/DOB; show possible duplicates with a "use existing" action.
- Soft duplicate warning on create (not a hard block unless phone+name+DOB match); log the
  override.
- Admin-only merge: choose the survivor, move appointments/treatments/invoices/payments/images,
  soft-delete the duplicate, audit the merge; never delete financial history.
- Add `ReferralSource` and `PreferredLanguage` to the patient profile; use them in reports and
  messaging.

**Acceptance:** a duplicate phone+name triggers a warning; merging preserves all financial and
clinical links; the merge is audited and reversible via the audit trail; quick-add takes < 60
seconds.

**Effort:** 4–6 dev-days. **Depends on:** F-04 (permissions), F-10. **Risk:** merge is destructive
if done wrong; implement in a transaction and test with invoices/payments attached.

### F-16 — Service catalog enrichment

**Outcome:** services carry enough information to book correctly, invoice consistently, and report
by category.

**Scope**

- `DentalService`: add `DurationMinutes`, `Category`, `IsToothSpecific`, `RequiresLab`,
  `Color` (calendar), `Code` (optional), `IsActive`.
- Use duration to default appointment length; use category in reports; use tooth-specific flag to
  show/hide the tooth selector; use lab flag to prompt a lab case later.
- Admin-only edits; price history is already snapshotted on invoice lines — keep that rule.
- Seed a realistic dental catalog (exam, cleaning, filling, crown, root canal, extraction,
  implant, ortho consult).

**Acceptance:** booking a service defaults the duration; the calendar color reflects the service;
reports can group by category; changing a price does not alter historical invoices.

**Effort:** 2–3 dev-days. **Depends on:** none. **Risk:** low.

### F-17 — Persisted preferences and resource-based localization

**Outcome:** language/theme/sidebar survive a refresh, and Arabic/English strings become
maintainable instead of ~150 inline pairs.

**Scope**

- Persist `Language`, `Theme`, `SidebarCollapsed` per user (browser storage now; user profile
  later) and rehydrate on circuit start.
- Move UI strings to `.resx` resources or a single typed localization catalog; remove inline
  `T(en, ar)` pairs feature by feature (start with the new screens, then the existing pages).
- Localize dates, numbers, currency, and plural forms; use the clinic timezone.
- Ensure print views and WhatsApp templates use the same resources.
- Add a pseudo-localization test or a missing-key build check.
- Keep RTL layout tests for tables, dialogs, and the calendar.

**Acceptance:** refresh keeps the chosen language/theme; no user-facing string is hardcoded in a
new component; missing keys fail the build/test; Arabic print output is correct.

**Effort:** 4–6 dev-days. **Depends on:** F-03. **Risk:** touching every page is tedious; migrate
incrementally, starting with high-traffic screens.

### F-18 — In-app notification center

**Outcome:** the bell icon is real; staff see new bookings, cancellations, payments, recalls, and
overdue lab cases without asking.

**Scope**

- `Notification` entity (clinic, user/role, type, title, body, link, read state, createdAt) or a
  derived feed for the first cut.
- Events from domain handlers/outbox: appointment created/cancelled/confirmed, payment received,
  recall due, lab overdue, low-priority system alerts.
- Bell with unread count; dropdown; mark read/all read; links to the entity.
- Poll every 60 seconds for the pilot; SignalR later if real-time is needed.
- Do not include PHI in the notification body beyond the patient name and appointment time.

**Acceptance:** a new booking appears in the receptionist's bell within a minute; marking read
persists; notifications are scoped to the clinic/role; no duplicate entries for one event.

**Effort:** 3–5 dev-days. **Depends on:** F-12 (outbox/events), F-04. **Risk:** notification noise;
make types configurable and start with the four highest-value events.

---

## 11. Phase 2 — Clinical differentiation

**Goal:** the system becomes the dentist's primary record, not just the receptionist's scheduler.

### F-19 — Odontogram

**Outcome:** a clinician can see and update the state of every tooth, with the full history behind
each tooth, in one screen.

**Scope**

- `PatientTooth`: `PatientId`, `ToothNumber` (FDI), `Condition` (Healthy, Caries, Filled, Crown,
  Bridge, Implant, Missing, Extracted, RootCanal, Sealant, Fracture), `Surfaces` (M/O/D/B/L flags),
  `Notes`, `UpdatedAtUtc`, `UpdatedByUserId`; unique `(PatientId, ToothNumber)`.
- Support permanent (11–48) and primary (51–85) teeth; a toggle for dentition; missing/extracted
  visual states.
- `TreatmentRecord.ToothNumber` becomes nullable (a consultation or sedation is not tooth-specific);
  centralize FDI validation in an `FdiTooth` value object; remove the duplicated validators.
- `GET /patients/{id}/odontogram` returns teeth state + treatment/image history grouped by tooth in
  one request; `PUT /patients/{id}/teeth/{toothNumber}` upserts condition/surfaces/notes.
- Cache tag `tooth`; invalidate on treatment create/update, tooth update, image upload.
- UI: clickable tooth chart (reuse `ToothChart.razor` as the base), color legend, drawer with
  condition, surfaces, history, treatments, images, and "add treatment" action; print the chart.
- Backfill: do not invent conditions from historical treatment text; mark untouched teeth as
  "unknown until reviewed" or healthy per clinic policy.

**Acceptance:** the chart loads in one request; a condition change is visible immediately and
audited; per-tooth history is complete; primary teeth render correctly; the printed chart is legible
in Arabic and English; a treatment with no tooth can be saved.

**Effort:** 8–12 dev-days. **Depends on:** F-03, F-16. **Risk:** clinical vocabulary must be agreed
with the pilot dentist before coding; avoid a free-text condition field.

### F-20 — Treatment plans and printable estimates

**Outcome:** the dentist presents a clear plan with costs, the patient accepts, and the clinic can
schedule and bill it without re-typing.

**Scope**

- `TreatmentPlan`: patient, doctor, title, status (Draft, Presented, Accepted, Declined, Completed,
  Cancelled), notes, presented/accepted timestamps, total estimate.
- `TreatmentPlanItem`: plan, service?, tooth?, description, quantity, unit price, discount,
  sequence, status (Planned, Performed, Cancelled), optional notes.
- Actions: add/remove/reorder items, apply a plan-level discount, present, accept/decline with a
  reason, convert accepted items into appointments/treatment records, mark performed.
- Only performed items become billable; the invoice generator reads performed items, not the whole
  plan.
- Printable estimate: clinic logo/header, patient, date, tooth, service, qty, price, discount,
  total, payment terms, acceptance signature line, Arabic/English.
- Dashboard: presented vs accepted value (case acceptance).
- Permissions: doctor/admin create/present; receptionist can view/print; acceptance recorded by
  staff.

**Acceptance:** a plan total is server-calculated; accepting a plan does not create an invoice;
converting items creates the right appointments/treatments; only performed items are invoiced;
the printout matches the plan; a declined plan is retained with a reason.

**Effort:** 8–12 dev-days. **Depends on:** F-19 (tooth/service links), F-05 (print). **Risk:** scope
creep into full ortho case management; keep the first cut to a linear list of items.

### F-21 — Clinical status and billing separation

**Outcome:** planned work, performed work, and billed work are distinct; the clinic stops billing
things that were never done.

**Scope**

- `TreatmentRecord`: add `Status` (Planned, Performed, Cancelled), `ServiceId?`, `ClinicalNotes?`,
  `PerformedAtUtc?`; `ToothNumber` nullable; keep `Cost` as the agreed fee.
- Backfill existing records as `Performed` (they represent historical work).
- Invoice generation includes only `Performed` records with a positive cost; planned items are
  excluded.
- `InvoiceLine` already gets `ServiceId?`/`TreatmentRecordId?` in F-05; use them to trace a line
  back to the clinical record and prevent double-billing.
- Correction rules: a performed record that is already invoiced cannot be silently changed; use a
  controlled correction that adjusts the draft or creates a credit note after issue.
- Tests: the highest-value tests in the project — cost-per-session, only-performed billing,
  no-double-billing, correction rules.

**Acceptance:** a planned item never appears on an invoice; completing an appointment with two
performed treatments produces exactly two lines; editing a performed record after issue does not
mutate the invoice; the backfill leaves historical totals unchanged.

**Effort:** 5–7 dev-days. **Depends on:** F-19, F-20. **Risk:** changing billing semantics can break
existing invoice tests; introduce the status with a backfill and update tests deliberately.

### F-22 — Medical alerts everywhere

**Outcome:** allergies and medical risks are visible at the moment of care, not hidden on one tab.

**Scope**

- Extend `MedicalHistory`: `IsPregnant`, `PregnancyWeeks?`, `BloodPressure` (systolic/diastolic),
  `HasBleedingDisorder`, `HasAsthma`, `HasEpilepsy`, `HasKidneyDisease`, `Notes`.
- Replace/augment free-text allergies with `PatientAllergy` (substance, reaction, severity, notes);
  keep the old field for backward compatibility during migration.
- `MedicalAlertEvaluator` in the domain: derives severity (Critical/Warning/Info) from the flags;
  unit-test the table.
- Surface `AlertSeverity` on `PatientDto` so every screen can render it without re-deriving:
  patient list badge, appointment drawer, treatment form, odontogram, invoice drawer.
- Optional hard-stop: require an acknowledgement before recording a treatment for a critical
  allergy; default to a prominent warning, not a block, for the pilot.
- Do not expose the full medical history to roles that do not need it; respect `PatientScope`.

**Acceptance:** a patient with a critical allergy shows a red badge on the schedule board and the
treatment screen; the evaluator tests cover every combination; receptionists see the alert without
seeing clinical notes they should not; the alert survives refresh and print.

**Effort:** 3–4 dev-days. **Depends on:** F-04 (permissions). **Risk:** low; the data already exists,
the value is in surfacing it.

### F-23 — Clinical images and X-rays

**Outcome:** the patient's imaging is attached to the patient/tooth and visible in the clinical
record, without putting PHI in a public folder.

**Scope**

- `PatientImage`: patient, treatment?, tooth?, kind (X-ray, intraoral, before, after, document),
  file name, content type, size, storage key, captured date, uploaded by, soft delete.
- `IFileStorage`: local encrypted volume for the pilot; S3-compatible/Azure for scale; never serve
  from `wwwroot`.
- Upload validation: max size, content-type allow-list, magic-byte sniffing, generated server-side
  file name, optional malware scan; image dimensions/thumbnail generation.
- Authorized content endpoint: `GET /patients/{id}/images/{imageId}/content` with clinic/patient
  scope; no public URLs; cache headers private.
- UI: gallery on patient details, upload from odontogram/tooth drawer, before/after compare, delete
  with audit; permission: clinical write for upload/delete, read for staff.
- Retention/export: include images in the patient data export; document deletion policy.

**Acceptance:** a JPEG/PNG upload succeeds and renders; an executable disguised as an image is
rejected; an unauthenticated request cannot fetch the file; an image linked to a tooth appears in
that tooth's history; deleting is soft and audited.

**Effort:** 6–9 dev-days. **Depends on:** F-19, F-03. **Risk:** security-critical; do not ship
without the validation tests and an authorization test.

---

## 12. Phase 3 — Money the owner trusts

**Goal:** the owner can run the clinic's cash flow, collections, and doctor pay from the system.

### F-24 — Patient ledger, aging, and statement

**Outcome:** "who owes me money, how old is it, and what is the full history?" answered in one
screen, server-side.

**Scope**

- `GET /billing/patients/{id}/balance` → total billed, paid, balance, open invoice count, oldest
  unpaid date.
- `GET /billing/patients/{id}/ledger` → chronological invoices, payments, adjustments, running
  balance; filter by date/type.
- `GET /billing/outstanding` → paginated patients with balance, aging buckets (0–30, 31–60, 61–90,
  90+), last payment date, contact action.
- `GET /billing/aging` → clinic-level totals by bucket.
- Statement: printable/WhatsApp-friendly patient statement with clinic header, invoices, payments,
  balance, payment instructions.
- Replace every client-side sum in `Dashboard.razor`, `Invoices.razor`, and `PatientDetails.razor`
  with these endpoints; add a regression test with > 1 page of invoices.
- Define "debt": issued invoices only; drafts are not receivable. Document it.

**Acceptance:** totals match the ledger for a patient with 50+ invoices; a doctor sees only their
own patients' balances (or is denied, per policy); the outstanding list matches SQL; the statement
prints correctly; no client-side sum remains.

**Effort:** 6–8 dev-days. **Depends on:** F-05, F-21. **Risk:** low technical risk; the main trap is
shipping another client-side sum.

### F-25 — Payment methods, receipts, refunds, and void

**Outcome:** every payment is reconcilable, every patient can get a receipt, and mistakes are
handled without deleting history.

**Scope**

- `Payment`: add `Method` (Cash, Card, BankTransfer, Cheque, Wallet, Other), `ReferenceNumber?`,
  `PayerName?`, `ReceivedByUserId`; keep `PaidAtUtc` and notes.
- Receipt print (80mm and A4) with invoice number, amount, method, remaining balance.
- Refund/credit note: a compensating `Payment` (negative) or a `CreditNote` entity linked to the
  invoice; never delete a payment; audit with reason and approver.
- Void invoice: only if unpaid; reason required; number retained (marked void); audit.
- Paid invoice correction: credit note + optional reissue; do not edit the issued invoice.
- Cash/card reconciliation report by day and method.

**Acceptance:** a partial cash payment prints a receipt with the correct balance; a refund reduces
the paid amount and appears in the ledger; a voided invoice keeps its number and is excluded from
receivables; concurrent payments still produce one winner (existing row-version test); every action
is audited.

**Effort:** 6–8 dev-days. **Depends on:** F-24. **Risk:** accounting semantics; get the clinic's
accountant to sign off on refund/credit behavior before coding.

### F-26 — Installment plans

**Outcome:** larger treatments can be paid over time, with due dates and reminders, without a
spreadsheet.

**Scope**

- `PaymentPlan` (invoice, total, installment count, start date, notes, status) and `Installment`
  (due date, amount, paid amount, status Pending/PartiallyPaid/Paid/Overdue).
- Creating a plan splits the remaining balance; payments allocate to the earliest due installment;
  early payoff allowed.
- Ledger shows plan schedule and remaining; statement includes it.
- Reminder: due soon/overdue list; WhatsApp template; optional automatic reminder via outbox.
- Permissions: admin/receptionist create; doctor view; audit changes.

**Acceptance:** a $600 invoice with 3 installments shows $200 due on each date; a $150 payment
marks the first installment partially paid; overdue status is correct at the clinic timezone; the
patient statement matches; cancelling a plan restores the invoice balance.

**Effort:** 5–7 dev-days. **Depends on:** F-24, F-25. **Risk:** allocation rules must be explicit;
do not let installments and invoice balance disagree.

### F-27 — Dual currency and historical exchange rates

**Outcome:** the clinic can transact in USD and local currency, while the owner reports in one
stable base currency and old invoices remain reproducible.

**Scope**

- `ExchangeRate`: base currency, quote currency, rate `decimal(18,6)`, effective date/time, source,
  created by; only one active rate per pair at a time; history retained.
- `Invoice`/`Payment`: `CurrencyCode`, `ExchangeRateToBase` snapshot, `AmountBase`; line amounts in
  the transaction currency; totals in both.
- `Clinic.BaseCurrency` (reporting/ledger) and `DisplayCurrency` (UI default); settings UI to set
  the current rate and view history.
- Validation: rate > 0; currency codes ISO-4217; no changing the rate on an issued invoice.
- Display: show both amounts consistently (e.g., `$100 / 1,250,000 SYP`); print both.
- Reports: sum `AmountBase`; never sum across currencies without conversion.
- Rounding: define and test (recommend 2 decimals, midpoint away from zero); document.
- Migration: existing rows get the clinic base currency and rate 1.

**Acceptance:** an invoice issued at 12,500 SYP/USD keeps that rate forever; a later rate change
does not alter it; a payment in a different currency converts correctly; reports reconcile to the
base ledger; rounding tests pass; no double conversion.

**Effort:** 9–12 dev-days. **Depends on:** F-24, F-25. **Risk:** highest blast radius in Phase 3;
do it after the ledger is server-side, and test with the pilot's real cash flow.

### F-28 — Owner dashboard and reports

**Outcome:** the owner opens one page and sees the truth: cash collected, revenue, debt, no-shows,
and production by doctor/service, for any date range.

**Scope**

- Server-side query handlers with SQL aggregates, `AsNoTracking`, and indexes:
  - Today: appointments, completed, no-shows, collected, outstanding, new patients.
  - Period: revenue (invoiced and collected), payments by method, outstanding, average ticket.
  - By doctor: appointments, completed, production, collected, no-show rate.
  - By service/category: revenue, volume.
  - No-show rate and cancellation rate with explicit definitions.
  - New vs returning patients; recall conversion.
  - Chair utilization (requires F-11); if chairs are not modeled, use booked minutes per doctor.
- Date range picker (today, this week, this month, custom); clinic timezone boundaries.
- Export CSV/Excel; print; scheduled email later.
- Short cache TTL (30–60s) with tags; never cache across clinics.
- Dashboard UI: replace client-side sums; add a date range and a "last updated" timestamp.
- Define every metric in a tooltip/glossary so the owner and the accountant agree.

**Acceptance:** dashboard numbers match direct SQL for a seeded period; a doctor sees only their
own production unless Admin; date boundaries respect the clinic timezone; export matches the screen;
no client-side aggregation remains.

**Effort:** 8–10 dev-days. **Depends on:** F-24, F-27. **Risk:** metric definitions cause more
arguments than code; write them down before building.

### F-29 — Doctor commission and payout report

**Outcome:** the monthly "how much does each doctor earn?" spreadsheet is replaced by a transparent,
auditable report.

**Scope**

- `CommissionRule`: doctor, percent, basis (Collected or Invoiced), effective from/to, optional
  service-category overrides, include/exclude lab cost; effective-dated, no retroactive edits
  without an audit.
- Calculation: per doctor, per period, per invoice/payment; show the underlying lines; handle
  refunds/credit notes and lab costs.
- Payout report: doctor, production, collections, commission %, commission amount, lab deductions,
  adjustments, net payable; export/print.
- Optionally store a `CommissionPayout` snapshot when the owner finalizes a period; never recalculate
  a finalized payout silently.
- Permissions: Admin only; a doctor can see their own report if the owner allows it.
- Tests: rounding, effective dates, refunds, lab deductions, overlapping rules (reject).

**Acceptance:** the report reconciles to the ledger for a period; changing a rule does not alter a
finalized payout; a refund reduces the commission correctly; the doctor's own view is scoped; every
rule change is audited.

**Effort:** 6–8 dev-days. **Depends on:** F-28, F-31 (lab costs). **Risk:** commission policy is a
business decision; get it in writing before coding (see §20).

### F-30 — Daily cash close and expenses

**Outcome:** the receptionist closes the drawer at the end of the day; the owner sees expected vs.
counted cash and the day's expenses.

**Scope**

- `CashSession`: date, opening float, expected cash (payments in cash − cash refunds), counted cash,
  variance, closed by, closed at, notes; one open session per clinic/day.
- `Expense`: date, category (rent, salaries, supplies, lab, utilities, other), amount, method,
  vendor, receipt image?, paid by, notes; optional approval.
- Daily close screen: payments by method, refunds, expenses, expected vs counted, variance reason.
- Report: daily cash summary, monthly expenses by category; export.
- Lock the day after close (Admin can reopen with a reason; audit).
- Link to the ledger: expenses are not invoice payments; keep them separate.

**Acceptance:** a day with $500 cash payments and a $50 cash refund expects $450 + float; a
discrepancy requires a reason; closing locks the day; reopening is audited; the monthly expense
report matches.

**Effort:** 5–7 dev-days. **Depends on:** F-25. **Risk:** low; it is mostly UI + one transaction.

### F-31 — Lab case tracking

**Outcome:** crowns, bridges, and dentures are tracked from sent to returned, with expected dates
and costs, so nothing is lost and the patient is called when it arrives.

**Scope**

- `LabCase`: patient, doctor, treatment?, tooth?, lab name, case type, status (Sent, AtLab, Trial,
  Returned, Fitted, Failed/Cancelled), sent/expected/returned dates, cost, charge-to-patient?,
  notes.
- Link from a tooth/treatment plan item ("send to lab"); auto-create from a service flagged
  `RequiresLab`.
- Overdue list and notification; background job (single-replica-safe) marks overdue.
- Cost handling: lab cost is either an expense or a billable line; decide per clinic; include in
  commission basis if configured.
- UI: list/kanban by status, overdue highlighting, patient/tooth link; print lab slip.
- Permissions: clinical write for status changes; receptionist view.

**Acceptance:** a case sent today with a 7-day expectation appears overdue on day 8; returning it
updates the patient record; the lab cost appears in the expense/reporting path chosen; no case is
lost after a status change; the lab slip prints.

**Effort:** 6–8 dev-days. **Depends on:** F-19, F-21. **Risk:** lab cost accounting is a business
decision; do not guess.

---

## 13. Phase 4 — Scale and moat

**Goal:** onboard clinic #2 without a rewrite, and make data migration/export painless.

### F-32 — Multi-clinic platform administration

**Outcome:** the operator can create, suspend, and support clinics; each clinic's data and settings
are isolated; a platform admin exists without becoming a clinic super-user by accident.

**Scope**

- Complete the tenancy work: `ClinicUser` roles per clinic, platform admin role, clinic creation/
  suspension, provision the first clinic admin, reset clinic admin password.
- Tenant resolution from the token/claim (or subdomain if desired); fail-closed if missing.
- Per-clinic settings, numbering, currency, timezone, logo.
- Cross-tenant isolation test matrix (read/write/list/export/audit); a clinic admin must not gain
  platform privileges.
- Platform health page: clinic count, active users, last backup, error rate; no PHI.
- Billing/licensing: manual invoicing for the first clinics; do not build subscription billing yet.
- Onboarding runbook: create clinic, import data, train, go-live.

**Acceptance:** two clinics cannot see each other's patients/appointments/invoices/audit/images;
a platform admin can create a clinic and its first admin; a clinic admin cannot list other clinics;
suspension blocks login but preserves data; isolation tests pass.

**Effort:** 10–15 dev-days. **Depends on:** F-03, F-04. **Risk:** a cross-tenant leak is fatal; do
not onboard clinic #2 until the matrix is green.

### F-33 — Import and export (Excel/CSV)

**Outcome:** a clinic can migrate from Excel/another system in hours, and can take its data out if
it leaves.

**Scope**

- Import patients, doctors, services, opening balances, and historical invoices/payments (optional,
  phased).
- Template download; column mapping; preview/dry-run; per-row validation with a downloadable error
  report; no partial silent commits.
- Import through the same domain factories/validators as the API; invalidate cache tags; audit the
  import; idempotency key to prevent double import.
- Duplicate detection (phone/name/DOB) with merge/skip options.
- Export: patients, appointments, invoices, payments, treatments, ledger, audit (CSV/Excel); a
  "full clinic export" for data portability.
- Permissions: Admin only; audit downloads.

**Acceptance:** a 500-row patient file imports with a clear error report for bad rows; a re-run does
not duplicate; imported patients appear in the registry and can be booked; the export opens in
Excel with Arabic text intact; no PHI leaks to unauthorized users.

**Effort:** 8–12 dev-days. **Depends on:** F-32 (clinic scope), F-03. **Risk:** bulk import is the
highest-volume source of bad data; preview + rejection report are mandatory.

### F-34 — Patient self-service and online booking (evaluate)

**Outcome:** patients can request/book appointments and complete intake without calling, reducing
receptionist load.

**Scope (only if the pilot asks and the economics work)**

- Public booking page per clinic: service, doctor, available slot, name/phone, confirmation.
- Intake form before the visit; medical history updates; consent forms.
- Reschedule/cancel link in the WhatsApp message; no login required (signed token).
- Abuse protection: rate limit, CAPTCHA, phone verification; no PHI beyond the booking.
- Staff approval mode for the pilot (request → receptionist confirms) rather than instant booking.
- Do **not** build a full patient portal or native app yet.

**Acceptance:** a patient can request an appointment from a phone; the request appears in the
schedule board for confirmation; duplicate requests are blocked; the link expires; no PHI is exposed
in the URL.

**Effort:** 10–15 dev-days. **Depends on:** F-10, F-12. **Risk:** public endpoints attract abuse;
start with request-only and a manual confirm.

### F-35 — Insurance, e-invoicing, inventory (evaluate only)

**Outcome:** a decision framework, not code, until the market and legal requirements are clear.

**Scope**

- Interview 5 clinics and the pilot's accountant: do they need insurance claims, tax/e-invoicing,
  or inventory/sterilization logs?
- If insurance is needed: pre-authorization, coverage %, claim status, patient share, payer master.
- If e-invoicing is needed: legal format, sequential numbers, tax lines, QR, submission API.
- If inventory is needed: materials, batches, expiry, stock movements, procedure consumption.
- Write an ADR with cost, risk, and go/no-go; do not build speculatively.

**Acceptance:** a written decision with evidence; no code until approved.

**Effort:** 2–5 dev-days (research), then a separate project if approved. **Depends on:** paying
customers who ask.

---

## 14. Cross-cutting workstreams

These run alongside every phase; they are not a separate phase.

### 14.1 Security and privacy

- **Secrets:** no secrets in git; user-secrets in Development, environment/vault in Production;
  rotate the SQL SA password and Grafana/Seq credentials; document rotation.
- **Transport:** TLS everywhere; HSTS; secure cookies if/when the BFF is introduced.
- **Authorization:** permission matrix; every endpoint tested; fail-closed; no client-supplied
  clinic/doctor/patient ids trusted.
- **PHI:** encrypt backups and image storage; restrict image access; no PHI in logs, audit summaries,
  or notification bodies; retention/deletion policy.
- **Uploads:** size limits, MIME/magic checks, generated filenames, malware scanning where available.
- **Dependencies:** `dotnet list package --vulnerable` in CI; patch the current
  `System.Security.Cryptography.Pkcs` advisory.
- **Headers:** CSP, `X-Content-Type-Options`, `Referrer-Policy`, frame ancestors; test with a
  browser security scan.
- **Rate limiting:** keep the auth policy; add sensible limits for public booking/import endpoints.
- **Legal:** privacy policy, patient consent, data-processing agreement, retention policy; do not
  claim HIPAA/GDPR compliance without review.

### 14.2 Quality and testing

- **Unit:** domain invariants, money/FX/commission rounding, tooth validation, availability,
  permissions.
- **Application:** handler tests with in-memory/SQLite where appropriate; authz tests for every
  handler; cache invalidation tests.
- **Integration:** Testcontainers SQL; concurrency (double booking, double payment, concurrent
  invoice numbering); migrations; tenant isolation.
- **Web:** bUnit for critical components (schedule board, odontogram, invoice print, ledger);
  Playwright for 5 E2E flows (login → book → treat → invoice → pay; refresh session; role denial;
  print; restore demo).
- **Ops:** backup restore drill, migration rollback, load test for calendar/dashboard, failure
  injection for outbox/jobs.
- **CI gates:** build API + Web, unit tests, integration tests, migration test, Web tests, E2E smoke,
  dependency scan, warning budget (no new warnings).
- **Coverage:** do not chase a number; require tests for money, authorization, and state machines.

### 14.3 Localization and UX

- Resource-based strings; persisted language/theme; localized dates/numbers/currency.
- Arabic RTL print CSS for invoices, receipts, plans, statements, day sheets.
- Accessibility: labels, focus states, keyboard navigation for the schedule board.
- Empty states, loading skeletons, error messages that tell the user what to do next.
- Mobile/tablet layout for the calendar and patient file; clinics use tablets at the chair.
- A consistent design system; the existing CSS is a good start — do not add a UI framework that
  fights it.

### 14.4 Observability and operations

- Business metrics: appointments created/completed/no-show, invoices issued/paid, reminders sent,
  recalls booked, imports, backups.
- Alerts: 5xx rate, job failures, outbox backlog, backup staleness, disk space, certificate expiry,
  DB connection failures.
- Correlation id in logs; no PHI; structured Serilog properties.
- Health endpoints for Web and API; a status page for the pilot.
- Runbooks: deploy, rollback, restore, rotate secrets, onboard clinic, incident response.
- Version endpoint/build info in the UI footer for support.

### 14.5 Performance and data

- Indexes: patients (last/first name, phone), appointments `(DoctorId, ScheduledDateTime)`,
  `(ScheduledDateTime, Status)`, invoices `(PatientId, LifecycleStatus)`, `(ClinicId, IssuedAtUtc)`,
  payments `(InvoiceId, PaidAtUtc)`, audit `(ClinicId, OccurredAtUtc)`.
- Pagination everywhere; no unbounded `ToListAsync` on clinic-wide data.
- `AsNoTracking` for reads; projections instead of full entities.
- Cache only stable reads; short TTL for dashboards; invalidate by tag.
- Avoid N+1 in the calendar/ledger; test query counts.
- Archive old audit/notification rows if volume grows; do not archive financial records.

---

## 15. Data model changes (summary)

| Entity | Change | Phase |
|---|---|---|
| `Clinic` | New: profile, branding, timezone, currencies, invoice prefix | 0 |
| `ClinicUser` | New: membership/role per clinic | 0 |
| Tenant tables | `ClinicId` on Patient, Doctor, DentalService, Appointment, TreatmentRecord, Invoice, InvoiceLine, Payment | 0 |
| `AppUser` | `IsActive`, `MustChangePassword`, `DeactivatedAtUtc` | 0 |
| `Invoice` | `InvoiceNumber`, `IssuedAtUtc`, `IssuedByUserId`, `VoidedAtUtc`, `VoidReason`, `CurrencyCode`, `ExchangeRateToBase`, `AmountBase` | 0 / 3 |
| `InvoiceLine` | `ServiceId?`, `TreatmentRecordId?`, `ToothNumber?`, `DiscountAmount?` | 0 / 2 |
| `AuditLog` | New: append-only sensitive-action trail | 0 |
| `Appointment` | `ConfirmationStatus`, `ConfirmedAtUtc`, `ReminderSentAtUtc`, `ReminderChannel`, `ChairId?` | 1 |
| `ClinicWorkingHours` / `ClinicHoliday` / `DoctorAvailability` | New: real availability | 1 |
| `PatientRecall` | New: recall type, due date, status | 1 |
| `WaitlistEntry` | New: desired window, priority, status | 1 |
| `Patient` | `ReferralSource?`, `PreferredLanguage?`; duplicate indexes | 1 |
| `DentalService` | `DurationMinutes`, `Category`, `IsToothSpecific`, `RequiresLab`, `Color`, `Code` | 1 |
| `PatientTooth` | New: condition, surfaces, notes, per-tooth state | 2 |
| `TreatmentPlan` / `TreatmentPlanItem` | New: plan/estimate/acceptance | 2 |
| `TreatmentRecord` | `Status`, `ServiceId?`, `ClinicalNotes?`, `PerformedAtUtc?`, nullable `ToothNumber` | 2 |
| `MedicalHistory` / `PatientAllergy` | Pregnancy, BP, bleeding/asthma/epilepsy, structured allergies | 2 |
| `PatientImage` | New: kind, storage key, tooth/treatment link | 2 |
| `Payment` | `Method`, `ReferenceNumber?`, `PayerName?`, `ReceivedByUserId`, `CurrencyCode`, `AmountBase` | 3 |
| `PaymentPlan` / `Installment` | New: installment schedule | 3 |
| `ExchangeRate` | New: effective-dated FX history | 3 |
| `CommissionRule` / `CommissionPayout` | New: effective-dated commission + finalized payout | 3 |
| `CashSession` / `Expense` | New: daily close and expenses | 3 |
| `LabCase` | New: lab workflow | 3 |
| `Notification` / `OutboxMessage` | New: reliable side effects and in-app feed | 1 |
| `ImportJob` | New: import status/error report | 4 |

---

## 16. API surface additions (summary)

| Area | Endpoints | Phase |
|---|---|---|
| Staff | `GET/POST /staff-users`, `PUT /staff-users/{id}/roles`, `POST /{id}/deactivate`, `POST /{id}/reset-password`, `POST /doctors/{id}/link-user`, `DELETE /doctors/{id}/link-user` | 0 |
| Clinic | `GET/PUT /clinic/settings`, `POST /clinic/logo`, `GET/PUT /clinic/working-hours`, `GET/POST/DELETE /clinic/holidays` | 0 / 1 |
| Audit | `GET /audit?from&to&actor&entity&action` | 0 |
| Invoices | `POST /invoices/{id}/void`, `GET /invoices/{id}/print`, `GET /invoices/{id}/receipt` | 0 |
| Appointments | `GET /appointments/calendar`, `GET /appointments/availability`, `PATCH /{id}/confirm`, `PATCH /{id}/decline`, `POST /{id}/remind` | 1 |
| Recalls | `GET/POST /recalls`, `PATCH /recalls/{id}` | 1 |
| Waitlist | `GET/POST /waitlist`, `PATCH /waitlist/{id}` | 1 |
| Patients | `POST /patients/{id}/merge`, `GET /patients/duplicates`, `GET /patients/{id}/odontogram`, `PUT /patients/{id}/teeth/{tooth}` | 1 / 2 |
| Plans | `GET/POST /treatment-plans`, `PUT /{id}`, `POST /{id}/present`, `POST /{id}/accept`, `POST /{id}/decline`, `POST /{id}/items`, `DELETE /{id}/items/{itemId}` | 2 |
| Images | `POST /patients/{id}/images`, `GET /patients/{id}/images`, `GET /patients/{id}/images/{imageId}/content`, `DELETE /patients/{id}/images/{imageId}` | 2 |
| Billing | `GET /billing/patients/{id}/balance`, `/ledger`, `GET /billing/outstanding`, `GET /billing/aging`, `GET /billing/statements/{patientId}` | 3 |
| Payments | `POST /invoices/{id}/refunds`, `GET /payments?method&from&to` | 3 |
| FX | `GET/POST /settings/exchange-rates`, `GET /settings/exchange-rates/current` | 3 |
| Reports | `GET /reports/financial-summary`, `/reports/doctors`, `/reports/services`, `/reports/no-shows`, `/reports/cash-close` | 3 |
| Commission | `GET/POST /commission-rules`, `GET /reports/commission`, `POST /reports/commission/finalize` | 3 |
| Lab | `GET/POST /lab-cases`, `PATCH /lab-cases/{id}/status` | 3 |
| Platform | `GET/POST /platform/clinics`, `POST /platform/clinics/{id}/suspend`, `POST /platform/clinics/{id}/admin` | 4 |
| Import/export | `POST /imports/{entity}`, `GET /imports/{id}`, `GET /imports/{id}/errors`, `GET /exports/{entity}` | 4 |

All additions follow the existing conventions: `api/v{version}`, `[Authorize(Policy=…)]`,
`Result<T>`, ProblemDetails, pagination, and a scope check in the handler.

---

## 17. Test strategy by phase

| Phase | Must-have tests |
|---|---|
| 0 | Tenant isolation (read/write/list); doctor-user link; session rehydration/revocation; invoice numbering concurrency; print totals; audit writes; backup restore drill; concurrent `CreateInvoice` |
| 1 | Calendar query correctness/timezone; availability rules; reminder idempotency; recall generation/dedupe; waitlist matching; duplicate detection/merge integrity |
| 2 | Odontogram state/history; FDI validation (single source); plan totals; performed-only billing; no double-billing; medical alert evaluator; image upload security/authz |
| 3 | Ledger vs SQL; aging buckets; refund/void/credit; installment allocation; FX snapshot/rounding; dashboard reconciliation; commission effective dates/refunds; cash close variance; lab overdue |
| 4 | Cross-tenant matrix; import preview/error report/idempotency; export completeness; public booking abuse/rate limits |

Add these to CI gradually; do not wait for the end. Every bug fixed in production gets a regression
test.

---

## 18. 90-day execution plan (two developers)

| Week | Developer A | Developer B | Exit criteria |
|---|---|---|---|
| 1 | F-01 deployment/CI | F-08 debt cleanup | Clean deploy; no vulnerable package; tests green |
| 2 | F-02 session resilience | F-03 clinic foundation (schema) | Refresh works; tenant schema merged |
| 3 | F-03 filters/backfill/settings | F-04 staff admin (API) | Isolation tests pass; staff API ready |
| 4 | F-04 staff UI | F-05 invoice numbering/print | Owner can onboard staff; numbered invoice prints |
| 5 | F-06 audit log | F-05 print polish/RTL | Audit page; invoice/receipt approved by pilot |
| 6 | F-07 backup/restore | F-09 demo data/onboarding | Restore drill passes; demo clinic ready |
| 7 | F-10 schedule board | F-11 availability | Receptionist runs a day from the board |
| 8 | F-10 drag/quick actions | F-12 WhatsApp click-to-chat | Confirm/reschedule/remind in one screen |
| 9 | F-13 recall | F-15 quick intake/dedupe | Recall list in use; duplicate warning live |
| 10 | F-14 waitlist | F-16 service catalog | Cancellation fill works; durations/categories live |
| 11 | F-17 preferences/localization | F-18 notifications | Refresh keeps language/theme; bell works |
| 12 | Pilot onboarding + training | Metrics baseline + stabilization | First clinic live; go/no-go dashboard |

This is aggressive but feasible if the team is two seniors and the pilot clinic is available for
weekly feedback. If one developer, double the calendar time and cut F-14/F-18 from the first release.

---

## 19. Definition of Done (per feature)

- [ ] Business outcome and metric are written down.
- [ ] Server-authoritative; no client-supplied money/ids/roles.
- [ ] Controller policy + handler permission/ownership check; authz matrix updated.
- [ ] Clinic-scoped; cache key includes clinic/scope; tag invalidation on mutation.
- [ ] Audit event for sensitive actions.
- [ ] Additive migration + `MigrationTests` green.
- [ ] Unit + application + integration tests; Web/E2E test for a user-visible flow.
- [ ] Arabic/RTL checked; print checked if applicable.
- [ ] Telemetry/metric/alert added where failure matters.
- [ ] Docs/runbook/README updated; no new warnings.
- [ ] Demoed to the pilot user who will actually use it.
- [ ] Rollback plan: feature flag or additive revert; backup before the migration.

---

## 20. Decisions required before Phase 0

Defaults are recommendations; the owner should confirm or change them.

| ID | Decision | Recommended default | Why it blocks |
|---|---|---|---|
| D-01 | Launch market and pilot clinic | One Arabic-speaking, 2–3 chair clinic; cash + USD | Timezone, currency, tax, wording, support |
| D-02 | Ledger/base currency | USD base; local currency display; historical FX snapshots | Invoice/payment schema and reports |
| D-03 | WhatsApp approach | Click-to-chat first; Cloud API after the pilot proves volume | Scope and provider cost |
| D-04 | Tenancy timing | Seam now (`ClinicId`), platform UI after 3 paying clinics | Data model and migration risk |
| D-05 | Image storage | Encrypted local volume for pilot; S3-compatible for scale | Security/ops and F-23 |
| D-06 | Invoice numbering rules | `INV-YYYY-000001`, per clinic/year, gaps allowed unless legal says otherwise | Legal/accounting and F-05 |
| D-07 | Commission basis | % of collected revenue, lab cost deducted, effective-dated | F-29 and doctor contracts |
| D-08 | Hosting | On-prem/regional VPS with Docker Compose + off-site backup | Connectivity, support, F-01/F-07 |
| D-09 | Pilot pricing | Free 60 days + setup fee, then $149/month | Sales motion |
| D-10 | Phase 1 scope | No AI, insurance, inventory, patient portal, or native app | Focus |

---

## 21. Parking lot (deliberately deferred)

- Periodontal charting (pocket depths, bleeding, mobility) — natural extension of the odontogram.
- Prescriptions and drug interaction checks — clinical/legal scope.
- Orthodontic case management (cephalometric, bracket tracking).
- Insurance claims and pre-authorization.
- Inventory, batches, expiry, sterilization logs.
- Full accounting, general ledger, payroll, tax engine.
- Patient mobile app and full portal.
- Online instant booking (request-only first).
- Multi-branch analytics and chair-level utilization (after chairs are modeled).
- AI X-ray analysis and clinical decision support.
- E-invoicing/QR compliance per market.
- SMS/email marketing automation.
- Telemedicine.
- Gift cards, deposits, memberships.
- Referral partner tracking and marketing attribution beyond a simple source field.
- NPS/satisfaction surveys.

Each item needs a paying customer, a legal requirement, or a clear strategic reason before it enters
the roadmap.

---

## 22. Sources and evidence

- **Code inspection:** `src/DentalClinic.*`, `tests/`, `docs/product/*`, `Dockerfile`,
  `docker-compose.yml`, `.github/workflows/build-and-test.yml`.
- **Test run:** `DentalClinic.Domain.UnitTests` 113 passed; `DentalClinic.Application.UnitTests`
  61 passed. Docker-dependent suites not run. The repository contains 483 test attributes across
  five test projects.
- **Build note:** `dotnet build DentalClinic.slnx` reached file-lock failures because the API and
  Web were already running; it reported 93 warnings (baseline) and no compile errors before the
  copy failures. Re-run with the apps stopped to confirm.
- **Already fixed since `AUDIT.md`:** demo quick-login passwords, invoice authorization/ownership,
  request-object logging/secret redaction, production exception-detail hiding, and the reschedule
  serializable transaction. Do not schedule those again.
- **Still open from `AUDIT.md`:** `CreateInvoice` duplicate race, `IdentityService` throw on a
  deleted user, `InvoiceLine` soft-delete comment/policy, `ErrorKind.Unauthorized` mapping, dead
  `AppSettings`, stale Prometheus target, client-side dashboard math, doctor-user link gap,
  `System.Security.Cryptography.Pkcs` advisory, and the Web deployment gap.
- **In-progress working tree:** `Dockerfile` and `docker-compose.yml` add a Web container but the
  service sets `ASSEMBLY_NAME` (unused; Dockerfile reads `APP_PATH`) and
  `ApiSettings__BaseUrl` (unused; `Program.cs` reads `ApiBaseUrl`). F-01 corrects this; do not
  deploy the current compose file as-is.
- **Older docs:** `docs/product/PRD.md` and `MVP-ROADMAP.md` are dated 2026-10-12 and based on
  commit `1369f0a`; several findings are stale. Use them for intent, not current state. `README.md`
  still claims 380/380 tests and stale ports.
