-- Creating tables for holding data and relationships:

CREATE TABLE Departments (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	Name varchar(100) NOT NULL,
	AppointmentCost int NOT NULL DEFAULT 1000,
	
	Created DATETIME NOT NULL DEFAULT GETDATE(),
	CreatedBy varchar(128) NOT NULL DEFAULT 'N/A',
	Modified DATETIME NOT NULL DEFAULT GETDATE(),
	ModifiedBy varchar(128) NOT NULL DEFAULT 'N/A',
	IsActive BIT NOT NULL DEFAULT 1,
);

CREATE TABLE Patients (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	Name varchar(100) NOT NULL, 
	Password varchar(100) NOT NULL, 
	Gender SMALLINT NOT NULL, 
	Email varchar(100) NOT NULL, 
	Address varchar(100) NOT NULL, 
	Phone int NOT NULL,

	Created DATETIME NOT NULL DEFAULT GETDATE(),
	CreatedBy varchar(128) NOT NULL DEFAULT 'N/A',
	Modified DATETIME NOT NULL DEFAULT GETDATE(),
	ModifiedBy varchar(128) NOT NULL DEFAULT 'N/A',
	IsActive BIT NOT NULL DEFAULT 1,
);

CREATE TABLE Doctors (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	Name varchar(100) NOT NULL, 
	Password varchar(100) NOT NULL, 
	DepartmentID int NOT NULL FOREIGN KEY REFERENCES Departments(ID) ON DELETE CASCADE,
	Available BIT NOT NULL DEFAULT 1, 
	Salary int NOT NULL DEFAULT 0,
	
	Created DATETIME NOT NULL DEFAULT GETDATE(),
	CreatedBy varchar(128) NOT NULL DEFAULT 'N/A',
	Modified DATETIME NOT NULL DEFAULT GETDATE(),
	ModifiedBy varchar(128) NOT NULL DEFAULT 'N/A',
	IsActive BIT NOT NULL DEFAULT 1,
);

CREATE TABLE Admins (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	Name varchar(100) NOT NULL, 
	Password varchar(100) NOT NULL,
	
	Created DATETIME NOT NULL DEFAULT GETDATE(),
	CreatedBy varchar(128) NOT NULL DEFAULT 'N/A',
	Modified DATETIME NOT NULL DEFAULT GETDATE(),
	ModifiedBy varchar(128) NOT NULL DEFAULT 'N/A',
	IsActive BIT NOT NULL DEFAULT 1,
);

CREATE TABLE TimeSlots (
	ID int NOT NULL PRIMARY KEY IDENTITY(1, 1),
	Timing varchar(50) NOT NULL UNIQUE,

	Created DATETIME NOT NULL DEFAULT GETDATE(),
	CreatedBy varchar(128) NOT NULL DEFAULT 'N/A',
	Modified DATETIME NOT NULL DEFAULT GETDATE(),
	ModifiedBy varchar(128) NOT NULL DEFAULT 'N/A',
	IsActive BIT NOT NULL DEFAULT 1,
);

CREATE TABLE Appointments (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	PatientID int NOT NULL FOREIGN KEY REFERENCES Patients(ID) ON DELETE CASCADE,
	DoctorID int NOT NULL FOREIGN KEY REFERENCES Doctors(ID) ON DELETE CASCADE, 
	TimeSlot int NOT NULL FOREIGN KEY REFERENCES TimeSlots(ID) ON DELETE CASCADE,
	Date DATE NOT NULL,
	Completed BIT NOT NULL DEFAULT 0,
	Description varchar(MAX) NOT NULL DEFAULT 'N/A',

	
	Created DATETIME NOT NULL DEFAULT GETDATE(),
	CreatedBy varchar(128) NOT NULL DEFAULT 'N/A',
	Modified DATETIME NOT NULL DEFAULT GETDATE(),
	ModifiedBy varchar(128) NOT NULL DEFAULT 'N/A',
	IsActive BIT NOT NULL DEFAULT 1,

	CONSTRAINT PatientClash UNIQUE (TimeSlot, Date, PatientID, Completed, IsActive),
	CONSTRAINT DoctorClash UNIQUE (TimeSlot, Date, DoctorID, Completed, IsActive),
);

