CREATE TABLE Departments (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	Name varchar(100) NOT NULL,
	AppointmentCost int NOT NULL DEFAULT 1000,
);

CREATE TABLE Patients (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	Name varchar(100) NOT NULL, 
	Password varchar(100) NOT NULL, 
	Gender SMALLINT NOT NULL, 
	Email varchar(100) NOT NULL, 
	Address varchar(100) NOT NULL, 
	Phone int NOT NULL
);

CREATE TABLE Doctors (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	Name varchar(100) NOT NULL, 
	Password varchar(100) NOT NULL, 
	DepartmentID int NOT NULL FOREIGN KEY REFERENCES Departments(ID) ON DELETE CASCADE,
	Available BIT NOT NULL DEFAULT 1, 
	Salary int NOT NULL DEFAULT 0
);

CREATE TABLE Admins (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	Name varchar(100) NOT NULL, 
	Password varchar(100) NOT NULL
);

CREATE TABLE TimeSlots (
	ID int NOT NULL PRIMARY KEY IDENTITY(1, 1),
	Timing varchar(50) NOT NULL UNIQUE,
);

CREATE TABLE Appointments (
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1), 
	PatientID int NOT NULL FOREIGN KEY REFERENCES Patients(ID) ON DELETE CASCADE,
	DoctorID int NOT NULL FOREIGN KEY REFERENCES Doctors(ID) ON DELETE CASCADE, 
	TimeSlot int NOT NULL FOREIGN KEY REFERENCES TimeSlots(ID) ON DELETE CASCADE,
	Date DATETIME NOT NULL,
	Completed BIT NOT NULL DEFAULT 0,
	Description varchar(MAX) NOT NULL DEFAULT 'N/A',
	CONSTRAINT StopClash UNIQUE (TimeSlot, Date)
);


INSERT INTO TimeSlots (Timing) VALUES ('11:30 AM to 12:30 PM'), ('12:30 PM to 1:30 PM'), ('1:30 PM to 2:30 PM');
GO

CREATE PROCEDURE GetNumDepartments AS
BEGIN
	SELECT COUNT(*) FROM Departments
END
GO

CREATE PROCEDURE GetAllDepartments AS 
BEGIN 
	SELECT * FROM Departments 
END
GO

CREATE PROCEDURE GetDepartmentById (@id int) AS 
BEGIN 
	SELECT TOP 1 * FROM Departments WHERE ID = @id
END
GO

CREATE PROCEDURE GetNumPatients AS
BEGIN
	SELECT COUNT(*) FROM Patients
END
GO

CREATE PROCEDURE GetAllPatients AS 
BEGIN 
	SELECT * FROM Patients
END
GO

CREATE PROCEDURE GetNumDoctors AS
BEGIN
	SELECT COUNT(*) FROM Doctors
END
GO

CREATE PROCEDURE GetNumDoctorsInDepartment @deptId int AS
BEGIN
	SELECT COUNT(*) FROM Doctors WHERE DepartmentID = @deptId
END
GO

CREATE PROCEDURE GetNumAvailableDoctorsInDepartment @deptId int AS
BEGIN
	SELECT COUNT(*) FROM Doctors WHERE DepartmentID = @deptId AND Available = 1
END
GO

CREATE PROCEDURE GetAllDoctors AS 
BEGIN 
	SELECT * FROM Doctors
END
GO

CREATE PROCEDURE GetAllTimeSlots AS 
BEGIN 
	SELECT * FROM TimeSlots 
END
GO


CREATE PROCEDURE GetNumPendingAppointments AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 0
END
GO

CREATE PROCEDURE GetNumCompletedAppointments AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 1
END
GO

CREATE PROCEDURE GetNumDoctorCompletedAppointments (@doctorId int) AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 1 AND DoctorID = @doctorId
END
GO

CREATE PROCEDURE GetNumDoctorPendingAppointments (@doctorId int) AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 0 AND DoctorID = @doctorId
END
GO

CREATE PROCEDURE GetNumDoctorPatients (@doctorId int) AS
BEGIN
	SELECT COUNT(DISTINCT PatientID) FROM Appointments WHERE DoctorID = @doctorId AND Completed = 0
END
GO

CREATE PROCEDURE GetDoctorPatients (@doctorId int) AS
BEGIN
	SELECT DISTINCT p.* FROM Patients p INNER JOIN Appointments a ON p.ID = a.PatientID WHERE a.DoctorID = @doctorId
