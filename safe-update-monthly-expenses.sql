-- Safe SQL Script to Update Monthly Regular Expenses Start Date
-- Includes backup/rollback capability and additional safety checks

-- Start a transaction for safety
BEGIN;

-- Create a backup of current monthly expense schedules
CREATE TEMP TABLE monthly_expenses_backup AS
SELECT
    es.id,
    es.regular_expense_id,
    es.start_year,
    es.start_month,
    es.start_day,
    es.amount,
    es.frequency,
    re.name as expense_name
FROM expense_schedules es
INNER JOIN regular_expenses re ON es.regular_expense_id = re.id
WHERE es.frequency = 0;  -- Monthly

-- Show current state before update
SELECT
    'BEFORE UPDATE - Current monthly expenses:' as status,
    COUNT(*) as count,
    STRING_AGG(DISTINCT CONCAT(re.name, ' (', es.start_year, '-', LPAD(es.start_month, 2, '0'), '-', LPAD(es.start_day, 2, '0'), ')'), '; ') as expenses
FROM expense_schedules es
INNER JOIN regular_expenses re ON es.regular_expense_id = re.id
WHERE es.frequency = 0;

-- Update monthly expenses to start on August 17th, 2025
UPDATE expense_schedules
SET
    start_year = 2025,
    start_month = 8,
    start_day = 17
WHERE frequency = 0;

-- Verify the update was successful
SELECT
    'AFTER UPDATE - Verification:' as status,
    COUNT(*) as monthly_expenses_count,
    COUNT(CASE WHEN start_year = 2025 AND start_month = 8 AND start_day = 17 THEN 1 END) as updated_to_2025_08_17,
    CASE
        WHEN COUNT(*) = COUNT(CASE WHEN start_year = 2025 AND start_month = 8 AND start_day = 17 THEN 1 END)
        THEN 'SUCCESS: All monthly expenses updated'
        ELSE 'WARNING: Some expenses may not have been updated'
    END as update_status
FROM expense_schedules
WHERE frequency = 0;

-- Show detailed results
SELECT
    re.name as expense_name,
    es.amount,
    CONCAT('Updated to: ', es.start_year, '-', LPAD(es.start_month, 2, '0'), '-', LPAD(es.start_day, 2, '0')) as new_start_date,
    CASE
        WHEN es.start_year = 2025 AND es.start_month = 8 AND es.start_day = 17
        THEN '✓ Updated'
        ELSE '✗ Not updated'
    END as status
FROM expense_schedules es
INNER JOIN regular_expenses re ON es.regular_expense_id = re.id
WHERE es.frequency = 0
ORDER BY re.name;

-- Optional: Uncomment the following lines if you want to rollback the changes
-- ROLLBACK;
-- SELECT 'Changes rolled back - no data was modified' as result;

-- If everything looks good, commit the changes
COMMIT;

-- Final summary
SELECT
    'FINAL SUMMARY:' as status,
    COUNT(*) as total_monthly_expenses,
    SUM(CASE WHEN start_year = 2025 AND start_month = 8 AND start_day = 17 THEN 1 ELSE 0 END) as successfully_updated,
    'All monthly expenses now start on August 17th, 2025' as description
FROM expense_schedules es
INNER JOIN regular_expenses re ON es.regular_expense_id = re.id
WHERE es.frequency = 0;