-- Indexing for faster operations.

CREATE INDEX DepartmentActive ON Departments(IsActive);

CREATE INDEX PatientEmail ON Patients(Email);
CREATE INDEX PatientActive ON Patients(IsActive);

CREATE INDEX DoctorDepartment ON Doctors(DepartmentID);
CREATE INDEX DoctorAvailable ON Doctors(Available);
CREATE INDEX DoctorActive ON Doctors(IsActive);

CREATE INDEX AdminActive ON Admins(IsActive);

CREATE UNIQUE INDEX TimeSlotTiming ON TimeSlots(Timing);

CREATE INDEX AppointmentPatient ON Appointments(PatientID);
CREATE INDEX AppointmentDoctor ON Appointments(DoctorID);
CREATE INDEX AppointmentTimeSlot ON Appointments(TimeSlot);
CREATE INDEX AppointmentDate ON Appointments(Date);
CREATE INDEX AppointmentCompleted ON Appointments(Completed);
CREATE INDEX AppointmentActive ON Appointments(IsActive);

-- Default Time Slots, more can be added in the future.

INSERT INTO TimeSlots (Timing) VALUES ('11:30 AM to 12:30 PM');
GO
INSERT INTO TimeSlots (Timing) VALUES ('12:30 PM to 1:30 PM');
GO
INSERT INTO TimeSlots (Timing) VALUES ('1:30 PM to 2:30 PM');
GO


-- Get Number of Departments (Scalar)
CREATE PROCEDURE GetNumDepartments AS
BEGIN
	SELECT COUNT(*) FROM Departments WHERE IsActive = 1
END
GO

-- Get Number of Admins (Scalar)
CREATE PROCEDURE GetNumAdmins AS
BEGIN
	SELECT COUNT(*) FROM Admins WHERE IsActive = 1
END
GO

-- Get All Departments (Tabloid)
CREATE PROCEDURE GetAllDepartments AS 
BEGIN 
	SELECT * FROM Departments WHERE IsActive = 1
END
GO

-- Get a single department by ID (Tabloid, Singular)
CREATE PROCEDURE GetDepartmentById (@id int) AS 
BEGIN 
	SELECT TOP 1 * FROM Departments WHERE ID = @id AND IsActive = 1
END
GO


-- Get Number of Patients (Scalar)
CREATE PROCEDURE GetNumPatients AS
BEGIN
	SELECT COUNT(*) FROM Patients WHERE IsActive = 1
END
GO

-- Get All Patients (Tabloid)
CREATE PROCEDURE GetAllPatients AS 
BEGIN 
	SELECT * FROM Patients WHERE IsActive = 1
END
GO

-- Get Number of Doctors (Scalar)
CREATE PROCEDURE GetNumDoctors AS
BEGIN
	SELECT COUNT(*) FROM Doctors WHERE IsActive = 1
END
GO

-- Get Number of Dotors in a Department (Scalar)
CREATE PROCEDURE GetNumDoctorsInDepartment @deptId int AS
BEGIN
	SELECT COUNT(*) FROM Doctors WHERE DepartmentID = @deptId AND IsActive = 1
END
GO

-- Get Number of Available Doctors In A Department (Scalar)
CREATE PROCEDURE GetNumAvailableDoctorsInDepartment @deptId int AS
BEGIN
	-- Only available and active
	SELECT COUNT(*) FROM Doctors WHERE DepartmentID = @deptId AND Available = 1 AND IsActive = 1
END
GO

-- Get All Doctors in the Database (Tabloid)
CREATE PROCEDURE GetAllDoctors AS 
BEGIN 
	SELECT * FROM Doctors WHERE IsActive = 1
END
GO

-- Get ALL Available doctors in the database (Tabloid)
CREATE PROCEDURE GetAvailableDoctors AS 
BEGIN 
	SELECT * FROM Doctors WHERE Available = 1 AND IsActive = 1
END
GO

