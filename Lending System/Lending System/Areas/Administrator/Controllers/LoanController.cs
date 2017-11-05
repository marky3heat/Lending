using Lending_System.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Lending_System.Areas.Administrator.Models;

namespace Lending_System.Areas.Administrator.Controllers
{
    public class LoanController : Controller
    {
        db_lendingEntities db = new db_lendingEntities();
        DateTime _serverDateTime = DateTime.Now;

        // GET: Administrator/Loan
        public ActionResult Index()
        {
            ViewBag.Form = "Loan";
            ViewBag.Controller = "Loan";
            ViewBag.Action = "Module";

            if (Session["UserId"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }
    }
}