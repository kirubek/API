-- Script to update leave request statuses to 'Approved' for all finalized leave plans
-- This script should be run against the BaseOps_API database

-- Update all leave requests associated with finalized plans to 'Approved' status
UPDATE annual_leave_requests
SET 
    status = 'Approved',
    updated_at = CURRENT_TIMESTAMP,
    updated_by = (
        SELECT finalized_by_user_id 
        FROM annual_leave_plans 
        WHERE annual_leave_plans.id IN (
            SELECT annual_leave_plan_id 
            FROM annual_leave_plan_entries 
            WHERE annual_leave_plan_entries.annual_leave_request_id = annual_leave_requests.id
        )
        AND annual_leave_plans.status = 'Finalized'
        LIMIT 1
    )
WHERE id IN (
    SELECT annual_leave_request_id 
    FROM annual_leave_plan_entries 
    WHERE annual_leave_plan_id IN (
        SELECT id 
        FROM annual_leave_plans 
        WHERE status = 'Finalized'
    )
)
AND status != 'Approved';

-- Display the number of updated records
SELECT COUNT(*) as 'Updated Leave Requests' 
FROM annual_leave_requests 
WHERE id IN (
    SELECT annual_leave_request_id 
    FROM annual_leave_plan_entries 
    WHERE annual_leave_plan_id IN (
        SELECT id 
        FROM annual_leave_plans 
        WHERE status = 'Finalized'
    )
)
AND status = 'Approved';
