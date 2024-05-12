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

namespace AMS
{
    public static class Settings
    {
        public static string ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AMS;Integrated Security=True";
    }
}

namespace AMS.Controllers
{
    public partial class DefaultController : Controller
    {
        #region Generic

        #region Error Page
        public ActionResult ErrorPage(string title = "Error!", string message = "An error has occured.", string backTo = "Index")
        {
            ViewBag.Title = title;
            ViewBag.Message = message;
            ViewBag.BackTo = backTo;
            return View();
        }
        #endregion

        #region Index Page/Dashboard
        public ActionResult Index()
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            User user = (User)Session["User"];

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            if (user.Type == UserType.Admin)
            {
                SqlCommand cmd = new SqlCommand($"EXEC GetNumDoctors", con);
                ViewBag.NumDoctors = (int)cmd.ExecuteScalar();
                cmd.Dispose();

                cmd = new SqlCommand($"EXEC GetNumPatients", con);
                ViewBag.NumPatients = (int)cmd.ExecuteScalar();
                cmd.Dispose();

                cmd = new SqlCommand($"EXEC GetNumDepartments", con);
                ViewBag.NumDepartments = (int)cmd.ExecuteScalar();
                cmd.Dispose();

                cmd = new SqlCommand($"EXEC GetNumPendingAppointments", con);
                ViewBag.NumAppointmentsActive = (int)cmd.ExecuteScalar();
                cmd.Dispose();

                cmd = new SqlCommand($"EXEC GetNumCompletedAppointments", con);
                ViewBag.NumAppointmentsCompleted = (int)cmd.ExecuteScalar();
                cmd.Dispose();
            }
            else if (user.Type == UserType.Doctor)
            {
                SqlCommand cmd = new SqlCommand($"EXEC GetNumDoctorPendingAppointments @doctorId = {user.Id}", con);
                ViewBag.NumAppointmentsActive = (int)cmd.ExecuteScalar();
                cmd.Dispose();

                cmd = new SqlCommand($"EXEC GetNumDoctorCompletedAppointments @doctorId = {user.Id}", con);
                ViewBag.NumAppointmentsCompleted = (int)cmd.ExecuteScalar();
                cmd.Dispose();

                cmd = new SqlCommand($"EXEC GetNumDoctorPatients @doctorId = {user.Id}", con);
                ViewBag.NumPatients = (int)cmd.ExecuteScalar();
                cmd.Dispose();
            }
            else
            {
                SqlCommand cmd = new SqlCommand($"EXEC GetNumPatientPendingAppointments @patientId = {user.Id}", con);
                ViewBag.NumAppointmentsActive = (int)cmd.ExecuteScalar();
                cmd.Dispose();

                cmd = new SqlCommand($"EXEC GetNumPatientCompletedAppointments @patientId = {user.Id}", con);
                ViewBag.NumAppointmentsCompleted = (int)cmd.ExecuteScalar();
                cmd.Dispose();
            }

            con.Close();


            return View();
        }

        #endregion

        #region Login, Register, Logout

        public ActionResult Logout()
        {
            Session["User"] = null;
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Register(string error = "")
        {
            ViewBag.Error = error;
            return View();
        }

        [HttpPost]
        public ActionResult Register(Patient patient)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Register", new { error = "Incorrect field(s) or length!" });
            }

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand($"EXEC CreatePatient @name = '{patient.Name}', @password = '{patient.Password}', @gender = {(byte)patient.Gender}, @email = '{patient.Email}', @address = '{patient.Address}', @phone = {patient.Phone}", con);

            cmd.ExecuteNonQuery();
            con.Close();


            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Login(string error = "")
        {
            ViewBag.Error = error;
            return View();
        }

        [HttpPost]
        public ActionResult Login(User user)
        {
            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand($"EXEC Login{(user.Type == UserType.Patient ? "Patient" : (user.Type == UserType.Doctor ? "Doctor" : "Admin"))} " +
                $"@name = '{user.Name}', @password = '{user.Password}'", con);

            DataTable dt = new DataTable();
            SqlDataAdapter sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);


            if (dt.Rows.Count == 0)
            {
                return RedirectToAction("Login", new { error = "Invalid account or password or type!" });
            }

            con.Close();


            if (user.Type == UserType.Patient)
            {
                Session["User"] = new Patient()
                {
                    Id = Convert.ToInt32(dt.Rows[0][0].ToString()),
                    Name = dt.Rows[0][1].ToString(),
                    Password = dt.Rows[0][2].ToString(),
                    Gender = (Gender)Enum.ToObject(typeof(Gender), Convert.ToByte(dt.Rows[0][3].ToString())),
                    Email = dt.Rows[0][4].ToString(),
                    Address = dt.Rows[0][5].ToString(),
                    Phone = Convert.ToInt32(dt.Rows[0][6].ToString()),
                };
            }
            else if (user.Type == UserType.Doctor)
            {
                con.Open();

                SqlCommand ncmd = new SqlCommand($"EXEC GetDepartmentById @id = {Convert.ToInt32(dt.Rows[0][3].ToString())}", con);

                DataTable deptTable = new DataTable();
                SqlDataAdapter deptAdp = new SqlDataAdapter(ncmd);

                deptAdp.Fill(deptTable);

                Department dept = new Department() { Id = Convert.ToInt32(deptTable.Rows[0][0].ToString()), Name = deptTable.Rows[0][1].ToString() };

                ncmd.Dispose();
                con.Close();

                Session["User"] = new Doctor()
                {
                    Id = Convert.ToInt32(dt.Rows[0][0].ToString()),
                    Name = dt.Rows[0][1].ToString(),
                    Password = dt.Rows[0][2].ToString(),
                    Department = dept,
                    Available = Convert.ToBoolean(dt.Rows[0][4].ToString()),
                    Salary = Convert.ToInt32(dt.Rows[0][5].ToString()),
                };
            }
            else if (user.Type == UserType.Admin)
            {
                Session["User"] = new Admin()
                {
                    Id = Convert.ToInt32(dt.Rows[0][0].ToString()),
                    Name = dt.Rows[0][1].ToString(),
                    Password = dt.Rows[0][2].ToString(),
                };
            }

            FormsAuthentication.SetAuthCookie(((User)Session["User"]).Name, false);

            return RedirectToAction("Index");
        }
        #endregion
        #endregion
    }
}