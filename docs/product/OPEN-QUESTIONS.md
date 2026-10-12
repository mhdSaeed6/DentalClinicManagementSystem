# DentalClinic — Open Questions & Decisions Register

| Field | Value |
|---|---|
| Document status | Draft for stakeholder review |
| Version | 1.0 |
| Date | 2026-10-12 |
| Companion | [`PRD.md`](./PRD.md), [`MVP-ROADMAP.md`](./MVP-ROADMAP.md) |
| Purpose | Collect every decision that blocks or changes the MVP, with the relevant context and a recommended default |

> **How to use this document.** Each question states **why it matters**, **what we assume today**,
> **options**, a **recommendation**, its **blocking phase**, and **owner**. Where a decision is needed
> before code changes, it is marked **BLOCKING**. Nothing here should be treated as decided until the
> Decision/Date/Owner fields are filled.

Legend: **BLOCKING** = must be answered before the phase can start · **IMPORTANT** = answer before
launch · **LOW** = can be answered during the pilot.

---

## Priority at a glance

| ID | Question | Blocks | Severity |
|---|---|---|---|
| Q1 | Target market, currency, timezone, locale defaults | Phase 1, 5, 8 | IMPORTANT |
| Q2 | Hosting model for the Blazor Web app | Phase 9 | IMPORTANT |
| Q3 | Adopt an `InProgress` appointment (open visit) state? | Phase 4/6 | BLOCKING |
| Q4 | Legacy invoice backfill & migration-history policy | Phase 0/1/5/6 | BLOCKING |
| Q5 | Secret management & credential rotation | Phase 0 | BLOCKING |
| Q6 | Notification channel (email/SMS/WhatsApp) for invitations & reminders | Phase 2/9 | IMPORTANT |
| Q7 | Refunds / credit notes / e-invoicing / tax | Deferred (INV-008) | IMPORTANT |
| Q8 | Legal & regulatory posture per market | Launch claims | IMPORTANT |
| Q9 | Patient data retention & deletion policy | Launch | IMPORTANT |
| Q10 | CI strategy for Docker/SQL integration tests | Phase 0 onwards | BLOCKING |

---

## Q1 — Target market, currency, timezone and locale defaults

**Status:** OPEN · **Blocks:** Phases 1, 5, 8 · **Severity:** IMPORTANT

**Why it matters.** The PRD proposes per-clinic currency and timezone (`INV-009`, `SET-001`, `SET-003`),
but the current code hardcodes `USD` and mixes UTC/local time `[CODE]`. Defaults and formatting rules
must be known to build settings correctly, and tax/legal behavior (Q7/Q8) depends on the market.

**Assumptions today** `[INFERRED]`: Arabic-speaking market; pilot clinic not yet named; currency
unknown; timezone unknown.

**Options.**
- (a) Ship per-clinic settings with no global default (operator sets on provisioning).
- (b) Pick one launch market and hardcode sensible defaults, still overridable.
- (c) Support multiple currencies with an FX table (rejected for MVP — adds accounting scope).

**Recommendation.** (a) with a documented placeholder default; decide the launch market before Phase 8
so settings UI defaults and number/date formats are right.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q2 — Hosting model for the Blazor Web app

**Status:** OPEN · **Blocks:** Phase 9 · **Severity:** IMPORTANT

**Why it matters.** Dockerfile/compose currently build only the API (D-13); the Blazor Web App has no
deployment path `[CODE]`. Session handling (`ClinicSessionState`, D-14) interacts with hosting.

**Options.**
- (a) Containerize the Web app alongside the API (same compose); simplest for the pilot.
- (b) Host the Web app on IIS/Kestrel behind the API on the same origin.
- (c) Static/WebAssembly front end calling the API (larger change — not recommended now).

**Recommendation.** (a): add a Web container and co-deploy with the API for the pilot; revisit scaling later.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q3 — Adopt an `InProgress` appointment (open visit) state?

**Status:** OPEN · **Blocks:** Phases 4 and 6 · **Severity:** BLOCKING

**Why it matters.** The current appointment lifecycle is `Scheduled → Completed/Cancelled/NoShow` `[CODE]`.
The workflow "open a visit, record treatments, then create a draft" implies an *open* state while the
patient is in the chair. Without it, treatment entry must remain allowed on a `Scheduled` appointment,
or completion becomes the trigger (which conflicts with "cancel/no-show" semantics).

**Options.**
- (a) Add `InProgress` between `Scheduled` and `Completed`; treatments attached to an `InProgress` visit.
- (b) No new state: allow treatments on `Scheduled`; `Completed` only marks the visit done, and billing
  is a separate explicit step (`WFL-002`).
- (c) Auto-transition `Scheduled → InProgress` on first treatment saved (implicit).

**Recommendation.** (b) for MVP — smallest change, no new state, and it preserves the rule that
completing never bills. Adopt (a) later if reception/clinical staff need to see "in progress" on the
schedule. Avoid (c): implicit transitions hide state from users.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q4 — Legacy invoice backfill & migration-history policy

**Status:** OPEN · **Blocks:** Phases 0, 1, 5, 6 · **Severity:** BLOCKING

**Why it matters.** Migration files exist but startup uses `EnsureCreatedAsync()` `[CODE]` (D-05), so a
database may have a schema with **no** `__EFMigrationsHistory`. Switching to `MigrateAsync()` and
adding tenant columns depends on knowing the real DB state. Existing invoices also have no lines,
number, or lifecycle, and must be backfilled without changing their totals (PRD F.5).

**Questions to answer.**
1. Is there **any** existing production/pilot database, or is the environment disposable? If disposable,
   we can reset cleanly (simplest).
