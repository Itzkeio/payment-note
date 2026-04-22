using PaymentNote.Models;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace PaymentNote.Controllers
{
    public class PoTypeController : Controller
    {
        private readonly DbPaymentNoteEntities1 db;

        public PoTypeController()
        {
            db = new DbPaymentNoteEntities1();
        }

        // GET: PoType
        public ActionResult Index()
        {
            var po_type = db.po_type.Where(d => d.deleted != true).ToList();
            return View(po_type);
        }

        public ActionResult Editor(string mode, string id)
        {
            ViewBag.Mode = mode;
            if (mode == "Create")
            {
                return View(new PoTypeViewModel { deleted = false });
            }
            var poType = db.po_type.Where(d => d.type_code == id).FirstOrDefault();
            if (poType == null)
            {
                TempData["Error"] = "PO Type not found.";
                return RedirectToAction("Index");
            }

            var poTypeViewModel = new PoTypeViewModel
            {
                type_code = poType.type_code,
                type_desc = poType.type_desc,
                created_at = poType.created_at,
                created_by = poType.created_by,
                edited_at = poType.edited_at,
                edited_by = poType.edited_by,
                deleted_at = poType.deleted_at,
                deleted_by = poType.deleted_by,
                deleted = poType.deleted
            };
            return View(poTypeViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editor(PoTypeViewModel poTypeViewModel, string mode)
        {
            try
            {
                var currentUsename = GetCurrentUsername();
                if (mode == "Create")
                {
                    var PoTypeExist = db.po_type.FirstOrDefault(p => p.type_code == poTypeViewModel.type_code);
                    if (PoTypeExist != null && PoTypeExist.deleted == true)
                    {
                        PoTypeExist.type_desc = poTypeViewModel.type_desc;
                        PoTypeExist.deleted = false;
                        PoTypeExist.created_at = DateTime.Now;
                        PoTypeExist.created_by = currentUsename;
                        PoTypeExist.edited_at = null;
                        PoTypeExist.edited_by = null;
                        PoTypeExist.deleted_at = null;
                        PoTypeExist.deleted_by = null;
                    }
                    else if (PoTypeExist != null && PoTypeExist.deleted != true)
                    {
                        TempData["Error"] = "PO Type code already exists.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        var poType = new po_type
                        {
                            type_code = poTypeViewModel.type_code,
                            type_desc = poTypeViewModel.type_desc,
                            created_at = DateTime.Now,
                            created_by = currentUsename,
                            deleted = false
                        };
                        db.po_type.Add(poType);
                    }
                }
                else if (mode == "Edit")
                {
                    var poTypeExist = db.po_type.Find(poTypeViewModel.type_code);
                    if (poTypeExist == null)
                    {
                        TempData["Error"] = "PO Type not found.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        poTypeExist.type_desc = poTypeViewModel.type_desc;
                        poTypeExist.edited_at = DateTime.Now;
                        poTypeExist.edited_by = currentUsename;
                    }
                }
                else if (mode == "Delete")
                {
                    var poTypeExist = db.po_type.Find(poTypeViewModel.type_code);
                    if (poTypeExist != null)
                    {
                        poTypeExist.deleted = true;
                        poTypeExist.deleted_at = DateTime.Now;
                        poTypeExist.deleted_by = currentUsename;

                    }
                    else
                    {
                        TempData["Error"] = "PO Type not found.";
                        return RedirectToAction("Index");
                    }
                }
                db.SaveChanges();
                TempData["Success"] = $"PO Type {mode}d successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while processing your request: {ex.Message}";
                return RedirectToAction("Index");
            }
            ViewBag.Mode = mode;
            return View(poTypeViewModel);
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