-- GetAvailableDoctors but as a function so it can be used in a nested SELECT query (adding new appointment). (Tabloid)
CREATE FUNCTION fGetAvailableDoctors() RETURNS TABLE AS 
	RETURN (SELECT * FROM Doctors WHERE Available = 1 AND IsActive = 1)
GO


-- Get ALL ppointments of a Department (Tabloid)
CREATE PROCEDURE GetDepartmentAppointments (@depId int) AS
BEGIN
	SELECT a.* FROM Appointments a JOIN Doctors doc ON a.DoctorID = doc.ID JOIN Departments dep ON doc.DepartmentID = dep.ID WHERE dep.ID = @depId AND dep.IsActive = 1 AND a.IsActive = 1 ORDER BY Completed ASC, Date DESC, TimeSlot ASC
END
GO

-- Get a specific departments appointment as a function (for nested queries) (Tabloid)
CREATE FUNCTION fGetDepartmentAppointments (@depId int) RETURNS TABLE AS 
	RETURN (SELECT a.* FROM Appointments a JOIN Doctors doc ON a.DoctorID = doc.ID JOIN Departments dep ON doc.DepartmentID = dep.ID WHERE dep.ID = @depId AND dep.IsActive = 1 AND a.IsActive = 1)
GO

-- Get all timeslots (Tabloid)
CREATE PROCEDURE GetAllTimeSlots AS 
BEGIN 
	SELECT * FROM TimeSlots
END
GO

-- Get num pending appointments (Scalar)
CREATE PROCEDURE GetNumPendingAppointments AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 0 AND IsActive = 1
END
GO

-- Get num completed appointents (Scalar)
CREATE PROCEDURE GetNumCompletedAppointments AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 1 AND IsActive = 1
END
GO

-- Get num of completed appointments by doctor (Scalar)
CREATE PROCEDURE GetNumDoctorCompletedAppointments (@doctorId int) AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 1 AND DoctorID = @doctorId AND IsActive = 1
END
GO

-- Get num of a doctor's pending appointments (Scalar)
CREATE PROCEDURE GetNumDoctorPendingAppointments (@doctorId int) AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 0 AND DoctorID = @doctorId AND IsActive = 1
END
GO

-- Get num of a doctor's patients (Scalar)
CREATE PROCEDURE GetNumDoctorPatients (@doctorId int) AS
BEGIN
	SELECT COUNT(DISTINCT PatientID) FROM Appointments WHERE DoctorID = @doctorId AND Completed = 0 AND IsActive = 1
END
GO

-- Get a doctor's patients (Tabloid)
CREATE PROCEDURE GetDoctorPatients (@doctorId int) AS
BEGIN
	SELECT DISTINCT p.* FROM Patients p INNER JOIN Appointments a ON p.ID = a.PatientID WHERE a.DoctorID = @doctorId AND p.IsActive = 1 AND a.IsActive = 1
END
GO

-- Get a count of patient's completed appointments (Scalar)
CREATE PROCEDURE GetNumPatientCompletedAppointments (@patientId int) AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 1 AND PatientID = @patientId AND IsActive = 1
END
GO

-- Get a count of patient's pending appointments (Scalar)
CREATE PROCEDURE GetNumPatientPendingAppointments (@patientId int) AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 0 AND PatientID = @patientId AND IsActive = 1
END
GO

-- Create a new patient (INSERT)
CREATE PROCEDURE CreatePatient (@name varchar(MAX), @password varchar(MAX), @gender BIT, @email varchar(MAX), @address varchar(MAX), @phone int, @queryBy varchar(128) = 'N/A') AS
BEGIN
	INSERT INTO Patients (Name, Password, Gender, Email, Address, Phone, CreatedBy) VALUES 
                (@name, @password, @gender, @email, @address, @phone, @queryBy)
END
GO

-- Delete a patient (DELETE)
CREATE PROCEDURE DeletePatient (@id int, @queryBy varchar(128) = 'N/A') AS
BEGIN
	UPDATE Patients SET IsActive = 0, Modified = GETDATE(), ModifiedBy = @queryBy WHERE ID = @id