2. If not disposable: does `__EFMigrationsHistory` exist and what does it contain?
3. Should existing invoices be backfilled as **Issued** (they represent already-charged amounts) or
   **Draft** (to allow review)? Recommendation: **Issued**, with one `InvoiceLine` "Legacy balance".
4. What `ClinicId` do legacy rows get (pilot clinic)?

**Recommendation.** Prefer a clean reset for a disposable environment. Otherwise: verify history, add a
baseline migration if needed, backfill `ClinicId` for the pilot clinic, mark legacy invoices `Issued`,
and create one legacy balance line each.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q5 — Secret management & credential rotation

**Status:** OPEN · **Blocks:** Phase 0 · **Severity:** BLOCKING

**Why it matters.** A JWT secret, a SQL Server SA password and demo user passwords are committed `[CODE]`
(D-07). They must be rotated and moved to a secret store (user secrets in dev, environment variables or
a secrets manager in deployment). Rotating invalidates existing tokens.

**Options.**
- (a) Env vars + user-secrets; documented rotation runbook.
- (b) A secrets manager (e.g., Azure Key Vault / AWS Secrets Manager / HashiCorp Vault).

**Recommendation.** (a) for the pilot; note (b) as the target for multi-clinic SaaS.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q6 — Notification channel for invitations & reminders

**Status:** OPEN · **Blocks:** Phase 2 (invitations) and Phase 9 (reminders) · **Severity:** IMPORTANT

**Why it matters.** `AUTH-003` (invite/set-password) and `NOT-002` (reminders) need a delivery channel.
If none is chosen, MVP uses **manual account creation with a temporary password + forced change** and
**no reminders** (PRD non-goals).

**Options.** (a) Email; (b) SMS; (c) WhatsApp Business; (d) none for MVP (manual/out-of-band).

**Recommendation.** (d) for MVP — keep scope small; add the chosen channel post-pilot.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q7 — Refunds, credit notes, tax & e-invoicing

**Status:** OPEN (deferred) · **Blocks:** full `INV-008` · **Severity:** IMPORTANT

**Why it matters.** Some markets legally require sequential numbered invoices, VAT lines, credit notes
and e-invoicing formats. Building the wrong shape now is costly. MVP ships **void only** for draft/unpaid
invoices (PRD K).

**Options.** (a) MVP void-only, revisit after legal review; (b) add tax fields now; (c) full e-invoicing
integration (rejected for MVP).

**Recommendation.** (a). Design `InvoiceLine`/`Invoice` so tax/discount can be added without a rewrite
(e.g., a discount field per line and a nullable tax rate), but do not implement tax engines.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q8 — Legal & regulatory posture per market

**Status:** OPEN · **Blocks:** any compliance claim · **Severity:** IMPORTANT

**Why it matters.** The PRD explicitly avoids asserting GDPR/HIPAA/local compliance until reviewed.
Data residency, patient-record retention, and (in some jurisdictions) medical-device/software rules may apply.

**Action.** Obtain a legal review for the launch market before making any compliance or privacy claim.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q9 — Patient data retention & deletion policy

**Status:** OPEN · **Blocks:** launch readiness · **Severity:** IMPORTANT

**Why it matters.** The system supports soft delete but there is no retention/erasure policy. Financial
history must generally be preserved even when a patient is archived (PRD `PAT-004`). "Right to
erasure" vs. financial-record retention must be reconciled legally.

**Recommendation.** Define retention windows with the clinic; keep financial records; anonymize clinical
notes on valid erasure requests; document the process. No destructive delete of issued invoices.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Q10 — CI strategy for Docker/SQL integration tests

**Status:** OPEN · **Blocks:** Phases 0–10 (test gates) · **Severity:** BLOCKING

**Why it matters.** The repo has `Testcontainers.MsSql`, Respawn and `WebApplicationFactory` tests `[CODE]`.
CI runs `dotnet test` `[CODE]` but it is unverified whether CI provides Docker/SQL, so integration
tests may not execute. All phase acceptance gates depend on these tests actually running.

**Options.**
- (a) CI provides Docker (GitHub-hosted runners do); run the full suite including Testcontainers.
- (b) Add a SQL Server service container to the workflow and point tests at it.
- (c) Split unit vs integration tests; run integration on a scheduled/self-hosted job.

**Recommendation.** (a)/(b): confirm the current workflow, make Docker/SQL available, and require the
full suite green as a merge gate. Capture the actual pass count to replace the README's unverified
"380/380" `[README]`.

**Decision:** ____ · **Owner:** ____ · **Date:** ____

---

## Additional open items (not phase-blocking)

| ID | Question | Notes |
|---|---|---|
| Q11 | Should receptionists see invoice amounts and register payments (PRD C.1 assumes yes)? | Confirm the permission matrix before Phase 2. |
| Q12 | Are doctors linked to user accounts in the pilot (DOC-001 optional link)? | Affects "my visits" views. |
| Q13 | Should the notification bell (`D-16`) be implemented or removed? | Recommend remove for MVP unless Q6 selects a channel. |
| Q14 | Is one clinic per user enough for the pilot, or do some users need multiple clinics? | MVP assumes one (`TEN-004`). |
| Q15 | Expected patient volume/concurrency for the pilot? | Validates the PRD I.5 performance hypotheses. |
| Q16 | Who is the platform administrator operationally (the operator) and how do they authenticate? | MVP: manual provisioning by the operator. |

---

*End of open questions. Questions marked BLOCKING (Q3, Q4, Q5, Q10) and the launch-critical Q1, Q7, Q8,
Q9 must be resolved before the corresponding phases begin or before launch. This list is expected to
evolve as the pilot provides evidence.*
