using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Runtime;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Web.Security;
using System.Security.Cryptography;
using Microsoft.Reporting.WebForms;

namespace AMS.Controllers
{
    public partial class DefaultController : Controller
    {
        #region Appointments
        #region Create New Appointment
        [HttpGet, Authorize]
        public ActionResult NewAppointment(string error = "")
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            if (((User)Session["User"]).Type != UserType.Patient)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You cannot create an appointment without the patients consent.", backTo = "Index" });
            }

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand($"EXEC GetAllDepartments", con);

            DataTable dt = new DataTable();
            SqlDataAdapter sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<Department> lDepts = new List<Department>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lDepts.Add(new Department()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Name = dt.Rows[i][1].ToString(),
                        AppointmentCost = Convert.ToInt32(dt.Rows[i][2].ToString()),
                    });
                }
            }


            cmd = new SqlCommand($"EXEC GetAllTimeSlots", con);

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<SelectListItem> TimeSlots = new List<SelectListItem>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    TimeSlots.Add(new SelectListItem()
                    {
                        Value = dt.Rows[i][0].ToString(),
                        Text = dt.Rows[i][1].ToString()
                    });
                }
            }

            con.Close();

            if(lDepts.Count == 0)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "There are no departments created. Contact an administrator.", backTo = "Index" });
            }
            
            if (TimeSlots.Count == 0)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "There are no time slots set. Contact an administrator.", backTo = "Index" });
            }

            ViewBag.TimeSlots = TimeSlots;
            ViewBag.Departments = lDepts;
            ViewBag.Error = error;
            return View();
        }

        [HttpPost, Authorize]
        public ActionResult NewAppointment(int department, string TimeSlot, DateTime Date, string Description)
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }


            if (((User)Session["User"]).Type != UserType.Patient)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You cannot create an appointment without the patients consent.", backTo = "Index" });
            }

            if (Date < DateTime.Now)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You cannot create an appointment in the past.", backTo = "Index" });
            }

            Patient user = (Patient)Session["User"];

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();


            SqlCommand cmd = new SqlCommand($"EXEC CheckAppointmentClash @patientID = {user.Id}, @timeSlot = {TimeSlot}, @date = @dateVal", con);
            cmd.Parameters.AddWithValue("@dateVal", Date.ToString("yyyy-MM-dd"));
            int duplicate = (int)cmd.ExecuteScalar();

            if (duplicate > 0)
            {
                cmd.Dispose();
                con.Close();
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You already have an appointment for that time slot.", backTo = "NewAppointment" });
            }

            cmd = new SqlCommand($"EXEC GetNumAvailableDoctorsInDepartment @deptId = {department}", con);
            int numDoctors = (int)cmd.ExecuteScalar();

            if (numDoctors < 1)
            {
                cmd.Dispose();
                con.Close();
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "No doctors are currently available in that department.", backTo = "NewAppointment" });
            }

            cmd = new SqlCommand($"SELECT dbo.GetNumFreeDoctorsForAppointment({department}, {TimeSlot}, @dateVal)", con);
            cmd.Parameters.AddWithValue("@dateVal", Date.ToString("yyyy-MM-dd"));
            int numDoctorsAvail = (int)cmd.ExecuteScalar();

            if (numDoctorsAvail < 1)
            {
                cmd.Dispose();
                con.Close();
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "No doctors are currently available for that time slot and date in that department.", backTo = "NewAppointment" });
            }

            cmd = new SqlCommand($"EXEC CreateAppointment @patientId = {user.Id}, @deptId = {department}, @timeSlot = {TimeSlot}, @date = @dateVal, @desc = '{Description}', @queryBy = '{((User)Session["User"]).Type.ToString()} {((User)Session["User"]).Name} ({((User)Session["User"]).Id})'", con);

            cmd.Parameters.AddWithValue("@dateVal", Date.ToString("yyyy-MM-dd"));

            cmd.ExecuteNonQuery();
            con.Close();
           

            return RedirectToAction("ViewAppointments");
        }
        #endregion

        #region View Appointments
        [Authorize]
        public ActionResult ViewAppointments()
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            User user = (User)Session["User"];

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand($"EXEC GetAllDepartments", con);

            DataTable dt = new DataTable();
            SqlDataAdapter sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<Department> lDepartments = new List<Department>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lDepartments.Add(new Department()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Name = dt.Rows[i][1].ToString(),
                        AppointmentCost = Convert.ToInt32(dt.Rows[i][2].ToString()),


                        Created = DateTime.Parse(dt.Rows[i][3].ToString()),
                        CreatedBy = dt.Rows[i][4].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][5].ToString()),
                        ModifiedBy = dt.Rows[i][6].ToString(),
                    });
                }
            }

            cmd = new SqlCommand($"EXEC GetAllPatients", con);

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<Patient> lPatients = new List<Patient>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lPatients.Add(new Patient()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Name = dt.Rows[i][1].ToString(),
                        Password = "",
                        Gender = (Gender)Enum.ToObject(typeof(Gender), Convert.ToByte(dt.Rows[i][3].ToString())),
                        Email = dt.Rows[i][4].ToString(),
                        Address = dt.Rows[i][5].ToString(),
                        Phone = Convert.ToInt32(dt.Rows[i][6].ToString()),
                        Type = user.Type,


                        Created = DateTime.Parse(dt.Rows[i][7].ToString()),
                        CreatedBy = dt.Rows[i][8].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][9].ToString()),
                        ModifiedBy = dt.Rows[i][10].ToString(),
                    });
                }
            }

            cmd = new SqlCommand($"EXEC GetAllDoctors", con);

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<Doctor> lDoctors = new List<Doctor>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lDoctors.Add(new Doctor()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Name = dt.Rows[i][1].ToString(),
                        Password = "",
                        Department = lDepartments.Find(x => x.Id == Convert.ToInt32(dt.Rows[i][3].ToString())),
                        Available = Convert.ToBoolean(dt.Rows[i][4].ToString()),
                        Salary = Convert.ToInt32(dt.Rows[i][5].ToString()),
                        Type = user.Type,


                        Created = DateTime.Parse(dt.Rows[i][6].ToString()),
                        CreatedBy = dt.Rows[i][7].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][8].ToString()),
                        ModifiedBy = dt.Rows[i][9].ToString(),
                    });
                }
            }

            List<Appointment> lAppointments = new List<Appointment>();

            if (user.Type == UserType.Patient)
            {
                cmd = new SqlCommand($"EXEC GetPatientAppointments @patientId = {user.Id}", con);
            }
            else if (user.Type == UserType.Doctor)
            {
                cmd = new SqlCommand($"EXEC GetDoctorAppointments @doctorId = {user.Id}", con);
            }
            else if (user.Type == UserType.Admin)
            {
                cmd = new SqlCommand($"EXEC GetAllAppointments", con);
            }

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lAppointments.Add(new Appointment()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Patient = lPatients.Find(x => x.Id == Convert.ToInt32(dt.Rows[i][1].ToString())),
                        Doctor = lDoctors.Find(x => x.Id == Convert.ToInt32(dt.Rows[i][2].ToString())),
                        TimeSlot = Convert.ToInt32(dt.Rows[i][3].ToString()),
                        Date = DateTime.Parse(dt.Rows[i][4].ToString()),
                        Completed = Convert.ToBoolean(dt.Rows[i][5].ToString()),
                        Description = dt.Rows[i][6].ToString(),


                        Created = DateTime.Parse(dt.Rows[i][7].ToString()),
                        CreatedBy = dt.Rows[i][8].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][9].ToString()),
                        ModifiedBy = dt.Rows[i][10].ToString(),
                    });
                }
            }


            cmd = new SqlCommand($"EXEC GetAllTimeSlots", con);

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<SelectListItem> TimeSlots = new List<SelectListItem>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    TimeSlots.Add(new SelectListItem()
                    {
                        Value = dt.Rows[i][0].ToString(),
                        Text = dt.Rows[i][1].ToString()
                    });
                }
            }


            con.Close();

            ViewBag.Departments = lDepartments;
            ViewBag.Patients = lPatients;
            ViewBag.Doctors = lDoctors;
            ViewBag.Appointments = lAppointments;
            ViewBag.TimeSlots = TimeSlots;
            return View();
        }
        #endregion

        #region Appointment Reporting
        [Authorize]
        public ActionResult ViewAppointment(int id)
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            User user = (User)Session["User"];

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand($"EXEC GetAllDepartments", con);

            DataTable dt = new DataTable();
            SqlDataAdapter sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<Department> lDepartments = new List<Department>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lDepartments.Add(new Department()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Name = dt.Rows[i][1].ToString(),
                        AppointmentCost = Convert.ToInt32(dt.Rows[i][2].ToString()),


                        Created = DateTime.Parse(dt.Rows[i][3].ToString()),
                        CreatedBy = dt.Rows[i][4].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][5].ToString()),
                        ModifiedBy = dt.Rows[i][6].ToString(),
                    });
                }
            }

            cmd = new SqlCommand($"EXEC GetAllPatients", con);

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<Patient> lPatients = new List<Patient>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lPatients.Add(new Patient()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Name = dt.Rows[i][1].ToString(),
                        Password = "",
                        Gender = (Gender)Enum.ToObject(typeof(Gender), Convert.ToByte(dt.Rows[i][3].ToString())),
                        Email = dt.Rows[i][4].ToString(),
                        Address = dt.Rows[i][5].ToString(),
                        Phone = Convert.ToInt32(dt.Rows[i][6].ToString()),
                        Type = user.Type,


                        Created = DateTime.Parse(dt.Rows[i][7].ToString()),
                        CreatedBy = dt.Rows[i][8].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][9].ToString()),
                        ModifiedBy = dt.Rows[i][10].ToString(),
                    });
                }
            }

            cmd = new SqlCommand($"EXEC GetAllDoctors", con);

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<Doctor> lDoctors = new List<Doctor>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lDoctors.Add(new Doctor()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Name = dt.Rows[i][1].ToString(),
                        Password = "",
                        Department = lDepartments.Find(x => x.Id == Convert.ToInt32(dt.Rows[i][3].ToString())),
                        Available = Convert.ToBoolean(dt.Rows[i][4].ToString()),
                        Salary = Convert.ToInt32(dt.Rows[i][5].ToString()),
                        Type = user.Type,


                        Created = DateTime.Parse(dt.Rows[i][6].ToString()),
                        CreatedBy = dt.Rows[i][7].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][8].ToString()),
                        ModifiedBy = dt.Rows[i][9].ToString(),
                    });
                }
            }

            List<Appointment> lAppointments = new List<Appointment>();

            if (user.Type == UserType.Patient)
            {
                cmd = new SqlCommand($"EXEC GetPatientAppointments @patientId = {user.Id}", con);
            }
            else if (user.Type == UserType.Doctor)
            {
                cmd = new SqlCommand($"EXEC GetDoctorAppointments @doctorId = {user.Id}", con);
            }
            else if (user.Type == UserType.Admin)
            {
                cmd = new SqlCommand($"EXEC GetAllAppointments", con);
            }

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lAppointments.Add(new Appointment()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Patient = lPatients.Find(x => x.Id == Convert.ToInt32(dt.Rows[i][1].ToString())),
                        Doctor = lDoctors.Find(x => x.Id == Convert.ToInt32(dt.Rows[i][2].ToString())),
                        TimeSlot = Convert.ToInt32(dt.Rows[i][3].ToString()),
                        Date = DateTime.Parse(dt.Rows[i][4].ToString()),
                        Completed = Convert.ToBoolean(dt.Rows[i][5].ToString()),
                        Description = dt.Rows[i][6].ToString(),


                        Created = DateTime.Parse(dt.Rows[i][7].ToString()),
                        CreatedBy = dt.Rows[i][8].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][9].ToString()),
                        ModifiedBy = dt.Rows[i][10].ToString(),
                    });
                }
            }


            cmd = new SqlCommand($"EXEC GetAllTimeSlots", con);

            dt = new DataTable();
            sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            Dictionary<int, string> TimeSlots = new Dictionary<int, string>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    TimeSlots.Add(Convert.ToInt32(dt.Rows[i][0].ToString()), dt.Rows[i][1].ToString());
                }
            }


            con.Close();

            Appointment find = lAppointments.Find(x => x.Id == id);
            string timeSlot = "";
            
            if (find == null || !TimeSlots.TryGetValue(find.TimeSlot, out timeSlot))
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "The appointment is invalid or you don't have the permission to view it.", backTo = "ViewAppointments" });
            }

            LocalReport report = new LocalReport();
            report.DataSources.Clear();
            report.ReportPath = Server.MapPath("~/Reports/Appointment.rdlc");


            ReportParameter[] parameters = new ReportParameter[] {
                new ReportParameter("PatientID", find.Patient.Id.ToString()),
                new ReportParameter("PatientName", find.Patient.Name),
                new ReportParameter("PatientEmail", find.Patient.Email),
                new ReportParameter("PatientAddress", find.Patient.Address),
                new ReportParameter("PatientPhone", find.Patient.Phone.ToString()),
                new ReportParameter("PatientGender", find.Patient.Gender.ToString()),

                new ReportParameter("DepartmentName", find.Doctor.Department.Name),
                new ReportParameter("DoctorName", find.Doctor.Name),

                new ReportParameter("AppointmentID", find.Id.ToString()),
                new ReportParameter("AppointmentDate", find.Date.ToString("yyyy-MM-dd")),
                new ReportParameter("AppointmentLastUpdate", find.Modified.ToString("yyyy-MM-dd HH:mm:ss")),
                new ReportParameter("AppointmentCreated", find.Created.ToString("yyyy-MM-dd HH:mm:ss")),
                new ReportParameter("AppointmentTimeSlot", timeSlot),
                new ReportParameter("AppointmentDescription", find.Description),
                new ReportParameter("AppointmentCompleted", (find.Completed ? "Completed" : "Pending")),

                new ReportParameter("GenerateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            };

            string fileName = $"{find.Patient.Name} {find.Id} {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.pdf";
            
            report.SetParameters(parameters);
            report.DisplayName = fileName;

            byte[] pdfBytes = null;
            try
            {
                pdfBytes = report.Render("PDF");
            }
            catch { }

            Response.ContentType = "application/pdf";
            Response.AddHeader("Content-Disposition", $"inline; filename={fileName}");
            return File(pdfBytes, "application/pdf");
        }
        #endregion

        #region Mark as Completed
        [Authorize]
        public ActionResult CompleteAppointment(int id)
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand($"EXEC MarkAppointmentComplete @id = {id}, @queryBy = '{((User)Session["User"]).Type.ToString()} {((User)Session["User"]).Name} ({((User)Session["User"]).Id})'", con);
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("ViewAppointments");
        }
        #endregion

        #endregion
    }
}