END
GO

-- Login checks: (Tabloid, Singular)
CREATE PROCEDURE LoginPatient (@name varchar(MAX), @password varchar(MAX)) AS
BEGIN
	SELECT TOP 1 * FROM Patients WHERE Name = @name AND Password = @password AND IsActive = 1
END
GO

CREATE PROCEDURE LoginDoctor (@name varchar(MAX), @password varchar(MAX)) AS
BEGIN
	SELECT TOP 1 * FROM Doctors WHERE Name = @name AND Password = @password AND IsActive = 1
END
GO

CREATE PROCEDURE LoginAdmin (@name varchar(MAX), @password varchar(MAX)) AS
BEGIN
	SELECT TOP 1 * FROM Admins WHERE Name = @name AND Password = @password AND IsActive = 1
END
GO

-- Get all admins (Tabloid)
CREATE PROCEDURE GetAllAdmins AS 
BEGIN 
	SELECT * FROM Admins WHERE IsActive = 1
END
GO

-- Get all appointments (Tabloid)
CREATE PROCEDURE GetAllAppointments AS 
BEGIN 
	SELECT * FROM Appointments WHERE IsActive = 1 ORDER BY Completed ASC, Date DESC, TimeSlot ASC
END
GO

-- Get a patient's appointments (Tabloid)
CREATE PROCEDURE GetPatientAppointments (@patientId int) AS
BEGIN
	SELECT * FROM Appointments WHERE PatientID = @patientId AND IsActive = 1 ORDER BY Completed ASC, Date DESC, TimeSlot ASC
END
GO

-- Get a doctor's assigned appointments (Tabloid)
CREATE PROCEDURE GetDoctorAppointments (@doctorId int) AS
BEGIN
	SELECT * FROM Appointments WHERE DoctorID = @doctorId AND IsActive = 1 ORDER BY Completed ASC, Date DESC, TimeSlot ASC
END
GO

-- Mark an appointment as complete (UPDATE)
CREATE PROCEDURE MarkAppointmentComplete (@id int, @queryBy varchar(128) = 'N/A') AS 
BEGIN 
	-- If it's not complete yet, to prevent spamming.
	IF (SELECT Completed FROM Appointments WHERE ID = @id) = 0
	BEGIN
		UPDATE Appointments SET Completed = 1, Modified = GETDATE(), ModifiedBy = @queryBy WHERE ID = @id
		-- Change modified as we update the salary to add onto the department's appointment cost
		UPDATE Doctors SET Modified = GETDATE(), ModifiedBy = CONCAT('AppointmentCompletion: ', @queryBy), Salary = Salary + (SELECT dep.AppointmentCost FROM Appointments a JOIN Doctors doc ON doc.ID = a.DoctorID JOIN Departments dep ON dep.ID = doc.DepartmentID WHERE a.ID = @id) WHERE ID = (SELECT DoctorID FROM Appointments WHERE ID = @id)
	END
END
GO

-- Check appointment clash (Scalar, num. clashes)
CREATE PROCEDURE CheckAppointmentClash (@patientId int, @timeSlot int, @date DATE) AS 
BEGIN 
	SELECT COUNT(*) FROM Appointments WHERE PatientID = @patientId AND Completed = 0 AND TimeSlot = @timeSlot AND Date = @date AND IsActive = 1
END
GO

-- Get free doctors for appointment at a timeslot on a date. (Tabloid, Doctor)
-- Created as a function so it can be nested inside another function or query.
CREATE FUNCTION GetFreeDoctorsForAppointment (@departmentId int, @timeSlot int, @date DATE) RETURNS TABLE AS 
	-- Join Doctors by Appointments in his respective department and check for clashes.
	RETURN (SELECT DISTINCT doc.* FROM fGetAvailableDoctors() doc WHERE doc.DepartmentID = @departmentId AND doc.ID NOT IN (SELECT DISTINCT b.DoctorID FROM fGetDepartmentAppointments(@departmentId) b WHERE b.Completed = 0 AND b.TimeSlot = @timeSlot AND b.Date = @date))
GO

