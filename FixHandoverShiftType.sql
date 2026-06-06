-- Convert existing ShiftType string values to int values
UPDATE handovers SET ShiftType = 1 WHERE ShiftType = 'Day';
UPDATE handovers SET ShiftType = 2 WHERE ShiftType = 'Night';
UPDATE handovers SET ShiftType = 3 WHERE ShiftType = 'Evening';
