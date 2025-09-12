-- SQL Script to Update Monthly Regular Expenses Start Date to August 17th, 2025
-- This script finds all monthly regular expenses and sets their start date to 2025-08-17

-- First, let's see what we're working with - find all monthly expenses
SELECT
    re.id as expense_id,
    re.name as expense_name,
    es.id as schedule_id,
    es.start_year,
    es.start_month,
    es.start_day,
    es.amount,
    es.frequency
FROM regular_expenses re
INNER JOIN expense_schedules es ON re.id = es.regular_expense_id
WHERE es.frequency = 0  -- 0 = Monthly in the Frequency enum
ORDER BY re.name;

-- Update all monthly expense schedules to start on August 17th, 2025
UPDATE expense_schedules
SET
    start_year = 2025,
    start_month = 8,
    start_day = 17
WHERE frequency = 0  -- 0 = Monthly
  AND regular_expense_id IN (
      SELECT re.id
      FROM regular_expenses re
      WHERE re.id IN (
          SELECT DISTINCT regular_expense_id
          FROM expense_schedules
          WHERE frequency = 0
      )
  );

-- Verify the changes
SELECT
    re.id as expense_id,
    re.name as expense_name,
    es.id as schedule_id,
    CONCAT(es.start_year, '-', LPAD(es.start_month, 2, '0'), '-', LPAD(es.start_day, 2, '0')) as start_date,
    es.amount,
    CASE es.frequency
        WHEN 0 THEN 'Monthly'
        WHEN 1 THEN 'Quarterly'
        WHEN 2 THEN 'SemiAnnually'
        WHEN 3 THEN 'Annually'
        ELSE 'Unknown'
    END as frequency_name
FROM regular_expenses re
INNER JOIN expense_schedules es ON re.id = es.regular_expense_id
WHERE es.frequency = 0  -- Monthly expenses only
ORDER BY re.name;

-- Count how many records were updated
SELECT
    COUNT(*) as monthly_expenses_updated,
    'Monthly expenses updated to start on 2025-08-17' as description
FROM expense_schedules
WHERE frequency = 0
  AND start_year = 2025
  AND start_month = 8
  AND start_day = 17;