-- Get num of free doctors for a appointment (Scalar)
CREATE FUNCTION GetNumFreeDoctorsForAppointment (@departmentId int, @timeSlot int, @date DATE) RETURNS int AS
BEGIN
	-- Variable to store count.
	DECLARE @count INT;
	-- Execute function and get count.
    SELECT @count = COUNT(DISTINCT ID) FROM GetFreeDoctorsForAppointment(@departmentId, @timeSlot, @date);
    RETURN @count;
END
GO

-- Create a new appointment
CREATE PROCEDURE CreateAppointment (@patientId int, @deptId int, @timeSlot int, @date DATE, @desc varchar(MAX), @queryBy varchar(128) = 'N/A') AS 
BEGIN
	-- Insert the respective values but doctor ID will be from thefunction GetFreeDoctorsForAppointment.
	INSERT INTO Appointments (PatientID, DoctorID, TimeSlot, Date, Description, CreatedBy) 
	VALUES (@patientId, (SELECT TOP 1 ID FROM GetFreeDoctorsForAppointment(@deptId, @timeSlot, @date) ORDER BY NEWID()), @timeSlot, @date, @desc, @queryBy)
END
GO

-- Create a new department
CREATE PROCEDURE CreateDepartment (@name varchar(MAX), @appointmentCost int, @queryBy varchar(128) = 'N/A') AS 
BEGIN
	INSERT INTO Departments (Name, AppointmentCost, CreatedBy) VALUES (@name, @appointmentCost, @queryBy)
END
GO

-- Create a new doctor
CREATE PROCEDURE CreateDoctor (@name varchar(MAX), @password varchar(MAX), @deptId int, @queryBy varchar(128) = 'N/A') AS 
BEGIN
	INSERT INTO Doctors (Name, Password, DepartmentID, CreatedBy) VALUES (@name, @password, @deptId, @queryBy)
END
GO

-- Delete a doctor
CREATE PROCEDURE DeleteDoctor (@docId int, @queryBy varchar(128) = 'N/A') AS 
BEGIN
	UPDATE Doctors SET IsActive = 0, Modified = GETDATE(), ModifiedBy = @queryBy WHERE ID = @docId
END
GO

-- Create an admin
CREATE PROCEDURE CreateAdmin (@name varchar(MAX), @password varchar(MAX), @queryBy varchar(128) = 'N/A') AS 
BEGIN
	INSERT INTO Admins (Name, Password, CreatedBy) VALUES (@name, @password, @queryBy)
END
GO

-- Delete an admin
CREATE PROCEDURE DeleteAdmin (@admId int, @queryBy varchar(128) = 'N/A') AS 
BEGIN
	UPDATE Admins SET IsActive = 0, Modified = GETDATE(), ModifiedBy = @queryBy WHERE ID = @admId
END
GO

-- Delete a admin
CREATE PROCEDURE DeleteDepartment (@deptId int, @queryBy varchar(128) = 'N/A') AS 
BEGIN
	UPDATE Departments SET IsActive = 0, Modified = GETDATE(), ModifiedBy = @queryBy WHERE ID = @deptId
END
GO

-- Toggle a doctors available state (on break)
CREATE PROCEDURE ToggleDoctorAvailability (@doctorId int, @queryBy varchar(128) = 'N/A') AS
BEGIN
	-- If available then unavailable, and vice versa
	-- Modified changed as we updated Available column
    IF (SELECT Available FROM Doctors WHERE ID = @doctorId) = 1
        UPDATE Doctors SET Available = 0, Modified = GETDATE(), ModifiedBy = CONCAT('StartBreak: ', @queryBy) WHERE ID = @doctorId;
    ELSE
        UPDATE Doctors SET Available = 1, Modified = GETDATE(), ModifiedBy = CONCAT('StopBreak: ', @queryBy) WHERE ID = @doctorId;
END
GO

