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
    public class RestructureController : Controller
    {
        db_lendingEntities db = new db_lendingEntities();
        DateTime _serverDateTime = DateTime.Now;

        // GET: Administrator/Restructure
        public ActionResult Index()
        {
            ViewBag.Form = "Restructure";
            ViewBag.Controller = "Restructure";
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

        public ActionResult LoadList()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    List<restructureModel> list = new List<restructureModel>();
                    var result = from d in db.tbl_loan_processing where d.due_date < _serverDateTime && d.loantype_id == 2 && d.status == "Released" orderby d.loantype_id select d;

                    foreach (var dt in result)
                    {
                        DateTime newDueDate = (DateTime)dt.due_date;
                        var dueDate = newDueDate.ToString("MM/dd/yyyy");

                        decimal loanBalance = decimal.Round((decimal)GetLedgerBalance(dt.loan_no), 2, MidpointRounding.AwayFromZero);
                        if (loanBalance > 0)
                        {
                            list.Add(new restructureModel { autonum = dt.autonum, customer_name = dt.customer_name, due_date = dueDate, loan_no = dt.loan_no, balance = String.Format("{0:0.00}", loanBalance) });
                        }
                    }
               
                    return Json(new { data = list }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public ActionResult GetLoanDetail(int id)
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    List<loanDetailForRestructure> list = new List<loanDetailForRestructure>();
                    var result = from d in db.tbl_loan_processing where d.autonum.Equals(id) select d;

                    foreach (var dt in result)
                    {
                        DateTime newDueDate = (DateTime)dt.due_date;
                        var dueDate = newDueDate.ToString("MM/dd/yyyy");

                        DateTime newLoanDate = (DateTime)dt.loan_date;
                        var loanDate = newLoanDate.ToString("MM/dd/yyyy");

                        decimal loanBalance = decimal.Round((decimal)GetLedgerBalance(dt.loan_no), 2, MidpointRounding.AwayFromZero);
                        if (loanBalance > 0)
                        {
                            list.Add(new loanDetailForRestructure { autonum = dt.autonum,
                                customer_name = dt.customer_name,
                                loan_no = dt.loan_no,
                                loan_granted = String.Format("{0:0.00}", dt.loan_granted),
                                loan_interest_rate = String.Format("{0:0.00}", dt.loan_interest_rate),
                                payment_scheme = dt.payment_scheme,
                                due_date = dueDate,
                                loan_date = loanDate,
                                installment_no = dt.installment_no.ToString(),
                                total_receivables = String.Format("{0:0.00}", dt.total_receivable),
                                balance = String.Format("{0:0.00}", loanBalance) });
                        }
                    }

                    return Json(list, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public decimal? GetLedgerBalance(string id)
        {
            using (db = new db_lendingEntities())
            {
                var found = false;
                decimal? balance = 0;
                var result =
                    from d in db.tbl_loan_ledger
                    where d.loan_no.Equals(id)
                    orderby (d.autonum)
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
                        case "Debit memo":
                            balance = balance + decimal.Round((decimal)data.interest, 2, MidpointRounding.AwayFromZero);
                            break;
                        case "Credit memo":
                            balance = balance - decimal.Round((decimal)data.interest, 2, MidpointRounding.AwayFromZero);
                            break;
                        default:
                            break;
                    }



                }
                return balance;
            }
        }
    }
}