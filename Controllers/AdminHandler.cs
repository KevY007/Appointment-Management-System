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
        #region Admins
        #region Create New Admin
        [HttpGet, Authorize]
        public ActionResult NewAdmin(string error = "")
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
        public ActionResult NewAdmin(Admin adm)
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
                return RedirectToAction("NewAdmin", new { error = "Incorrect/Incomplete field(s) or length (min 5)!" });
            }

            SqlConnection con = new SqlConnection(Settings.ConnectionString);
            con.Open();

            var cmd = new SqlCommand($"EXEC CreateAdmin @name = '{adm.Name}', @password = '{adm.Password}'", con);
            cmd.ExecuteNonQuery();
            con.Close();
           

            return RedirectToAction("ViewAdmins");
        }
        #endregion

        #region View Admins
        [Authorize]
        public ActionResult ViewAdmins()
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

            SqlCommand cmd = new SqlCommand($"EXEC GetAllAdmins", con);

            DataTable dt = new DataTable();
            SqlDataAdapter sd = new SqlDataAdapter(cmd);

            sd.Fill(dt);

            List<Admin> lAdmin = new List<Admin>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count > 0)
                {
                    lAdmin.Add(new Admin()
                    {
                        Id = Convert.ToInt32(dt.Rows[i][0].ToString()),
                        Name = dt.Rows[i][1].ToString(),
                        Password = ""
                    });
                }
            }

            con.Close();

            ViewBag.Admins = lAdmin;
            return View();
        }
        #endregion

        #region Delete Admin
        [Authorize]
        public ActionResult DeleteAdmin(int id)
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

            SqlCommand cmd = new SqlCommand($"EXEC DeleteAdmin @admId = {id}", con);
            cmd.ExecuteNonQuery();
            con.Close();

            return (id == ((User)Session["User"]).Id) ? RedirectToAction("Logout") : RedirectToAction("ViewAdmins");
        }
        #endregion

        #endregion
    }
}