-- Generate a departments monthly report (Tabloid, Cursor)
CREATE PROCEDURE GenerateDepartmentsMonthlyReport AS
BEGIN
	-- Declare variables
    DECLARE @DepartmentID INT, @DepartmentName VARCHAR(100), @AppointmentCost INT, @TotalAppointments INT, @CompletedAppointments INT, @PendingAppointments INT, @TotalIncome INT, @LastAppointmentOn DATE;
    
	-- Starting from the start of this month (current date + (- date diff) = 1st of month)
	DECLARE @StartDate DATE = DATEADD(month, DATEDIFF(month, 0, GETDATE()), 0);
	-- End of this month.
    DECLARE @EndDate DATE = EOMONTH(GETDATE());


	-- Temporary report table with custom structure. Deleted after the procedure is ran.
    CREATE TABLE #tmpMonthlyReport (
        DepartmentID INT, DepartmentName varchar(100), TotalAppointments INT, 
		CompletedAppointments INT, PendingAppointments INT, TotalIncome INT, LastAppointmentOn DATE,
		INDEX DeptID NONCLUSTERED (DepartmentID)
    );

	-- Create a cursor pertaining to ID, Name and AppointmentCost from Departments which are Active.
    DECLARE department_cursor CURSOR FOR SELECT ID, Name, AppointmentCost FROM Departments WHERE IsActive = 1;

	-- Open/Allocate the cursor
    OPEN department_cursor

	-- Fetch the first element
    FETCH NEXT FROM department_cursor INTO @DepartmentID, @DepartmentName, @AppointmentCost;

	-- While it keeps on fetching or current loop index isnt null
    WHILE @@FETCH_STATUS = 0
    BEGIN
		-- Select maximum, counts and sum total income. Cases are when a condition is to be met in count.
        SELECT 
            @TotalAppointments = COUNT(a.ID), 
            @CompletedAppointments = COUNT(CASE WHEN a.Completed = 1 THEN 1 END),
            @PendingAppointments = COUNT(CASE WHEN a.Completed = 0 THEN 1 END),
            @TotalIncome = SUM(CASE WHEN a.Completed = 1 THEN @AppointmentCost ELSE 0 END),
            @LastAppointmentOn = MAX(a.Date)
        FROM Appointments a JOIN Doctors d ON a.DoctorID = d.ID 
        WHERE d.DepartmentID = @DepartmentID AND a.Date BETWEEN @StartDate AND @EndDate AND a.IsActive = 1;
		-- Check between the dates we generated.

		-- Insert in temporary table.
        INSERT INTO #tmpMonthlyReport (DepartmentID, DepartmentName, TotalAppointments, CompletedAppointments, PendingAppointments, TotalIncome, LastAppointmentOn)
        VALUES (@DepartmentID, @DepartmentName, @TotalAppointments, @CompletedAppointments, @PendingAppointments, @TotalIncome, @LastAppointmentOn);

		-- Fetch next so loop can keep on continuing.
        FETCH NEXT FROM department_cursor INTO @DepartmentID, @DepartmentName, @AppointmentCost;
    END;

	-- Deallocate and close the cursor.
    CLOSE department_cursor;
    DEALLOCATE department_cursor;

    SELECT * FROM #tmpMonthlyReport;

    DROP TABLE #tmpMonthlyReport;
END
GO

-- On doctor deletion trigger.
CREATE TRIGGER DoctorDeletion ON Doctors AFTER UPDATE AS
BEGIN
	-- If it's inactive set all its joined(respective) appointments as inactive. (Deletion)
    UPDATE a SET a.IsActive = 0 FROM Appointments a JOIN inserted i ON a.DoctorID = i.ID WHERE i.IsActive = 0;
END
GO

-- Similar logic for below.
CREATE TRIGGER DepartmentDeletion ON Departments AFTER UPDATE AS
BEGIN
    UPDATE d SET d.IsActive = 0 FROM Doctors d JOIN inserted dept ON d.DepartmentID = dept.ID WHERE dept.IsActive = 0;
END
GO

CREATE TRIGGER PatientDeletion ON Patients AFTER UPDATE AS
BEGIN
    UPDATE a SET a.IsActive = 0 FROM Appointments a JOIN inserted i ON a.PatientID = i.ID WHERE i.IsActive = 0;
END
GO