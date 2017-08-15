using Lending_System.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Lending_System.Areas.Administrator.Controllers
{
    public class CashInController : Controller
    {
        // GET: Administrator/CashIn
        public ActionResult Index()
        {
            if (Session["UserId"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }
        public ActionResult LoadList()
        {
            try
            {
                using (db_lendingEntities db = new db_lendingEntities())
                {
                    var data = db.tbl_cash_in.OrderByDescending(a => a.CashInId).ToList();

                    return Json(new { data = data }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        [HttpPost]
        public async Task<JsonResult> Save(tbl_cash_in model)
        {
            try
            {
                db_lendingEntities db = new db_lendingEntities();

                tbl_cash_in tbl = new tbl_cash_in();

                bool success = false;
                string message = "";

                tbl.CashInDate = model.CashInDate;
                tbl.UserName = model.UserName;
                tbl.DateFrom = model.DateFrom;
                tbl.DateTo = model.DateTo;
                tbl.Amount = model.Amount;
                tbl.CreatedBy = Session["UserName"].ToString();
                tbl.CreatedAt = DateTime.Now;
                db.tbl_cash_in.Add(tbl);
                await db.SaveChangesAsync();

                var result = true;
                success = result;
                if (result)
                {
                    message = "Successfully saved.";
                }
                else
                {
                    message = "Error saving data. Duplicate entry.";
                }

                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public ActionResult GetServerDate()
        {
            var serverDate = DateTime.Now.ToString("MM/dd/yyyy");

            return Json(serverDate, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetUser()
        {
            var result = Session["UserFullName"];

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}