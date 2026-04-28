using PaymentNote.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentNote.ViewModel
{
    public class OperationalDetailsViewModel
    {
        public int detail_id { get; set; }
        public string po_id { get; set; }
        public string po_no { get; set; }
        public string po_type { get; set; }
        public Nullable<System.DateTime> po_date { get; set; }
        public string po_dept { get; set; }
        public Nullable<long> amount_dpp { get; set; }
        public Nullable<long> amount_vat { get; set; }
        public Nullable<long> amount_freight { get; set; }
        public Nullable<long> amount_freight_vat { get; set; }
        public Nullable<System.DateTime> created_at { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> edited_at { get; set; }
        public string edited_by { get; set; }
        public Nullable<bool> deleted { get; set; }
        public Nullable<System.DateTime> deleted_at { get; set; }
        public string deleted_by { get; set; }

        public virtual Operational Operational { get; set; }
    }
}