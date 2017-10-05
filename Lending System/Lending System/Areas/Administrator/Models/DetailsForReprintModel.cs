using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lending_System.Areas.Administrator.Models
{
    public class DetailsForReprintModel
    {
        public string ReceiptNo { get; set; }
        public string Date { get; set; }
        public string Borrower { get; set; }
        public string IdNo { get; set; }

        public string principalReference { get; set; }
        public string principalParticulars { get; set; }
        public string principalAmount { get; set; }

        public string interestReference { get; set; }
        public string interestParticulars { get; set; }
        public string interestAmount { get; set; }

        public string balancLoanNo { get; set; }
        public string balanceAmount { get; set; }
    }
}