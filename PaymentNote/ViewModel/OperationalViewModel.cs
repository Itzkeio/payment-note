using PaymentNote.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PaymentNote.ViewModel
{
    public class OperationalViewModel
    {
        // === Field form HEADER (form 1) ===
        public string po_id { get; set; }
        public string company_code { get; set; }
        public string currency { get; set; }
        public Nullable<long> currency_rate { get; set; }
        public int? vendor { get; set; }
        public string Remarks { get; set; }

        // === Audit fields ===
        public Nullable<System.DateTime> created_at { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> edited_at { get; set; }
        public string edited_by { get; set; }
        public Nullable<bool> deleted { get; set; }
        public Nullable<System.DateTime> deleted_at { get; set; }
        public string deleted_by { get; set; }

        // === List detail (form 2) ===
        public List<OperationalDetail> Details { get; set; }

        // === List untuk dropdown (diisi dari controller) ===
        public List<Company> CompanyList { get; set; }
        public List<Department> DepartmentList { get; set; }
        public List<Currency> CurrencyList { get; set; }
        public List<Vendor> VendorList { get; set; }
        public List<po_type> PoTypeList { get; set; }

        // === Constructor ===
        public OperationalViewModel()
        {
            // Inisialisasi list biar tidak null saat View di-render
            CompanyList = new List<Company>();
            DepartmentList = new List<Department>();
            CurrencyList = new List<Currency>();
            VendorList = new List<Vendor>();
            PoTypeList = new List<po_type>();
            Details = new List<OperationalDetail>();
        }

        // === Helper: status list (static, dipakai di view langsung) ===
        public static List<SelectListItem> GetStatusListItem()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "New", Text = "New" },
                new SelectListItem { Value = "Send To AP", Text = "Send To AP" },
                new SelectListItem { Value = "Cancel", Text = "Cancel" },
            };
        }

        // === Helper: convert entity list ke SelectListItem ===
        public List<SelectListItem> GetCompanyListItem()
        {
            return CompanyList
                .Where(c => c.deleted != true)
                .OrderBy(c => c.company_code)
                .Select(c => new SelectListItem
                {
                    Value = c.company_code,
                    Text = string.IsNullOrWhiteSpace(c.company_description)
                        ? c.company_code
                        : c.company_code + " - " + c.company_description
                })
                .ToList();
        }

        public List<SelectListItem> GetDepartmentListItem()
        {
            return DepartmentList
                .Where(d => d.deleted != true)
                .OrderBy(d => d.department_code)
                .Select(d => new SelectListItem
                {
                    Value = d.department_code,
                    Text = d.department_code
                })
                .ToList();
        }

        public List<SelectListItem> GetCurrencyListItem()
        {
            return CurrencyList
                .Where(c => c.deleted != true)
                .OrderBy(c => c.ccy_code)
                .Select(c => new SelectListItem
                {
                    Value = c.ccy_code,
                    Text = c.ccy_code
                })
                .ToList();
        }

        public List<SelectListItem> GetVendorListItem()
        {
            return VendorList
                .Where(v => v.deleted != true)
                .OrderBy(v => v.vendor_desc)
                .Select(v => new SelectListItem
                {
                    Value = v.vendor_id.ToString(),
                    Text = v.vendor_desc
                })
                .ToList();
        }

        public List<SelectListItem> GetPoTypeListItem()
        {
            return PoTypeList
                .Where(p => p.deleted != true)
                .Select(p => new SelectListItem
                {
                    Value = p.type_code,
                    Text = p.type_desc
                })
                .ToList();
        }
    }
}