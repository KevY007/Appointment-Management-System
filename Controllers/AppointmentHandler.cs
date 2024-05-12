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

            Patient user = (Patient)Session["User"];

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();


            SqlCommand cmd = new SqlCommand($"EXEC CheckAppointmentClash @patientID = {user.Id}, @timeSlot = {TimeSlot}, @date = @dateVal", con);
            cmd.Parameters.AddWithValue("@dateVal", Date.ToString("dd/MM/yyyy"));
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

            cmd = new SqlCommand($"EXEC CreateAppointment @patientId = {user.Id}, @deptId = {department}, @timeSlot = {TimeSlot}, @date = @dateVal, @desc = '{Description}'", con);

            cmd.Parameters.AddWithValue("@dateVal", Date.ToString("dd/MM/yyyy"));

            cmd.ExecuteNonQuery();
            con.Close();
           

            return RedirectToAction("Index");
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
                        Password = dt.Rows[i][2].ToString(),
                        Gender = (Gender)Enum.ToObject(typeof(Gender), Convert.ToByte(dt.Rows[i][3].ToString())),
                        Email = dt.Rows[i][4].ToString(),
                        Address = dt.Rows[i][5].ToString(),
                        Phone = Convert.ToInt32(dt.Rows[i][6].ToString()),
                        Type = user.Type,
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
                        Password = dt.Rows[i][2].ToString(),
                        Department = lDepartments.Find(x => x.Id == Convert.ToInt32(dt.Rows[i][3].ToString())),
                        Available = Convert.ToBoolean(dt.Rows[i][4].ToString()),
                        Salary = Convert.ToInt32(dt.Rows[i][5].ToString()),
                        Type = user.Type,
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

            SqlCommand cmd = new SqlCommand($"EXEC MarkAppointmentComplete @id = {id}", con);
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("ViewAppointments");
        }
        #endregion

        #endregion
    }
}