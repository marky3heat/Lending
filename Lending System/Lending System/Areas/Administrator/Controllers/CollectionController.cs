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
    public class CollectionController : Controller
    {
        db_lendingEntities db = new db_lendingEntities();
        DateTime _serverDateTime = DateTime.Now;

        // GET: Administrator/Collection
        public ActionResult Index()
        {
            ViewBag.Form = "Collection";
            ViewBag.Controller = "Collection";
            ViewBag.Action = "Module";

            if (Session["UserId"] != null)
            {
                LoadCustomer();
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
                var dateNow = DateTime.Now;
                dateNow = dateNow.AddDays(-7);

                using (db = new db_lendingEntities())
                {
                    var result = db.tbl_payment.Where(a => a.date_trans >= dateNow).ToList();

                    return Json(new { data = result }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        [HttpGet]
        public ActionResult LoadAccountList(long? id)
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    List<loanAccountModel> loanAccountList = new List<loanAccountModel>();

                    var result = from d in db.tbl_loan_processing where d.customer_id == id && d.status == "Released" orderby d.loantype_id select d;
                    foreach (var dt in result)
                    {
                        decimal balance = 0;
                        decimal payments = 0;
                        decimal totalBalance = 0;
                        decimal loanGranted = 0;
                        decimal interestRate = 0;
                        decimal interest = 0;
                        decimal paymentsInterest = 0;

                        var interest_type = GetInterestType(dt.loan_name);

                        balance = (decimal)GetLedgerBalanceForAccountList(dt.loan_no);
                        payments = GetPayments(dt.loan_no);
                        paymentsInterest = GetPaymentsInterest(dt.loan_no);
                        //totalBalance = balance - payments - paymentsInterest;
                        totalBalance = balance;

                        interestRate = (decimal)dt.loan_interest_rate;
                        loanGranted = (decimal)dt.loan_granted;
                        interest = loanGranted * (interestRate / 100);

                        totalBalance = decimal.Round((decimal)totalBalance, 2, MidpointRounding.AwayFromZero);

                        //if (interest_type == "1")
                        //{
                        //    totalBalance = totalBalance - interest;
                        //}

                        if (totalBalance > 0)
                        {
                            var stringBalance = string.Format("{0:0.00}", totalBalance);

                            loanAccountList.Add(new loanAccountModel { LoanNo = dt.loan_no, CustomerName = dt.customer_name, Balance = totalBalance });
                        }
                    }
                    return Json(new { data = loanAccountList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public void LoadCustomer()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var customerList = new List<SelectListItem>();
                    var dbQuery = from d in db.tbl_customer select d;
                    foreach (var d in dbQuery)
                    {
                        if ((d.firstname != null) && (d.lastname != null) && (d.middlename != null))
                        {
                            customerList.Add(new SelectListItem { Value = d.autonum.ToString(), Text = d.lastname.ToUpper() + ", " + d.firstname.ToUpper() + " " + d.middlename.ToUpper() });
                        }
                        if ((d.firstname != null) && (d.lastname != null) && (d.middlename == null))
                        {
                            customerList.Add(new SelectListItem { Value = d.autonum.ToString(), Text = d.lastname.ToUpper() + ", " + d.firstname.ToUpper() });
                        }
                        if ((d.firstname != null) && (d.lastname == null) && (d.middlename == null))
                        {
                            customerList.Add(new SelectListItem { Value = d.autonum.ToString(), Text = d.firstname.ToUpper() });
                        }
                    }
                    ViewBag.Customer = new SelectList(customerList.OrderBy(a => a.Text), "Value", "Text");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        // GET
        [HttpGet]
        [Route("Administrator/Customer/GetCustomerList")]
        public JsonResult GetPrincipal(string loanNo)
        {
            using (db = new db_lendingEntities())
            {
                List<loanListModel> list = new List<loanListModel>();
                decimal loanAmount = 0;
                decimal loanPayment = 0;
                decimal loanBalance = 0;

                decimal loanGranted = 0;
                decimal loanInterestRate = 0;
                decimal loanInterest = 0;

                var result = from d in db.tbl_loan_processing where d.loan_no == loanNo && d.status == "Released" orderby d.date_created select d;
                foreach (var dt in result)
                {
                    loanGranted = decimal.Round((decimal)dt.loan_granted, 2, MidpointRounding.AwayFromZero);
                    loanInterestRate = decimal.Round((decimal)dt.loan_interest_rate, 2, MidpointRounding.AwayFromZero);
                    loanInterest = decimal.Round((decimal)loanGranted * (loanInterestRate / 100), 2, MidpointRounding.AwayFromZero);

                    loanAmount = decimal.Round((decimal)dt.total_receivable, 2, MidpointRounding.AwayFromZero);
                    loanPayment = decimal.Round((decimal)GetPayments(dt.loan_no), 2, MidpointRounding.AwayFromZero);
                    loanBalance = decimal.Round(loanAmount - loanPayment, 2, MidpointRounding.AwayFromZero);

                    DateTime newDueDate = (DateTime)dt.due_date;
                    var dueDate = newDueDate.ToString("MM/dd/yyyy");

                    var interestType = GetInterestType(dt.loan_name);
                    if (interestType == "1")
                    {
                        loanBalance = loanBalance - loanInterest;
                    }
                    if (loanBalance > 0)
                    {
                        list.Add(new loanListModel { LoanNo = dt.loan_no, LoanType = dt.loan_name, DueDate = dueDate, AmountDue = String.Format("{0:0.00}", loanBalance) });
                    }
                }

                return Json(new { data = list }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetInterest(string loanNo)
        {
            using (db = new db_lendingEntities())
            {
                //GenerateInterest(loanNo);
                List<loanListModel> list = new List<loanListModel>();
                decimal loanInterest = 0;

                var result = from d in db.tbl_loan_processing where d.loan_no == loanNo && d.status == "Released" orderby d.date_created select d;
                foreach (var dt in result)
                {
                    if (GetInterestType(dt.loan_name) == "1")
                    {
                        loanInterest = decimal.Round((decimal)GetLedgerInterestBalance(dt.loan_no, dt.loan_granted * (dt.loan_interest_rate / 100)), 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        loanInterest = decimal.Round((decimal)GetLedgerInterestBalance(dt.loan_no, 0), 2, MidpointRounding.AwayFromZero);
                    }
                    DateTime newDueDate = (DateTime)dt.due_date;
                    var dueDate = newDueDate.ToString("MM/dd/yyyy");

                    if (loanInterest > 0)
                    {
                        list.Add(new loanListModel { LoanNo = dt.loan_no, LoanType = dt.loan_name, DueDate = dueDate, AmountDue = String.Format("{0:0.00}", loanInterest) });
                    }
                }

                return Json(new { data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<JsonResult> Save(collectionSaveModel model)
        {
            try
            {
                tbl_payment payment = new tbl_payment();

                bool success = false;
                string message = "";

                db = new db_lendingEntities();

                var isDuplicate = await db.tbl_payment.AllAsync(u => u.reference_no == model.PaymentNo);

                if (isDuplicate == false)
                {
                    payment.reference_no = model.PaymentNo;
                    payment.date_trans = DateTime.Parse(model.PaymentDate);
                    payment.payor_id = int.Parse(model.CustomerId);
                    payment.payor_name = model.CustomerName;
                    payment.total_amount = decimal.Round(decimal.Parse(model.Payment), 2,
                        MidpointRounding.AwayFromZero);
                    payment.created_by = Session["UserName"].ToString();
                    payment.date_created = DateTime.Now;

                    db.tbl_payment.Add(payment);
                    await db.SaveChangesAsync();

                    var result = true;
                    success = result;
                    if (result)
                    {
                        decimal totalPayment = 0;
                        decimal amountDuePrincipal = 0;
                        decimal amountDueInterest = 0;

                        try
                        {
                            totalPayment = decimal.Round(decimal.Parse(model.Payment), 2,
                                MidpointRounding.AwayFromZero);
                            amountDuePrincipal = decimal.Round(decimal.Parse(model.AmountDuePrincipal), 2,
                                MidpointRounding.AwayFromZero);
                            amountDueInterest = decimal.Round(decimal.Parse(model.AmountDueInterest), 2,
                                MidpointRounding.AwayFromZero);
                        }
                        catch (Exception ex)
                        {

                        }

                        decimal paymentInterest = 0;
                        decimal paymentPrincipal = 0;

                        if (amountDueInterest > 0)
                        {
                            if (totalPayment >= amountDueInterest)
                            {
                                paymentInterest = decimal.Round(amountDueInterest, 2, MidpointRounding.AwayFromZero);
                                totalPayment = totalPayment - paymentInterest;
                            }
                            else if (totalPayment < amountDueInterest)
                            {
                                paymentInterest = totalPayment;
                                totalPayment = 0;
                            }
                        }
                        if (amountDuePrincipal > 0)
                        {
                            paymentPrincipal = totalPayment;
                            totalPayment = 0;
                        }
                        if (paymentPrincipal > 0)
                        {
                            tbl_payment_details paymentDetailsPrincipal = new tbl_payment_details();
                            paymentDetailsPrincipal.reference_no = model.PaymentNo;
                            paymentDetailsPrincipal.payment_type = "OR Payment";
                            paymentDetailsPrincipal.loan_no = model.LoanNo;
                            paymentDetailsPrincipal.loan_name = model.LoanName;
                            paymentDetailsPrincipal.due_date = DateTime.Parse(model.LoanDueDate);
                            paymentDetailsPrincipal.amount =
                                decimal.Round(paymentPrincipal, 2, MidpointRounding.AwayFromZero);
                            paymentDetailsPrincipal.created_by = Session["UserName"].ToString();
                            paymentDetailsPrincipal.date_created = DateTime.Now;
                            db.tbl_payment_details.Add(paymentDetailsPrincipal);
                            await db.SaveChangesAsync();
                        }
                        if (paymentInterest > 0)
                        {
                            tbl_payment_details paymentDetailsInterest = new tbl_payment_details();
                            paymentDetailsInterest.reference_no = model.PaymentNo;
                            paymentDetailsInterest.payment_type = "OR Payment Interest";
                            paymentDetailsInterest.loan_no = model.LoanNo;
                            paymentDetailsInterest.loan_name = model.LoanName;
                            paymentDetailsInterest.due_date = DateTime.Parse(model.LoanDueDate);
                            paymentDetailsInterest.amount =
                                decimal.Round(paymentInterest, 2, MidpointRounding.AwayFromZero);
                            paymentDetailsInterest.created_by = Session["UserName"].ToString();
                            paymentDetailsInterest.date_created = DateTime.Now;
                            db.tbl_payment_details.Add(paymentDetailsInterest);
                            await db.SaveChangesAsync();
                        }

                        tbl_loan_ledger loanLedger = new tbl_loan_ledger();
                        loanLedger.date_trans = DateTime.Parse(model.PaymentDate);
                        loanLedger.trans_type = "OR Payment";
                        loanLedger.reference_no = model.PaymentNo;
                        loanLedger.loan_no = model.LoanNo;
                        loanLedger.loan_type_name = model.LoanName;
                        loanLedger.customer_id = int.Parse(model.CustomerId);
                        loanLedger.customer_name = model.CustomerName;
                        loanLedger.interest_type = GetInterestType(model.LoanName);
                        loanLedger.interest_rate = GetInterestRate(model.LoanName);
                        loanLedger.interest = decimal.Round(paymentInterest, 2, MidpointRounding.AwayFromZero);
                        loanLedger.amount_paid =
                            decimal.Round(decimal.Parse(model.Payment), 2, MidpointRounding.AwayFromZero);
                        loanLedger.principal = decimal.Round(paymentPrincipal, 2, MidpointRounding.AwayFromZero);
                        loanLedger.balance = 0;
                        loanLedger.date_created = DateTime.Now;
                        loanLedger.created_by = Session["UserName"].ToString();
                        db.tbl_loan_ledger.Add(loanLedger);
                        await db.SaveChangesAsync();

                        message = "Successfully saved.";
                    }
                    else
                    {
                        message = "Error saving data. Duplicate entry.";
                    }
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

                return Json(new { success = false, message = "Failed to save." });
            }
        }

        public ActionResult LoadReprintDetails(int id)
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var ReceiptNo = "";
                    var Date = "";
                    var Borrower = "";
                    var IdNo = "";
                    var principalReference = "";
                    var principalParticulars = "";
                    var principalAmount = "";
                    var interestReference = "";
                    var interestParticulars = "";
                    var interestAmount = "";
                    var balancLoanNo = "";
                    var balanceAmount = "";


                    var result = from d in db.tbl_payment where d.autonum == id select d;
                    foreach (var dt in result)
                    {
                        ReceiptNo = dt.reference_no;
                        Date = dt.date_trans.ToString();
                        Borrower = dt.payor_name;
                        IdNo = dt.payor_id.ToString();
                    }

                    var result1 = from d in db.tbl_payment_details where d.reference_no == ReceiptNo && d.payment_type == "OR Payment" select d;
                    foreach (var dt in result1)
                    {
                        principalReference = dt.reference_no.ToString();
                        principalParticulars = "Principal payment";
                        principalAmount = String.Format("{0:n}", dt.amount);
                        balancLoanNo = dt.loan_no;
                    }

                    var result2 = from d in db.tbl_payment_details where d.reference_no == ReceiptNo && d.payment_type == "OR Payment Interest" select d;
                    foreach (var dt in result2)
                    {
                        interestReference = dt.reference_no.ToString();
                        interestParticulars = "Interest payment";
                        interestAmount = String.Format("{0:n}", dt.amount);
                        balancLoanNo = dt.loan_no;
                    }

                    balanceAmount =  String.Format("{0:n}", DisplayLedgerBalance(balancLoanNo, ReceiptNo));

                    List<DetailsForReprintModel> list = new List<DetailsForReprintModel>();
                    list.Add(new DetailsForReprintModel
                    {
                        ReceiptNo = "Receipt No: " + ReceiptNo,
                        Date = "Date: " + Date,
                        Borrower = "Borrower: " + Borrower,
                        IdNo = "ID No: " + IdNo,
                        principalReference = principalReference,
                        principalParticulars = principalParticulars,
                        principalAmount = principalAmount,
                        interestReference = interestReference,
                        interestParticulars = interestParticulars,
                        interestAmount = interestAmount,
                        balancLoanNo = balancLoanNo,
                        balanceAmount = balanceAmount
                    });

                    return Json(list, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #region Methods
        public decimal GetBalance(string id)
        {

            db_lendingEntities db = new db_lendingEntities();
            {
                decimal balance = 0;
                var result = from d in db.tbl_loan_processing where d.loan_no == id select d.total_receivable;
                foreach (var data in result)
                {
                    balance = data.Value;
                }

                return balance;
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
                    orderby(d.autonum)
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

                    if (data.interest_type == "1")
                    {
                        if (data.date_trans.Value.Day == _serverDateTime.Day && data.date_trans.Value.DayOfYear == _serverDateTime.DayOfYear)
                        {
                            found = true;
                        }
                    }
                    else
                    {
                        if (data.date_trans.Value.Day == _serverDateTime.Day && data.date_trans.Value.DayOfYear == _serverDateTime.DayOfYear)
                        {
                            found = true;
                        }
                    }

                }
                if (found)
                {
                    balance = 0;
                    return balance;
                }
                else
                {
                    return balance;
                }
            }
        }
        public decimal? GetLedgerBalanceForAccountList(string id)
        {
            using (db = new db_lendingEntities())
            {
                decimal? balance = 0;
                var result =
                    from d in db.tbl_loan_ledger
                    where d.loan_no.Equals(id) orderby d.autonum
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
        public decimal? DisplayLedgerBalance(string id, string referenceNo)
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
                    int refNo = 0;
                    try
                    {
                        refNo = Int32.Parse(data.reference_no);
                    }
                    catch (Exception)
                    {
                        refNo = 0;
                    }
                    int crefNo = Int32.Parse(referenceNo);
                    if (refNo <= crefNo)
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
                }

                return balance;

            }
        }
        public decimal? GetLedgerInterestBalance(string id, decimal? initialInterest)
        {
            using (db = new db_lendingEntities())
            {
                decimal? interest = initialInterest;
                var result =
                    from d in db.tbl_loan_ledger
                    where d.loan_no.Equals(id)
                    select d;
                foreach (var data in result)
                {
                    switch (data.trans_type)
                    {
                        case "Late Payment Interest":
                            interest = interest + decimal.Round((decimal)data.interest, 2, MidpointRounding.AwayFromZero);
                            break;
                        case "OR Payment":
                            interest = interest - decimal.Round((decimal)data.interest, 2, MidpointRounding.AwayFromZero);
                            break;
                        case "Debit memo":
                            interest = interest + decimal.Round((decimal)data.interest, 2, MidpointRounding.AwayFromZero);
                            break;
                        case "Credit memo":
                            interest = interest - decimal.Round((decimal)data.interest, 2, MidpointRounding.AwayFromZero);
                            break;
                        default:
                            break;
                    }
                }
                return interest;
            }
        }
        public decimal GetPayments(string id)
        {

            db_lendingEntities db = new db_lendingEntities();
            {
                decimal balance = 0;
                var result = from d in db.tbl_payment_details where d.loan_no == id && d.payment_type.Equals("OR Payment") select d.amount;
                foreach (var data in result)
                {
                    balance = balance + data.Value;
                }

                return balance;
            }
        }
        public decimal GetPaymentsInterest(string id)
        {
            db_lendingEntities db = new db_lendingEntities();
            {
                decimal balance = 0;
                var result = from d in db.tbl_payment_details where d.loan_no.Contains(id) && d.payment_type.Contains("OR Payment Interest") select d.amount;
                foreach (var data in result)
                {
                    balance = balance + data.Value;
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
        public ActionResult GetServerDate()
        {
            var serverDate = DateTime.Now.ToString("MM/dd/yyyy");

            return Json(serverDate, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> GetPaymentNo()
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    if (db.tbl_payment.Any())
                    {
                        // The table is empty
                        var data = await db.tbl_payment.MaxAsync(a => a.autonum);
                        if (data == 0)
                        {
                            return HttpNotFound();
                        }
                        return Json(data + 1, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(1, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (DataException)
            {
                return Json(1, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GenerateInterest(string loanNo)
        {
            using (db = new db_lendingEntities())
            {
                var result =
                    from d in db.tbl_loan_processing
                    where d.loan_no == loanNo
                          && d.status == "Released"
                          && d.due_date < _serverDateTime
                    orderby d.loantype_id ascending
                    select d;
                foreach (var dt in result)
                {
                    var balance = (decimal)GetLedgerBalance(dt.loan_no);

                    if (balance > 0)
                    {
                        var interestRate = (decimal)dt.loan_interest_rate;
                        decimal noOfDays = 0;
                        var dateStart = GetInterestStartDate(dt.loan_no);

                        var skipIinterest = false;

                        if (GetInterestType(dt.loan_name) == "1")
                        {
                            noOfDays = decimal.ToInt32((_serverDateTime - dateStart).Value.Days);
                        }
                        else
                        {
                            skipIinterest = true;
                            //if ((decimal.ToInt32((_serverDateTime - dateStart).Value.Days)) >= 1)
                            //{
                            //    if ((decimal.ToInt32((_serverDateTime - dateStart).Value.Days)) == 1)
                            //    {
                            //        if (NoOfInterest(dt.loan_no) >=
                            //            (decimal.ToInt32((_serverDateTime - dateStart).Value.Days)))
                            //        {
                            //            skipIinterest = true;
                            //        }
                            //        noOfDays = 1;
                            //    }
                            //    else
                            //    {
                            //        noOfDays = (_serverDateTime - dateStart).Value.Days;
                            //        noOfDays = (noOfDays / 30);
                            //        noOfDays = decimal.Ceiling(noOfDays);
                            //        if (NoOfInterest(dt.loan_no) >= noOfDays)
                            //        {
                            //            skipIinterest = true;
                            //        }
                            //    }
                            //}
                        }
                        decimal totalInterest = 0;
                        for (var c = 0; c < noOfDays; c++)
                        {
                            var interest = (balance * (interestRate / 100));
                            balance = balance + interest;
                            totalInterest = totalInterest + interest;
                        }
                        if (skipIinterest == false)
                        {
                            db_lendingEntities dbSave = new db_lendingEntities();
                            tbl_loan_ledger tbl = new tbl_loan_ledger();

                            tbl.date_trans = _serverDateTime;
                            tbl.trans_type = "Late Payment Interest";
                            tbl.reference_no = "";
                            tbl.loan_no = dt.loan_no;
                            tbl.loan_type_name = dt.loan_name;
                            tbl.customer_id = dt.customer_id;
                            tbl.customer_name = dt.customer_name;
                            tbl.interest_type = GetInterestType(dt.loan_name);
                            tbl.interest_rate = dt.loan_interest_rate;
                            tbl.interest = totalInterest;
                            tbl.amount_paid = 0;
                            tbl.principal = 0;
                            tbl.balance = 0;
                            tbl.date_created = DateTime.Now;
                            tbl.created_by = Session["UserName"].ToString();

                            dbSave.tbl_loan_ledger.Add(tbl);

                            dbSave.SaveChanges();
                        }
                    }
                }
            }

            return Json("Success", JsonRequestBehavior.AllowGet);
        }
        public DateTime? GetInterestStartDate(string id)
        {
            using (db = new db_lendingEntities())
            {
                DateTime? dateStart = null;
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
                                dateStart = data.date_trans.Value.AddDays(1);
                            }
                            else if (data.interest_type == "2")
                            {
                                dateStart = data.date_trans.Value.AddDays(30);
                            }
                            break;
                        case "Late Payment Interest":
                            if (data.interest_type == "1")
                            {
                                dateStart = data.date_trans;
                            }
                            else if (data.interest_type == "2")
                            {
                                dateStart = data.date_trans;
                            }
                            break;
                        default:
                            dateStart = data.date_trans;
                            break;
                    }
                }
                return dateStart;
            }
        }

        public decimal? NoOfInterest(string id)
        {
            var noOfInterest = 0;
            using (db = new db_lendingEntities())
            {
                
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

                            break;
                        case "Late Payment Interest":
                            noOfInterest = noOfInterest + 1;
                            break;
                        case "OR Payment":

                            break;
                        case "OR Payment Interest":

                            break;
                        case "Debit memo":

                            break;
                        case "Credit memo":

                            break;
                        default:
                            break;
                    }
                }
            }

            return noOfInterest;
        }

        public JsonResult CheckIfForRestructure(string id)
        {
            try
            {
                using (db = new db_lendingEntities())
                {
                    var message = "";
                    if (isRestructuredDone(id) == false)
                    {
                        message = "true";
                    }
                    else
                    {
                        message = "false";
                    }

                    return Json(new { success = true, message = message }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
                    DateTime? newPaymentInterestDate = DateTime.Now;
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
                                newPaymentInterestDate= (DateTime)data.date_trans;
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
                    if (((double)loopCounter) > 1)
                    {
                        result = false;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}