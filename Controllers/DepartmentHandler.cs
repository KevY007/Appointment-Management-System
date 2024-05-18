﻿using System;
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
        #region Departments
        #region Create New Department
        [HttpGet, Authorize]
        public ActionResult NewDepartment(string error = "")
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            if (((User)Session["User"]).Type != UserType.Admin)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You are not allowed to view this page.", backTo = "Index" });
            }

            ViewBag.Error = error;
            return View();
        }

        [HttpPost, Authorize]
        public ActionResult NewDepartment(Department dep)
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            if (((User)Session["User"]).Type != UserType.Admin)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You are not allowed to view this page.", backTo = "Index" });
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction("NewDepartment", new { error = "Incorrect/Incomplete field(s) or length (min 5)!" });
            }

            Admin user = (Admin)Session["User"];

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            var cmd = new SqlCommand($"EXEC CreateDepartment @name = '{dep.Name}', @appointmentCost = {dep.AppointmentCost}", con);
            cmd.ExecuteNonQuery();
            con.Close();
           

            return RedirectToAction("ViewDepartments");
        }
        #endregion

        #region View Departments
        [Authorize]
        public ActionResult ViewDepartments()
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

            con.Close();

            ViewBag.Departments = lDepartments;
            return View();
        }
        #endregion

        #region Delete Department
        [Authorize]
        public ActionResult DeleteDepartment(int id)
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

            SqlCommand cmd = new SqlCommand($"EXEC DeleteDepartment @deptId = {id}", con);
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("ViewDepartments");
        }
        #endregion

        #endregion
    }
}