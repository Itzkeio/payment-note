using PaymentNote.Models;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PaymentNote.Controllers
{
    public class VendorController : Controller
    {
        private readonly DbPaymentNoteEntities2 db;

        public VendorController()
        {
            db = new DbPaymentNoteEntities2();
        }

        // GET: Vendor
        public ActionResult Index()
        {
            var vendors = db.Vendors.Where(v => v.deleted != true).ToList();
            return View(vendors);
        }

        public ActionResult Editor (string mode, string id)
        {
            ViewBag.Mode = mode;
            var viewModel = new VendorViewModel();

            if (mode == "Create")
            {
                return View(viewModel);
            }
            var vendor = db.Vendors.Find(id);
            if(vendor == null)
            {
                TempData["Error"] = "Vendor not found.";
                return RedirectToAction("Index");
            }

            var vendorViewModel = new VendorViewModel
            {
                vendor_semesta_code = vendor.vendor_semesta_code,
                vendor_sap_code = vendor.vendor_sap_code,
                vendor_desc = vendor.vendor_desc,
                created_at = vendor.created_at,
                created_by = vendor.created_by,
                edited_at = vendor.edited_at,
                edited_by = vendor.edited_by,
                deleted_at = vendor.deleted_at,
                deleted_by = vendor.deleted_by,
                deleted = vendor.deleted
            };
            return View(vendorViewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editor(VendorViewModel vendorViewModel, string mode)
        {
            try
            {
                var currentUsername = GetCurrentUsername();
                if(mode == "Create")
                {
                    var vendorExist = db.Vendors.FirstOrDefault(v => v.vendor_semesta_code == vendorViewModel.vendor_semesta_code);
                    if(vendorExist != null && vendorExist.deleted == true)
                    {
                        vendorExist.vendor_sap_code = vendorViewModel.vendor_sap_code;
                        vendorExist.vendor_desc = vendorViewModel.vendor_desc;
                        vendorExist.deleted = false;    
                        vendorExist.created_at = DateTime.Now;
                        vendorExist.created_by = currentUsername;
                        vendorExist.edited_at = null;
                        vendorExist.edited_by = null;
                        vendorExist.deleted_at = null;
                        vendorExist.deleted_by = null;
                    }
                    else if(vendorExist != null && vendorExist.deleted == false)
                    {
                        TempData["Error"] = "Vendor with this Semesta Code already exists.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        var newVendor = new Vendor
                        {
                            vendor_semesta_code = vendorViewModel.vendor_semesta_code,
                            vendor_sap_code = vendorViewModel.vendor_sap_code,
                            vendor_desc = vendorViewModel.vendor_desc,
                            created_at = DateTime.Now,
                            created_by = currentUsername,
                            deleted = false
                        };
                        db.Vendors.Add(newVendor);
                    }
                }
                else if(mode == "Edit")
                {
                    var vendor = db.Vendors.Find(vendorViewModel.vendor_semesta_code);
                    if (vendor != null)
                    {
                        vendor.vendor_sap_code = vendorViewModel.vendor_sap_code;
                        vendor.vendor_desc = vendorViewModel.vendor_desc;
                        vendor.edited_at = DateTime.Now;
                        vendor.edited_by = currentUsername;
                    }
                    else
                    {
                        TempData["Error"] = "Vendor not found.";
                        return RedirectToAction("Index");
                    }
                }
                else if(mode == "Delete")
                {
                    var vendor = db.Vendors.Find(vendorViewModel.vendor_semesta_code);
                    if (vendor != null)
                    {
                        vendor.deleted = true;
                        vendor.deleted_at = DateTime.Now;
                        vendor.deleted_by = currentUsername;
                    }
                    else
                    {
                        TempData["Error"] = "Vendor not found.";
                        return RedirectToAction("Index");
                    }
                }
                db.SaveChanges();
                TempData["Success"] = $"Vendor {mode}d successfully.";
                return RedirectToAction("Index");
            }
            catch(Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
            ViewBag.Mode = mode;
            return View(vendorViewModel);
        }
        private string GetCurrentUsername()
        {
            if (Session != null)
            {
                var username = Session["username"]?.ToString();
                if (!string.IsNullOrWhiteSpace(username))
                {
                    return username;
                }
            }

            if (User?.Identity != null && User.Identity.IsAuthenticated && !string.IsNullOrWhiteSpace(User.Identity.Name))
            {
                return User.Identity.Name;
            }

            return "System";
        }
    }
}
