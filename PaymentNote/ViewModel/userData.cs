using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentNote.ViewModel
{
    public class userData
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string first_name { get; set; }
        public string password { get; set; }
        public string location { get; set; }
        public bool isAdmin { get; set; }
        public bool deleted { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string  created_by { get; set; }
        public string updated_by { get; set; }
        public string Department { get; set; }
    }
}