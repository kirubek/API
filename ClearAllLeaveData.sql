-- Remove all leave data from the database
-- This will delete all leave requests, plans, and plan entries

USE BaseOps_API;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Check what will be deleted
SELECT 'annual_leave_plan_entries' AS TableName, COUNT(*) AS Count FROM annual_leave_plan_entries;
SELECT 'annual_leave_plans' AS TableName, COUNT(*) AS Count FROM annual_leave_plans;
SELECT 'annual_leave_requests' AS TableName, COUNT(*) AS Count FROM annual_leave_requests;
GO

-- Delete all plan entries first (due to foreign key constraints)
DELETE FROM annual_leave_plan_entries;
PRINT 'Deleted all annual_leave_plan_entries';
GO

-- Delete all plans
DELETE FROM annual_leave_plans;
PRINT 'Deleted all annual_leave_plans';
GO

-- Delete all leave requests
DELETE FROM annual_leave_requests;
PRINT 'Deleted all annual_leave_requests';
GO

-- Verify deletion
SELECT 'annual_leave_plan_entries' AS TableName, COUNT(*) AS Count FROM annual_leave_plan_entries;
SELECT 'annual_leave_plans' AS TableName, COUNT(*) AS Count FROM annual_leave_plans;
SELECT 'annual_leave_requests' AS TableName, COUNT(*) AS Count FROM annual_leave_requests;
GO

PRINT 'All leave data has been successfully removed.';
