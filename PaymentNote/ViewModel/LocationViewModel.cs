using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentNote.ViewModel
{
    public class LocationViewModel
    {
        public string location_code { get; set; }
        public string location_name { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> created_at { get; set; }
        public string edited_by { get; set; }
        public Nullable<System.DateTime> edited_at { get; set; }
        public string deleted_by { get; set; }
        public Nullable<System.DateTime> deleted_at { get; set; }
        public Nullable<bool> deleted { get; set; }
    }
}