END
GO

CREATE PROCEDURE GetNumPatientCompletedAppointments (@patientId int) AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 1 AND PatientID = @patientId
END
GO

CREATE PROCEDURE GetNumPatientPendingAppointments (@patientId int) AS
BEGIN
	SELECT COUNT(*) FROM Appointments WHERE Completed = 0 AND PatientID = @patientId
END
GO

CREATE PROCEDURE GetDepartmentAppointments (@depId int) AS
BEGIN
	SELECT * FROM Appointments a JOIN Doctors doc ON a.DoctorID = doc.ID JOIN Departments dep ON doc.DepartmentID = dep.ID WHERE dep.ID = @depId ORDER BY Completed ASC, Date DESC, TimeSlot ASC
END
GO

CREATE PROCEDURE CreatePatient (@name varchar(MAX), @password varchar(MAX), @gender BIT, @email varchar(MAX), @address varchar(MAX), @phone int) AS
BEGIN
	INSERT INTO Patients (Name, Password, Gender, Email, Address, Phone) VALUES 
                (@name, @password, @gender, @email, @address, @phone)
END
GO

CREATE PROCEDURE DeletePatient (@id int) AS
BEGIN
	DELETE FROM Patients WHERE ID = @id
END
GO

CREATE PROCEDURE LoginPatient (@name varchar(MAX), @password varchar(MAX)) AS
BEGIN
	SELECT TOP 1 * FROM Patients WHERE Name = @name AND Password = @password
END
GO

CREATE PROCEDURE LoginDoctor (@name varchar(MAX), @password varchar(MAX)) AS
BEGIN
	SELECT TOP 1 * FROM Doctors WHERE Name = @name AND Password = @password
END
GO

CREATE PROCEDURE LoginAdmin (@name varchar(MAX), @password varchar(MAX)) AS
BEGIN
	SELECT TOP 1 * FROM Admins WHERE Name = @name AND Password = @password
END
GO

CREATE PROCEDURE GetAllAppointments AS 
BEGIN 
	SELECT * FROM Appointments ORDER BY Completed ASC, Date DESC, TimeSlot ASC
END
GO

CREATE PROCEDURE GetPatientAppointments (@patientId int) AS
BEGIN
	SELECT * FROM Appointments WHERE PatientID = @patientId ORDER BY Completed ASC, Date DESC, TimeSlot ASC
END
GO

CREATE PROCEDURE GetDoctorAppointments (@doctorId int) AS
BEGIN
	SELECT * FROM Appointments WHERE DoctorID = @doctorId ORDER BY Completed ASC, Date DESC, TimeSlot ASC
END
GO

CREATE PROCEDURE MarkAppointmentComplete (@id int) AS 
BEGIN 
	IF (SELECT Completed FROM Appointments WHERE ID = @id) = 0
	BEGIN
		UPDATE Appointments SET Completed = 1 WHERE ID = @id
		UPDATE Doctors SET Salary = Salary + (SELECT dep.AppointmentCost FROM Appointments a JOIN Doctors doc ON doc.ID = a.DoctorID JOIN Departments dep ON dep.ID = doc.DepartmentID WHERE a.ID = @id) WHERE ID = (SELECT DoctorID FROM Appointments WHERE ID = @id)
	END
END
GO

CREATE PROCEDURE CheckAppointmentClash (@patientId int, @timeSlot int, @date DATETIME) AS 
BEGIN 
	SELECT COUNT(*) FROM Appointments WHERE PatientID = @patientId AND Completed = 0 AND TimeSlot = @timeSlot AND Date = @date 
END
GO

CREATE PROCEDURE CreateAppointment (@patientId int, @deptId int, @timeSlot int, @date DATETIME, @desc varchar(MAX)) AS 
BEGIN
	INSERT INTO Appointments (PatientID, DoctorID, TimeSlot, Date, Description) 
	VALUES (@patientId, (SELECT TOP 1 ID FROM Doctors WHERE DepartmentID = @deptId AND Available = 1 ORDER BY NEWID()), @timeSlot, @date, @desc)
END
GO

CREATE PROCEDURE ToggleDoctorAvailability (@doctorId int) AS
BEGIN
    IF (SELECT Available FROM Doctors WHERE ID = @doctorId) = 1
        UPDATE Doctors SET Available = 0 WHERE ID = @doctorId;
    ELSE
        UPDATE Doctors SET Available = 1 WHERE ID = @doctorId;
END
GO

