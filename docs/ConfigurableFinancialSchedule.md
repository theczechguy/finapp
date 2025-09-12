# Configurable Financial Schedule – Design Document

## Scope
- Applies only to the expense dashboard.
- Regular and irregular expense entry remains calendar-based.
- No migration concerns; schema can be recreated as needed.

## User Configuration
- User can choose between calendar month or custom financial schedule.
- For custom schedule, user specifies:
  - Start date (e.g., 15th of month)
  - Schedule length is fixed at 1 month (custom financial months align with calendar months but start on a different day)

## Regular Expense Changes
- Regular expenses should allow specifying a start date (day of month), not just the month.
- This enables better alignment with custom financial schedules.

## Dashboard Logic
- Dashboard groups, summarizes, and displays expenses according to the selected schedule (calendar or custom).
- Aggregation logic uses the user’s configured boundaries for both regular and irregular expenses.
- UI clearly indicates which schedule is active.

## Implementation Notes
- Store user’s schedule preference and parameters in user settings/profile.
- Expense queries and aggregations use dynamic boundaries based on configuration.
- No changes to expense entry, recurring logic, or category management.

## Out of Scope
- Investment subsystem remains unchanged.
- No legacy data migration required.
- No changes to expense entry forms or recurring logic.

---
This document outlines the requirements for supporting configurable financial schedules in the expense dashboard. Further details can be added as implementation progresses.
