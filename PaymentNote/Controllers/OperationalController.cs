using PaymentNote.Models;
using PaymentNote.ViewModel;
using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PaymentNote.Controllers
{
    public class OperationalController : Controller
    {
        private readonly DbPaymentNoteEntities2 db = new DbPaymentNoteEntities2();

        // Helper: populate dropdown lists ke ViewModel
        private void PopulateDropdownLists(OperationalViewModel viewModel)
        {
            viewModel.CompanyList = db.Companies
                .Where(c => c.deleted != true).ToList();

            viewModel.DepartmentList = db.Departments
                .Where(d => d.deleted != true).ToList();

            viewModel.CurrencyList = db.Currencies
                .Where(c => c.deleted != true).ToList();

            viewModel.VendorList = db.Vendors
                .Where(v => v.deleted != true).ToList();

            viewModel.PoTypeList = db.po_type
                .Where(p => p.deleted != true).ToList();
        }

        // Helper: generate PO ID baru
        // Format: po{YYYY}{NNNNN} — counter reset per tahun
        private string GenerateNextPoId()
        {
            int year = DateTime.Now.Year;
            string prefix = "PO" + year.ToString();

            var lastId = db.Operationals
                .Where(o => o.po_id.StartsWith(prefix))
                .OrderByDescending(o => o.po_id)
                .Select(o => o.po_id)
                .FirstOrDefault();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastId) && lastId.Length >= prefix.Length)
            {
                string numberPart = lastId.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int parsed))
                {
                    nextNumber = parsed + 1;
                }
            }

            return prefix + nextNumber.ToString("D5");
        }

        // GET: Operational (Index)
        public ActionResult Index()
        {
            var viewModel = db.Operationals
                .Include(o => o.OperationalDetails)
                .Where(o => o.deleted != true)
                .OrderByDescending(o => o.created_at)
                .ToList();

            // Load vendor dictionary untuk display vendor_desc
            var vendorDict = db.Vendors
                .Where(v => v.deleted != true)
                .ToDictionary(v => v.vendor_id, v => v.vendor_desc);
            ViewBag.VendorDict = vendorDict;

            return View(viewModel);
        }

        // AJAX: data Operational untuk DataTables refresh
        public ActionResult GetOperationalData(string search)
        {
            var query = db.Operationals.Where(o => o.deleted != true);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    o.po_id.Contains(search) ||
                    o.vendor.ToString().Contains(search) ||
                    o.status.Contains(search) ||
                    o.company_code.Contains(search));
            }

            var data = query
                .OrderByDescending(o => o.created_at)
                .Select(o => new
                {
                    o.po_id,
                    o.company_code,
                    o.currency,
                    o.currency_rate,
                    o.vendor,
                    o.status,
                    o.Remarks,
                    o.created_at,
                    o.created_by,
                    // Count detail yang aktif
                    detail_count = db.OperationalDetails.Count(d => d.po_id == o.po_id && d.deleted != true),
                    first_po_no = db.OperationalDetails
                    .Where(d => d.po_id == o.po_id && d.deleted != true)
                    .OrderBy(d => d.detail_id)
                    .Select(d => d.po_no)
                    .FirstOrDefault()
                })
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // GET: Operational/Editor
        // mode = "Create" | "Edit" | "View" | "Delete"
        public ActionResult Editor(string id = null, string mode = "Create")
        {
            OperationalViewModel viewModel;

            if (mode == "Create" || string.IsNullOrEmpty(id))
            {
                viewModel = new OperationalViewModel
                {
                    po_id = GenerateNextPoId()
                };
            }
            else
            {
                var existing = db.Operationals
                    .Where(o => o.po_id == id && o.deleted != true)
                    .FirstOrDefault();

                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Operational data not found";
                    return RedirectToAction("Index");
                }

                viewModel = new OperationalViewModel
                {
                    po_id = existing.po_id,
                    company_code = existing.company_code,
                    currency = existing.currency,
                    currency_rate = existing.currency_rate,
                    vendor = existing.vendor,
                    Remarks = existing.Remarks
                };

                ViewBag.CurrentStatus = existing.status;

                // Load details
                viewModel.Details = db.OperationalDetails
                    .Where(d => d.po_id == existing.po_id && d.deleted != true)
                    .OrderBy(d => d.detail_id)
                    .ToList();
            }

            PopulateDropdownLists(viewModel);
            ViewBag.Mode = mode;
            return View(viewModel);
        }

        // POST: Operational/Editor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editor(OperationalViewModel viewModel, string mode, string status_value = null)
        {
            try
            {
                mode = mode ?? "Create";

                if (mode == "Delete")
                {
                    return DeleteTransaction(viewModel.po_id);
                }

                // ---- Safeguard: regenerate PO ID kalau sudah dipakai ----
                if (mode == "Create")
                {
                    if (string.IsNullOrEmpty(viewModel.po_id))
                    {
                        viewModel.po_id = GenerateNextPoId();
                    }
                    else
                    {
                        var existingTransaction = db.Operationals
                            .Where(o => o.po_id == viewModel.po_id && o.deleted != true)
                            .FirstOrDefault();

                        if (existingTransaction != null)
                        {
                            viewModel.po_id = GenerateNextPoId();
                        }
                    }
                }

                // ---- Lookup master data ----
                string companyCode = null;
                if (!string.IsNullOrEmpty(viewModel.company_code))
                {
                    var company = db.Companies.FirstOrDefault(c => c.company_code == viewModel.company_code && c.deleted != true);
                    companyCode = company?.company_code;
                }

                string currencyCode = null;
                if (!string.IsNullOrEmpty(viewModel.currency))
                {
                    var currency = db.Currencies.FirstOrDefault(c => c.ccy_code == viewModel.currency && c.deleted != true);
                    currencyCode = currency?.ccy_code;
                }

                int? vendorId = null;
                if (viewModel.vendor.HasValue)
                {
                    var vendor = db.Vendors.FirstOrDefault(v => v.vendor_id == viewModel.vendor.Value && v.deleted != true);
                    vendorId = vendor?.vendor_id;
                }

                // ---- Required field validation (header only) ----
                if (string.IsNullOrEmpty(viewModel.po_id))
                    ModelState.AddModelError("po_id", "PO ID is required");
                if (string.IsNullOrEmpty(viewModel.company_code))
                    ModelState.AddModelError("company_code", "Company is required");
                if (string.IsNullOrEmpty(viewModel.currency))
                    ModelState.AddModelError("currency", "Currency is required");
                if (!viewModel.vendor.HasValue)
                    ModelState.AddModelError("vendor", "Vendor is required");

                // Resolve status
                string selectedStatus = status_value;
                if (string.IsNullOrEmpty(selectedStatus))
                    ModelState.AddModelError("status", "Status is required");

                if (selectedStatus == "Send To AP")
                {
                    // Cek apakah status lama juga sudah "Send To AP" (allow kalau iya)
                    string oldStatus = null;
                    if (mode == "Edit" && !string.IsNullOrEmpty(viewModel.po_id))
                    {
                        oldStatus = db.Operationals
                            .Where(o => o.po_id == viewModel.po_id && o.deleted != true)
                            .Select(o => o.status)
                            .FirstOrDefault();
                    }

                    if (oldStatus != "Send To AP")
                    {
                        ModelState.AddModelError("status",
                            "Status \"Send To AP\" hanya bisa diubah lewat tombol di halaman Index.");
                    }
                }

                if (ModelState.IsValid)
                {
                    string userName = string.IsNullOrEmpty(User.Identity.Name) ? "System" : User.Identity.Name;

                    if (mode == "Create")
                    {
                        // Insert header
                        var operationalHeader = new Operational
                        {
                            po_id = viewModel.po_id,
                            company_code = companyCode,
                            currency = currencyCode,
                            currency_rate = viewModel.currency_rate,
                            status = selectedStatus,
                            vendor = vendorId,
                            Remarks = viewModel.Remarks,
                            created_at = DateTime.Now,
                            created_by = userName,
                            deleted = false
                        };
                        db.Operationals.Add(operationalHeader);

                        // Insert details (jika ada)
                        if (viewModel.Details != null)
                        {
                            foreach (var detail in viewModel.Details)
                            {
                                // Skip row kosong
                                if (string.IsNullOrEmpty(detail.po_no)) continue;

                                var newDetail = new OperationalDetail
                                {
                                    po_id = viewModel.po_id,
                                    po_no = detail.po_no,
                                    po_type = detail.po_type,
                                    po_date = detail.po_date,
                                    po_dept = detail.po_dept,
                                    amount_dpp = detail.amount_dpp,
                                    amount_vat = detail.amount_vat,
                                    amount_freight = detail.amount_freight,
                                    amount_freight_vat = detail.amount_freight_vat,
                                    remarks_item = detail.remarks_item,
                                    created_at = DateTime.Now,
                                    created_by = userName,
                                    deleted = false
                                };
                                db.OperationalDetails.Add(newDetail);
                            }
                        }
                    }
                    else if (mode == "Edit")
                    {
                        // Update header
                        var existingOperational = db.Operationals
                            .Where(o => o.po_id == viewModel.po_id && o.deleted != true)
                            .FirstOrDefault();

                        if (existingOperational != null)
                        {
                            existingOperational.company_code = companyCode;
                            existingOperational.currency = currencyCode;
                            existingOperational.currency_rate = viewModel.currency_rate;
                            existingOperational.status = selectedStatus;
                            existingOperational.vendor = vendorId;
                            existingOperational.Remarks = viewModel.Remarks;
                            existingOperational.edited_at = DateTime.Now;
                            existingOperational.edited_by = userName;

                            // Update details: strategi replace-all
                            // (soft delete yang lama, tambah yang baru dari form)
                            var oldDetails = db.OperationalDetails
                                .Where(d => d.po_id == viewModel.po_id && d.deleted != true)
                                .ToList();

                            foreach (var old in oldDetails)
                            {
                                old.deleted = true;
                                old.deleted_at = DateTime.Now;
                                old.deleted_by = userName;
                            }

                            if (viewModel.Details != null)
                            {
                                foreach (var detail in viewModel.Details)
                                {
                                    if (string.IsNullOrEmpty(detail.po_no)) continue;

                                    var newDetail = new OperationalDetail
                                    {
                                        po_id = viewModel.po_id,
                                        po_no = detail.po_no,
                                        po_type = detail.po_type,
                                        po_date = detail.po_date,
                                        po_dept = detail.po_dept,
                                        amount_dpp = detail.amount_dpp,
                                        amount_vat = detail.amount_vat,
                                        amount_freight = detail.amount_freight,
                                        amount_freight_vat = detail.amount_freight_vat,
                                        remarks_item = detail.remarks_item,
                                        created_at = DateTime.Now,
                                        created_by = userName,
                                        deleted = false
                                    };
                                    db.OperationalDetails.Add(newDetail);
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Record not found for editing");
                        }
                    }

                    db.SaveChanges();

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            message = mode == "Create"
                                ? "Operational created successfully"
                                : "Operational updated successfully",
                            data = new
                            {
                                po_id = viewModel.po_id,
                                status = selectedStatus,
                                vendor = vendorId
                            }
                        });
                    }

                    TempData["SuccessMessage"] = mode == "Create"
                        ? "Operational created successfully"
                        : "Operational updated successfully";
                    return RedirectToAction("Index");
                }

                // ---- Validation failed ----
                PopulateDropdownLists(viewModel);
                ViewBag.Mode = mode;

                if (Request.IsAjaxRequest())
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    return Json(new { success = false, message = "Validation failed", errors = errors });
                }
                return View(viewModel);
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "An error occurred: " + ex.Message,
                        details = ex.ToString()
                    });
                }

                if (viewModel != null)
                {
                    PopulateDropdownLists(viewModel);
                }
                ViewBag.Mode = mode;
                return View(viewModel ?? new OperationalViewModel());
            }
        }

        // Delete (soft delete header + details)
        public ActionResult DeleteTransaction(string po_id)
        {
            try
            {
                if (string.IsNullOrEmpty(po_id))
                {
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = "PO ID is required" });

                    TempData["ErrorMessage"] = "PO ID is required";
                    return RedirectToAction("Index");
                }

                string userName = string.IsNullOrEmpty(User.Identity.Name) ? "System" : User.Identity.Name;

                // Soft delete header
                var records = db.Operationals
                    .Where(o => o.po_id == po_id && o.deleted != true)
                    .ToList();

                if (!records.Any())
                {
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = "Operational record not found" });

                    TempData["ErrorMessage"] = "Operational record not found";
                    return RedirectToAction("Index");
                }

                foreach (var op in records)
                {
                    op.deleted = true;
                    op.deleted_by = userName;
                    op.deleted_at = DateTime.Now;
                }

                // Soft delete detail terkait
                var details = db.OperationalDetails
                    .Where(d => d.po_id == po_id && d.deleted != true)
                    .ToList();

                foreach (var d in details)
                {
                    d.deleted = true;
                    d.deleted_by = userName;
                    d.deleted_at = DateTime.Now;
                }

                db.SaveChanges();

                if (Request.IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = true,
                        message = "Operational deleted successfully"
                    });
                }

                TempData["SuccessMessage"] = "Operational deleted successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "An error occurred while deleting: " + ex.Message
                    });
                }

                TempData["ErrorMessage"] = "An error occurred while deleting: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: detail JSON
        public ActionResult GetOperationalDetails(string po_id)
        {
            try
            {
                if (string.IsNullOrEmpty(po_id))
                {
                    return Json(new { success = false, message = "PO ID is required" }, JsonRequestBehavior.AllowGet);
                }

                var op = db.Operationals
                    .Where(o => o.po_id == po_id && o.deleted != true)
                    .FirstOrDefault();

                if (op == null)
                {
                    return Json(new { success = false, message = "Record not found" }, JsonRequestBehavior.AllowGet);
                }

                var details = db.OperationalDetails
                    .Where(d => d.po_id == po_id && d.deleted != true)
                    .OrderBy(d => d.detail_id)
                    .Select(d => new
                    {
                        d.detail_id,
                        d.po_no,
                        d.po_type,
                        po_date = d.po_date.HasValue ? d.po_date.Value.ToString("yyyy-MM-dd") : "",
                        d.po_dept,
                        d.amount_dpp,
                        d.amount_vat,
                        d.amount_freight,
                        d.amount_freight_vat,
                        d.remarks_item
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    header = new
                    {
                        op.po_id,
                        op.company_code,
                        op.currency,
                        op.currency_rate,
                        op.status,
                        op.vendor,
                        op.Remarks
                    },
                    details = details
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}