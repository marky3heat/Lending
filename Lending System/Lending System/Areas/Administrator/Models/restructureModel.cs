using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lending_System.Areas.Administrator.Models
{
    public class restructureModel
    {
        public int autonum { get; set; }
        public string loan_no { get; set; }
        public string due_date { get; set; }
        public string customer_name { get; set; }
        public string balance { get; set; }
    }
}