#!/bin/bash

# Script to identify and report invalid date combinations in ExpenseSchedule table
# This helps diagnose issues with existing production data

echo "Checking for invalid date combinations in ExpenseSchedule table..."

# This would need to be run against your PostgreSQL database
# Example query to identify problematic records:

echo "Run this SQL query against your production database to identify issues:"
echo ""
echo "SELECT id, start_year, start_month, start_day,"
echo "       CASE WHEN start_day > EXTRACT(days FROM (DATE_TRUNC('month', MAKE_DATE(start_year, start_month, 1)) + INTERVAL '1 month - 1 day'))"
echo "            THEN 'INVALID START DAY' ELSE 'OK' END as start_status,"
echo "       end_year, end_month, end_day,"
echo "       CASE WHEN end_year IS NOT NULL AND end_month IS NOT NULL AND end_day IS NOT NULL"
echo "                 AND end_day > EXTRACT(days FROM (DATE_TRUNC('month', MAKE_DATE(end_year, end_month, 1)) + INTERVAL '1 month - 1 day'))"
echo "            THEN 'INVALID END DAY' ELSE 'OK' END as end_status"
echo "FROM expense_schedules"
echo "WHERE (start_day > EXTRACT(days FROM (DATE_TRUNC('month', MAKE_DATE(start_year, start_month, 1)) + INTERVAL '1 month - 1 day')))"
echo "   OR (end_year IS NOT NULL AND end_month IS NOT NULL AND end_day IS NOT NULL"
echo "       AND end_day > EXTRACT(days FROM (DATE_TRUNC('month', MAKE_DATE(end_year, end_month, 1)) + INTERVAL '1 month - 1 day')));"
echo ""
echo "To fix invalid records, you can update them with valid dates:"
echo "UPDATE expense_schedules"
echo "SET start_day = EXTRACT(days FROM (DATE_TRUNC('month', MAKE_DATE(start_year, start_month, 1)) + INTERVAL '1 month - 1 day'))"
echo "WHERE start_day > EXTRACT(days FROM (DATE_TRUNC('month', MAKE_DATE(start_year, start_month, 1)) + INTERVAL '1 month - 1 day'));"
echo ""
echo "Similar query for end dates..."