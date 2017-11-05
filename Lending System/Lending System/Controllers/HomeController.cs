using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Lending_System.Models;

namespace Lending_System.Controllers
{
    public class HomeController : Controller
    {
        db_lendingEntities db = new db_lendingEntities();
        DateTime _serverDateTime = DateTime.Now;

        public ActionResult Index()
        {
            if (Session["UserId"] != null)
            {
                ViewBag.Form = "Dashboard";
                ViewBag.Controller = "Home";
                ViewBag.Action = "Dashboard";
                Session["RestructureCount"] = GetForRestructureDues();

                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        public JsonResult LoadDashboard()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var datetimenow = DateTime.Now;
                    var datenow = datetimenow.Date;

                    decimal CashReleased = 0;
                    var listCashReleased =
                        from d in db.tbl_loan_processing
                        where (d.loan_date >= datenow && d.loan_date <= datenow)
                        && d.status.Equals("Released")
                        select d.net_proceeds;
                    CashReleased = listCashReleased.AsEnumerable().Sum(o => o.Value);

                    decimal CashCollected = 0;
                    var listCashCollected =
                        from d in db.tbl_payment
                        where (d.date_trans >= datenow && d.date_trans <= datenow)
                        select d.total_amount;
                    CashCollected = listCashCollected.AsEnumerable().Sum(o => o.Value);

                    decimal Receivable = 0;

                    CashReleased = decimal.Round(CashReleased, 2, MidpointRounding.AwayFromZero); 
                    CashCollected = decimal.Round(CashCollected, 2, MidpointRounding.AwayFromZero);
                    Receivable = decimal.Round((decimal)GetReceivables(), 2, MidpointRounding.AwayFromZero);
                    var ForRestructure = GetForRestructureDues();

                    Session["Released"] = String.Format("{0:n}", CashReleased);
                    Session["Collection"] = String.Format("{0:n}", CashCollected);
                    Session["Receivables"] = String.Format("{0:n}", Receivable);
                    Session["RestructureCount"] = ForRestructure;

                    List<DashboardModel> list = new List<DashboardModel>();
                    list.Add(new DashboardModel
                    {
                        Released = String.Format("{0:n}", CashReleased),
                        Collection = String.Format("{0:n}", CashCollected),
                        Receivables = String.Format("{0:n}", Receivable),
                        ForRestructure = ForRestructure.ToString()
                    });


                    return Json(list, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

           
        }

        public decimal? GetReceivables()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var dateVar = DateTime.Now;
                    decimal? totalBalance = 0;

                    var result = from d in db.tbl_loan_processing where (d.due_date <= dateVar) && d.status == "Released" orderby d.customer_name ascending select d;
                    foreach (var dt in result)
                    {
                        decimal ledgerBalance = decimal.Round((decimal)GetLedgerBalance(dt.loan_no), 2, MidpointRounding.AwayFromZero);

                        if (ledgerBalance > 0)
                        {
                            decimal adjustment = decimal.Round((decimal)GetAdjustment(dt.loan_no), 2, MidpointRounding.AwayFromZero);
                            decimal? principal = decimal.Round((decimal)dt.loan_granted, 2, MidpointRounding.AwayFromZero);
                            decimal? principalInterest = decimal.Round((decimal)(dt.loan_granted * (dt.loan_interest_rate / 100)), 2, MidpointRounding.AwayFromZero);                             
                            decimal? interest = decimal.Round((decimal)GetInterest(dt.loan_no), 2, MidpointRounding.AwayFromZero);                             
                            decimal? additionalInterest = decimal.Round((decimal)ComputeInterest(dt.loan_no, (decimal)dt.loan_interest_rate, dateVar), 2, MidpointRounding.AwayFromZero);
                            interest = decimal.Round((decimal)(principalInterest + interest + additionalInterest), 2, MidpointRounding.AwayFromZero);
                            decimal? payment = decimal.Round((decimal)GetPayments(dt.loan_no), 2, MidpointRounding.AwayFromZero);

                            if (adjustment < 0)
                            {
                                adjustment = adjustment * -1;
                                if (adjustment >= interest)
                                {
                                    interest = 0;
                                    adjustment = adjustment - (decimal)interest;
                                    if (adjustment > 0 && principal > adjustment)
                                    {
                                        principal = principal - adjustment;
                                        adjustment = 0;
                                    }
                                }
                                else
                                {
                                    interest = interest - adjustment;
                                }
                            }
                            else
                            {
                                interest = interest + adjustment;
                            }

                            decimal? balance = decimal.Round((decimal)(principal + interest - payment), 2, MidpointRounding.AwayFromZero);

                            if (balance > 0)
                            {
                                if (GetInterestType(dt.loan_name) == "1")
                                {
                                }
                                else if (GetInterestType(dt.loan_name) == "2")
                                {
                                    balance = balance - principalInterest;
                                }
                                else if (GetInterestType(dt.loan_name) != "1" && GetInterestType(dt.loan_name) != "2")
                                {
                                    balance = balance - principalInterest;
                                }

                                totalBalance = totalBalance + balance;
                            }
                        }
                    }
                    return totalBalance;
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
                    orderby d.autonum
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
        public decimal? GetAdjustment(string id)
        {

            db = new db_lendingEntities();
            {
                decimal? adjustment = 0;
                var result = from d in db.tbl_adjustment where d.LoanNo.Equals(id) orderby d.Autonum select d;
                foreach (var data in result)
                {
                    if (data.TransType == "Debit memo")
                    {
                        adjustment = decimal.Round((decimal)(adjustment + data.Amount), 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        adjustment = decimal.Round((decimal)(adjustment - data.Amount), 2, MidpointRounding.AwayFromZero);
                    }
                }
                return adjustment;
            }
        }
        public decimal? GetInterest(string id)
        {

            db = new db_lendingEntities();
            {
                decimal? interest = 0;
                var result =
                    from d in db.tbl_loan_ledger
                    where d.loan_no.Equals(id)
                    orderby d.autonum
                    select d;

                foreach (var data in result)
                {
                    switch (data.trans_type)
                    {
                        case "Late Payment Interest":
                            interest = decimal.Round((decimal)(interest + data.interest), 2, MidpointRounding.AwayFromZero);
                            break;
                        default:
                            break;
                    }
                }
                return interest;
            }
        }
        public decimal ComputeInterest(string id, decimal interestRate, DateTime testDate)
        {
            decimal computedInterest = 0;
            decimal ledgerBalance = 0;
            DateTime? dateStartOfComputation = DateTime.Now;
            DateTime serverDate = testDate;
            decimal noOfDays = 0;

            try
            {
                using (db = new db_lendingEntities())
                {
                    dateStartOfComputation = GetStartDateForComputationOfInterest(id);
                    ledgerBalance = decimal.Round((decimal)GetLedgerBalance(id), 2, MidpointRounding.AwayFromZero);

                    if (GetInterestType(id) == "1")
                    {
                        noOfDays = decimal.ToInt32((serverDate - dateStartOfComputation).Value.Days);
                    }
                    else
                    {

                        if ((decimal.ToInt32((serverDate - dateStartOfComputation).Value.Days)) >= 1)
                        {
                            if ((decimal.ToInt32((serverDate - dateStartOfComputation).Value.Days)) == 1)
                            {
                                noOfDays = 1;
                            }
                            else
                            {
                                noOfDays = (serverDate - dateStartOfComputation).Value.Days;
                                noOfDays = (noOfDays / 30);
                                noOfDays = decimal.Ceiling(noOfDays) - 1;
                            }
                        }
                    }
                    for (var c = 0; c < noOfDays; c++)
                    {
                        var interest = (ledgerBalance * (interestRate / 100));
                        ledgerBalance = decimal.Round((decimal)(ledgerBalance + interest), 2, MidpointRounding.AwayFromZero);
                        computedInterest = decimal.Round((decimal)(computedInterest + interest), 2, MidpointRounding.AwayFromZero);
                    }
                }

                return computedInterest;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public DateTime? GetStartDateForComputationOfInterest(string id)
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    DateTime? dateStart = DateTime.Now;
                    var result =
                        from d in db.tbl_loan_ledger
                        where d.loan_no.Equals(id)
                        select d;

                    foreach (var data in result)
                    {
                        switch (data.trans_type)
                        {
                            case "Beginning Balance":
                                if (data.interest_type == "1")
                                {
                                    dateStart = data.date_trans.Value;
                                    dateStart = dateStart.Value.AddDays(1);
                                }
                                else if (data.interest_type == "2")
                                {
                                    dateStart = data.date_trans.Value.AddDays(30);
                                }
                                break;
                            case "Late Payment Interest":
                                if (data.interest_type == "1")
                                {
                                    dateStart = data.date_trans.Value.AddDays(0);
                                }
                                else if (data.interest_type == "2")
                                {
                                    dateStart = data.date_trans.Value.AddDays(0);
                                }
                                break;
                            default:
                                dateStart = data.date_trans.Value.AddDays(0);
                                break;
                        }
                    }
                    return dateStart;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public string GetInterestType(string id)
        {

            db = new db_lendingEntities();
            {
                var interest_type = "";
                var result = from d in db.tbl_loan_type where d.description == id select d;
                foreach (var data in result)
                {
                    interest_type = data.interest_type;
                }

                return interest_type;
            }
        }
        public decimal GetPayments(string id)
        {

            db = new db_lendingEntities();
            {
                decimal balance = 0;
                var result = from d in db.tbl_payment_details where d.loan_no.Equals(id) select d.amount;
                foreach (var data in result)
                {
                    balance = balance + data.Value;
                }

                return balance;
            }
        }
        public int GetForRestructureDues()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var count = 0;
                    var result = from d in db.tbl_loan_processing where d.due_date <= _serverDateTime && d.loantype_id == 2 && d.status == "Released" orderby d.loantype_id select d;

                    foreach (var dt in result)
                    {
                        decimal loanBalance = decimal.Round((decimal)GetLedgerBalance(dt.loan_no), 2, MidpointRounding.AwayFromZero);
                        if (loanBalance > 0)
                        {
                            count += 1;
                        }
                    }
                    return count;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}