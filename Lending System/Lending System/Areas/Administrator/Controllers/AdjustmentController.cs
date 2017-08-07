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
using System.Web.UI.WebControls.Expressions;
using Lending_System.Areas.Administrator.Models;
using Microsoft.Ajax.Utilities;

namespace Lending_System.Areas.Administrator.Controllers
{
    public class AdjustmentController : Controller
    {
        db_lendingEntities db = new db_lendingEntities();
        DateTime _serverDateTime = DateTime.Now;

        // GET: Administrator/Adjustment
        public ActionResult Index()
        {
            ViewBag.Form = "Adjustment";
            ViewBag.Controller = "Adjustment";
            ViewBag.Action = "Module";

            if (Session["UserId"] != null)
            {
                LoadLoanNo();
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        #region Method
        public void LoadLoanNo()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var list = new List<SelectListItem>();
                    var result = from d in db.tbl_loan_processing where d.status == "Released" orderby d.autonum, d.loantype_id select d;
                    foreach (var d in result)
                    {
                        if (GetLedgerBalanceForAccountList(d.loan_no) > 0)
                        {
                            list.Add(new SelectListItem { Value = d.autonum.ToString(), Text = d.loan_no.ToUpper() });
                        }
                    }
                    ViewBag.LoanNo = new SelectList(list.ToList(), "Value", "Text");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public JsonResult GetAdjustmentNo()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var count = db.tbl_adjustment.ToList();

                    if (count.Count != 0)
                    {
                        var result = db.tbl_adjustment.Max(a => a.Autonum);

                        return Json(result + 1, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(1, JsonRequestBehavior.AllowGet);
                    }
                }
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
        public ActionResult LoadList()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var result = db.tbl_adjustment.ToList();

                    return Json(new { data = result }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<JsonResult> Save(tbl_adjustment model)
        {
            try
            {
                tbl_adjustment adjustment = new tbl_adjustment();

                var adjustmentNo = 0;
                using (db = new db_lendingEntities())
                {
                    var count = db.tbl_adjustment.ToList();

                    if (count.Count != 0)
                    {
                        adjustmentNo = db.tbl_adjustment.Max(a => a.Autonum)+1;
                    }
                    else
                    {
                        adjustmentNo = 1;
                    }
                }

                bool success = false;
                string message = "";

                using (db_lendingEntities dbSave = new db_lendingEntities())
                {
                    adjustment.TransType = model.TransType;
                    adjustment.DateTrans = model.DateTrans;
                    adjustment.LoanNo = model.LoanNo;
                    adjustment.Remarks = model.Remarks;
                    adjustment.Amount = decimal.Round((decimal)model.Amount, 2, MidpointRounding.AwayFromZero);
                    adjustment.CreatedBy = Session["UserName"].ToString();
                    adjustment.CreatedAt = DateTime.Now;
                    dbSave.tbl_adjustment.Add(adjustment);
                    await dbSave.SaveChangesAsync();

                    var result = true;
                    success = result;
                    if (result)
                    {
                        var customerId = 0;
                        var customerName = "";
                        var loanName = "";

                        using (db = new db_lendingEntities())
                        {
                            var loan = from d in db.tbl_loan_processing where d.loan_no == model.LoanNo && d.status == "Released" select d;
                            foreach (var dt in loan)
                            {
                                customerId = (int)dt.customer_id;
                                customerName = dt.customer_name;
                                loanName = dt.loan_name;
                            }
                        }

                        tbl_loan_ledger loanLedger = new tbl_loan_ledger();
                        loanLedger.date_trans = model.DateTrans;
                        loanLedger.trans_type = model.TransType;
                        loanLedger.reference_no = adjustmentNo.ToString();
                        loanLedger.loan_no = model.LoanNo;
                        loanLedger.loan_type_name = loanName.ToString();
                        loanLedger.customer_id = customerId;
                        loanLedger.customer_name = customerName;
                        loanLedger.interest_type = GetInterestType(loanName);
                        loanLedger.interest_rate = GetInterestRate(loanName);
                        loanLedger.interest = decimal.Round((decimal)model.Amount, 2, MidpointRounding.AwayFromZero);
                        loanLedger.amount_paid = decimal.Round(0, 2, MidpointRounding.AwayFromZero);
                        loanLedger.principal = decimal.Round(0, 2, MidpointRounding.AwayFromZero);
                        loanLedger.balance = 0;
                        loanLedger.date_created = DateTime.Now;
                        loanLedger.created_by = Session["UserName"].ToString();
                        dbSave.tbl_loan_ledger.Add(loanLedger);
                        await dbSave.SaveChangesAsync();

                        message = "Successfully saved.";
                    }
                    else
                    {
                        message = "Error saving data. Duplicate entry.";
                    }
                }

                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public string GetInterestType(string id)
        {

            db_lendingEntities db = new db_lendingEntities();
            {
                var interest_type = "";
                var result = from d in db.tbl_loan_type where d.description.Equals(id) select d;
                foreach (var data in result)
                {
                    interest_type = data.interest_type;
                }

                return interest_type;
            }
        }
        public decimal? GetInterestRate(string id)
        {

            db_lendingEntities db = new db_lendingEntities();
            {
                decimal? interestRate = 0;
                var result = from d in db.tbl_loan_type where d.description.Equals(id) select d;
                foreach (var data in result)
                {
                    interestRate = data.interest;
                }

                return interestRate;
            }
        }
        public decimal? GetLedgerBalanceForAccountList(string id)
        {
            using (db = new db_lendingEntities())
            {
                decimal? balance = 0;
                var result =
                    from d in db.tbl_loan_ledger
                    where d.loan_no.Equals(id)
                    select d;

                foreach (var data in result)
                {
                    switch (data.trans_type)
                    {
                        case "Beginning Balance":
                            balance = data.balance;
                            break;
                        case "Late Payment Interest":
                            balance = balance + decimal.Round((decimal)data.interest, 2, MidpointRounding.AwayFromZero);
                            break;
                        case "OR Payment":
                            balance = balance - decimal.Round((decimal)data.amount_paid, 2, MidpointRounding.AwayFromZero);
                            break;
                        case "OR Payment Interest":
                            balance = balance - decimal.Round((decimal)data.interest, 2, MidpointRounding.AwayFromZero);
                            break;
                        default:
                            break;
                    }
                }
                return balance;
            }
        }
        #endregion
    }
}