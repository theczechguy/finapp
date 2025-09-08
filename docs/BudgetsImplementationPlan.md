# Budgets MVP — Implementation Plan (High-Level)

This plan outlines the steps to implement monthly per-category budgets with temporal (effective-dated) changes, inline editing, and clear status indicators. It avoids code-level detail and focuses on deliverables and acceptance criteria.

## 1) Scope & Objectives
- Per-category monthly budgets, no carry-over of leftovers.
- Temporal behavior: changes apply from selected month forward; past months untouched.
- In-app status and visuals: Under, Near (≥80%), Over (≥100%), No budget.
- Inline edit with scope: “This and future months” (default) or “This month only”.
- Dedicated Budgets card on Expenses monthly page; optional Manage Budgets page for bulk edits.

## 2) UX Deliverables
- Budgets card on Expenses page with:
  - Rows per category: Name, Budget, Spent, Status chip, progress bar.
  - Inline edit affordance for Budget with scope toggle and Save/Cancel.
  - Empty state: “No budgets set for this month” + actions.
- Optional Manage Budgets page (bulk edit): month picker, list of categories with editable budget values, “Copy previous month’s values”.

## 3) Data & Behavior (Conceptual)
- Effective-dated budgets per category (determine the budget for a given month by the latest change on or before that month).
- No budget state for categories without an effective entry in that month.
- Monthly spending = regular expenses applicable to the month + irregular expenses in that month.
- Base currency display only.

## 4) Status Logic & Thresholds
- Under: Spent/Budget < 80% (primary color). Show Remaining.
- Near: 80% ≤ Spent/Budget < 100% (warning color). Show Remaining.
- Over: Spent/Budget ≥ 100% (danger color). Show Over by X.
- No budget: muted label + inline Add.

## 5) Accessibility & Usability
- Progress bars include text and aria-labels; color not the only indicator.
- Edit controls labeled, scope radios grouped, aria-live for success messages.
- Responsive layout: rows stack on mobile.

## 6) Rollout & Settings
- Feature flag: Budgets card visibility toggle.
- Manage Budgets page behind the same flag (optional for MVP).
- No notifications/email in MVP.

## 7) Milestones & Tasks
1. Design finalization
   - Confirm card placement, labels, thresholds, and copy. (Done)
2. Data modeling (conceptual → implementation)
   - Introduce effective-dated budgets per category.
   - Ensure monthly spending computation aligns with existing views.
3. Expenses page integration (Budgets card)
   - Read effective budgets + actual spend for selected month.
   - Render rows with status chips and progress bars.
   - Inline edit flow with scope toggle + validations.
4. Optional Manage Budgets page
   - Month selection, editable table, “Copy previous month’s values”.
5. QA & polish
   - Edge cases: zero budget, tiny budgets, categories with no spend, large numbers.
   - Accessibility checks and mobile layout.
6. Rollout
   - Enable feature flag; soft launch.

## 8) Acceptance Criteria
- Users can set a budget for a category for a month.
- Users can update a budget with scope “This month only” or “This and future months (default)”.
- Budgets do not carry over leftover amounts between months.
- Budgets card shows status for every category with clear Under/Near/Over/No budget states.
- Progress bars and text match defined thresholds and copy.
- Mobile layout remains readable and operable.

## 9) Risks & Mitigations
- Confusion about scope of changes → Clear helper text explaining effective dates.
- Performance with many categories → Query optimizations and minimal over-fetching.
- UX complexity in bulk edit → Keep optional page simple; defer advanced presets/templates.

## 10) Out of Scope (MVP)
- Budget carry-over, forecasting, notifications, multi-currency.

## 11) Next Steps (Execution Order)
1) Implement effective-dated budgets data support.
2) Add Budgets card to Expenses page with read-only display.
3) Add inline edit with scope and validations.
4) (Optional) Implement Manage Budgets page.
5) QA pass and enable feature flag.
