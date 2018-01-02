using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lending_System.Areas.Administrator.Models
{
    public class loanDetailForRestructure
    {
        public int autonum { get; set; }
        public string customer_name { get; set; }
        public string loan_no { get; set; }
        public string loan_granted { get; set; }
        public string loan_interest_rate { get; set; }
        public string payment_scheme { get; set; }
        public string due_date { get; set; }
        public string loan_date { get; set; }
        public string installment_no { get; set; }
        public string total_receivables { get; set; }
        public string balance { get; set; }
        public string restructured_interest { get; set; }
        public string new_balance { get; set; }
    }
}