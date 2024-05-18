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
        #region Doctors
        #region Create New Doctor
        [HttpGet, Authorize]
        public ActionResult NewDoctor(string error = "")
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            if (((User)Session["User"]).Type != UserType.Admin)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You are not allowed to view this page.", backTo = "Index" });
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
                        AppointmentCost = Convert.ToInt32(dt.Rows[i][2].ToString())
                    });
                }
            }

            con.Close();

            ViewBag.Departments = lDepts;
            ViewBag.Error = error;
            return View();
        }

        [HttpPost, Authorize]
        public ActionResult NewDoctor(string name, string password, string department)
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            if (((User)Session["User"]).Type != UserType.Admin)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You are not allowed to view this page.", backTo = "Index" });
            }

            if (name == null || password == null || department == null || name.Length < 3 || password.Length < 3 || department.Length < 1)
            {
                return RedirectToAction("NewDoctor", new { error = $"Incorrect field(s) or length!" });
            }

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            var cmd = new SqlCommand($"EXEC CreateDoctor @name = '{name}', @password = '{password}', @deptid = {department}", con);
            cmd.ExecuteNonQuery();
            con.Close();
           

            return RedirectToAction("ViewDoctors");
        }
        #endregion

        #region View Doctors
        [Authorize]
        public ActionResult ViewDoctors()
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            if (((User)Session["User"]).Type != UserType.Admin)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You are not allowed to view this page.", backTo = "Index" });
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
                    });
                }
            }

            con.Close();

            ViewBag.Doctors = lDoctors;
            return View();
        }
        #endregion

        #region Delete Doctor
        [Authorize]
        public ActionResult DeleteDoctor(int id)
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            if (((User)Session["User"]).Type != UserType.Admin)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You are not allowed to view this page.", backTo = "Index" });
            }

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand($"EXEC DeleteDoctor @docId = {id}", con);
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("ViewDoctors");
        }
        #endregion

        #endregion
    }
}