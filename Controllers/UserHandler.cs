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
using System.Net;
using System.Security.Policy;
using System.Web.Helpers;

namespace AMS.Controllers
{
    public partial class DefaultController : Controller
    {
        #region Patient Controls: Admin & Doctor
        [Authorize]
        public ActionResult ViewPatients()
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            User user = (User)Session["User"];
            if (user.Type == UserType.Patient)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You are not allowed to view this page.", backTo = "Index" });
            }

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();
            
            string query = "";
            if(user.Type == UserType.Admin)
            {
                query = $"EXEC GetAllPatients";
            }
            else if(user.Type == UserType.Doctor)
            {
                query = $"EXEC GetDoctorPatients @doctorId = {user.Id}";
            }
            SqlCommand cmd = new SqlCommand(query, con);

            DataTable dt = new DataTable();
            SqlDataAdapter sd = new SqlDataAdapter(cmd);

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


                        Created = DateTime.Parse(dt.Rows[i][7].ToString()),
                        CreatedBy = dt.Rows[i][8].ToString(),
                        Modified = DateTime.Parse(dt.Rows[i][9].ToString()),
                        ModifiedBy = dt.Rows[i][10].ToString(),
                    });
                }
            }

            con.Close();

            ViewBag.Patients = lPatients;
            return View();
        }

        [Authorize]
        public ActionResult DeletePatient(int id)
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

            SqlCommand cmd = new SqlCommand($"EXEC DeletePatient @id = {id}, @queryBy = '{((User)Session["User"]).Type.ToString()} {((User)Session["User"]).Name} ({((User)Session["User"]).Id})'", con);
            cmd.ExecuteNonQuery();
            con.Close();

            return RedirectToAction("ViewPatients");
        }

        [Authorize]
        public ActionResult ToggleAvailability()
        {
            if (Session["User"] == null)
            {
                return RedirectToAction("Login");
            }

            User user = (User)Session["User"];
            if (user.Type != UserType.Doctor)
            {
                return RedirectToAction("ErrorPage", new { title = "Unable to proceed!", message = "You are not a doctor.", backTo = "Index" });
            }

            Doctor doc = (Doctor)user;
            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            string query = $"EXEC ToggleDoctorAvailability @doctorId = {doc.Id}, @queryBy = '{((User)Session["User"]).Type.ToString()} {((User)Session["User"]).Name} ({((User)Session["User"]).Id})'";
            
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.ExecuteNonQuery();
            con.Close();

            doc.Available = !doc.Available;
            Session["User"] = doc;
            return Redirect(this.Request.UrlReferrer.AbsolutePath);
        }
        #endregion
    }
}