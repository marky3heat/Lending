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
                    var result = from d in db.tbl_loan_processing where d.due_date < _serverDateTime && d.loantype_id > 1 && d.status == "Released" orderby d.loantype_id select d;

                    foreach (var dt in result)
                    {
                        if (isRestructuredDone(dt.loan_no) == false)
                        {
                            DateTime newDueDate = (DateTime)dt.due_date;
                            var dueDate = newDueDate.ToString("MM/dd/yyyy");

                            decimal loanBalance = decimal.Round((decimal)GetLedgerBalance(dt.loan_no), 2, MidpointRounding.AwayFromZero);
                            if (loanBalance > 0)
                            {
                                list.Add(new restructureModel { autonum = dt.autonum, customer_name = dt.customer_name, due_date = dueDate, loan_no = dt.loan_no, balance = String.Format("{0:0.00}", loanBalance) });
                            }
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

                        decimal restructuredInterest = 0;
                        decimal restructuredInterestTotal = 0;
                        decimal newLoanBalance = loanBalance;
                        int loop = interestLoop(dt.loan_no);
                        for (int i = 0; i < loop; i++)
                        {
                            restructuredInterest = decimal.Round(newLoanBalance * ((decimal)dt.loan_interest_rate / 100), 2, MidpointRounding.AwayFromZero);
                            newLoanBalance = newLoanBalance + restructuredInterest;
                            restructuredInterestTotal = restructuredInterestTotal + restructuredInterest;
                        }
                       
                        if (loanBalance > 0)
                        {
                            list.Add(new loanDetailForRestructure
                            {
                                autonum = dt.autonum,
                                customer_name = dt.customer_name,
                                loan_no = dt.loan_no,
                                loan_granted = String.Format("{0:0.00}", dt.loan_granted),
                                loan_interest_rate = String.Format("{0:0.00}", dt.loan_interest_rate),
                                payment_scheme = dt.payment_scheme,
                                due_date = dueDate,
                                loan_date = loanDate,
                                installment_no = dt.installment_no.ToString(),
                                total_receivables = String.Format("{0:0.00}", dt.total_receivable),
                                balance = String.Format("{0:0.00}", loanBalance),
                                restructured_interest = String.Format("{0:0.00}", restructuredInterestTotal),
                                new_balance = String.Format("{0:0.00}", newLoanBalance)
                            });
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

        public Boolean isRestructuredDone(string id)
        {
            bool result = false;

            try
            {
                using (db = new db_lendingEntities())
                {
                    DateTime? DueDate = DateTime.Now;
                    DateTime? latePaymentInterestDate = DateTime.Now;
                    Boolean hasLatePaymentInterest = false;
                    var result1 =
                        from d in db.tbl_loan_ledger
                        where d.loan_no.Equals(id)
                        orderby (d.autonum)
                        select d;

                    foreach (var data in result1)
                    {
                        switch (data.trans_type)
                        {
                            case "Beginning Balance":
                                DueDate = (DateTime)data.date_trans.Value.AddDays(0);
                                break;
                            case "Late Payment Interest":
                                latePaymentInterestDate = (DateTime)data.date_trans;
                                hasLatePaymentInterest = true;
                                break;
                            default:
                                break;
                        }
                    }
                    decimal loopCounter = decimal.ToInt32((_serverDateTime - DueDate).Value.Days);
                    loopCounter = Convert.ToInt32(Math.Floor(loopCounter / 30));
                    if (hasLatePaymentInterest == true)
                    {
                        latePaymentInterestDate = DueDate.Value.AddDays((double)loopCounter * 30);
                    }
                    else
                    {
                        latePaymentInterestDate = DueDate.Value.AddDays(0);
                    }
                  
                    if ((decimal.ToInt32((_serverDateTime - latePaymentInterestDate).Value.Days)) < 31)
                    {
                        result = true;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public int interestLoop(string id)
        {
            int result = 0;

            try
            {
                using (db = new db_lendingEntities())
                {
                    DateTime? DueDate = DateTime.Now;
                    DateTime? latePaymentInterestDate = DateTime.Now;
                    Boolean hasLatePaymentInterest = false;
                    var result1 =
                        from d in db.tbl_loan_ledger
                        where d.loan_no.Equals(id)
                        orderby (d.autonum)
                        select d;

                    foreach (var data in result1)
                    {
                        switch (data.trans_type)
                        {
                            case "Beginning Balance":
                                DueDate = (DateTime)data.date_trans.Value.AddDays(0);
                                break;
                            case "Late Payment Interest":
                                latePaymentInterestDate = (DateTime)data.date_trans;
                                hasLatePaymentInterest = true;
                                break;
                            default:
                                break;
                        }
                    }

                    decimal loopCounter = decimal.ToInt32((_serverDateTime - DueDate).Value.Days);
                    loopCounter = Convert.ToInt32(Math.Floor(loopCounter / 30));
                    if (hasLatePaymentInterest == true)
                    {
                        latePaymentInterestDate = DueDate.Value.AddDays((double)loopCounter * 30);
                    }
                    else
                    {
                        latePaymentInterestDate = DueDate.Value.AddDays(0);
                    }

                    decimal difference = (decimal.ToInt32((_serverDateTime - latePaymentInterestDate).Value.Days));

                    if (difference >= 30)
                    {
                        result = Convert.ToInt32(Math.Floor(difference / 30));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public ActionResult Save(string id)
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    string message = "";
                    var result = from d in db.tbl_loan_processing where d.loan_no.Equals(id) select d;

                    foreach (var dt in result)
                    {
                        DateTime newDueDate = (DateTime)dt.due_date;
                        var dueDate = newDueDate.ToString("MM/dd/yyyy");

                        DateTime newLoanDate = (DateTime)dt.loan_date;
                        var loanDate = newLoanDate.ToString("MM/dd/yyyy");

                        decimal loanBalance = decimal.Round((decimal)GetLedgerBalance(dt.loan_no), 2, MidpointRounding.AwayFromZero);

                        decimal restructuredInterest = 0;
                        decimal restructuredInterestTotal = 0;
                        decimal newLoanBalance = loanBalance;
                        int loop = interestLoop(dt.loan_no);
                        for (int i = 0; i < loop; i++)
                        {
                            restructuredInterest = decimal.Round(newLoanBalance * ((decimal)dt.loan_interest_rate / 100), 2, MidpointRounding.AwayFromZero);
                            newLoanBalance = newLoanBalance + restructuredInterest;
                            restructuredInterestTotal = restructuredInterestTotal + restructuredInterest;
                        }

                        if (loanBalance > 0)
                        {
                            db_lendingEntities dbSave = new db_lendingEntities();
                            tbl_loan_ledger tbl = new tbl_loan_ledger();

                            tbl.date_trans = _serverDateTime;
                            tbl.trans_type = "Late Payment Interest";
                            tbl.reference_no = "";
                            tbl.loan_no = dt.loan_no;
                            tbl.loan_type_name = dt.loan_name;
                            tbl.customer_id = dt.customer_id;
                            tbl.customer_name = dt.customer_name.ToUpper();
                            tbl.interest_type = GetInterestType(dt.loan_name);
                            tbl.interest_rate = dt.loan_interest_rate;
                            tbl.interest = restructuredInterestTotal;
                            tbl.amount_paid = 0;
                            tbl.principal = 0;
                            tbl.balance = 0;
                            tbl.date_created = DateTime.Now;
                            tbl.created_by = Session["UserName"].ToString();

                            dbSave.tbl_loan_ledger.Add(tbl);

                            dbSave.SaveChanges();

                            message = "Success!";
                        }
                        else
                        {
                            message = "Failed!";
                        }
                    }
                    return Json(new { success = true, message = message }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }



        }
    }
}