//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Lending_System.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class tbl_user
    {
        public int autonum { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string gender { get; set; }
        public Nullable<int> user_rank_id { get; set; }
        public Nullable<int> department_id { get; set; }
        public string system_type { get; set; }
    }
}
