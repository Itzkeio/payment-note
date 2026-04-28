using PaymentNote.Models;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PaymentNote.Controllers
{
    public class CompanyController : Controller
    {
        private readonly DbPaymentNoteEntities2 db;

        public CompanyController()
        {
            db = new DbPaymentNoteEntities2();
        }
        // GET: Company
        public ActionResult Index()
        {
            var companies = db.Companies.Where(d => d.deleted != true).ToList();
            return View(companies);
        }

        public ActionResult Editor(string mode, string id)
        {
            ViewBag.Mode = mode;
            if (mode == "Create")
            {
                return View(new CompanyViewModel { deleted = false });
            }
            var company = db.Companies.Find(id);
            if (company == null)
            {
                TempData["Error"] = "Company Not Found";
                return RedirectToAction("Index");
            }

            var companyViewModel = new CompanyViewModel
            {
                company_code = company.company_code,
                company_description = company.company_description,
                created_by = company.created_by,
                created_at = company.created_at,
                edited_by = company.edited_by,
                edited_at = company.edited_at,
                deleted_at = company.deleted_at,
                deleted = company.deleted,
                deleted_by = company.deleted_by
            };
            return View(companyViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editor(CompanyViewModel companyViewModel, string mode)
        {
            try
            {
                var currentUsername = GetCurrentUsername();
                if (mode == "Create")
                {
                    var companyExist = db.Companies.FirstOrDefault(c => c.company_code == companyViewModel.company_code);
                    if (companyExist != null && companyExist.deleted == true)
                    {
                        companyExist.company_description = companyViewModel.company_description;
                        companyExist.deleted = false;
                        companyExist.created_by = currentUsername;
                        companyExist.created_at = DateTime.Now;
                        companyExist.edited_by = null;
                        companyExist.edited_at = null;
                        companyExist.deleted_by = null;
                        companyExist.deleted_at = null;
                    }
                    else if (companyExist != null && companyExist.deleted != true)
                    {
                        TempData["Error"] = "Company Code already exist";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        var company = new Company
                        {
                            company_code = companyViewModel.company_code,
                            company_description = companyViewModel.company_description,
                            created_by = currentUsername,
                            created_at = DateTime.Now,
                            deleted = false
                        };
                        db.Companies.Add(company);
                    }
                }
                else if (mode == "Edit")
                {
                    var companyExist = db.Companies.Find(companyViewModel.company_code);
                    if (companyExist != null)
                    {
                        companyExist.company_code = companyViewModel.company_code;
                        companyExist.company_description = companyViewModel.company_description;
                        companyExist.edited_by = currentUsername;
                        companyExist.edited_at = DateTime.Now;
                    }
                    else
                    {
                        TempData["Error"] = "Company Not Found";
                        return RedirectToAction("Index");
                    }
                }
                else if (mode == "Delete")
                {
                    var companyExist = db.Companies.Find(companyViewModel.company_code);
                    if (companyExist != null)
                    {
                        companyExist.deleted = true;
                        companyExist.deleted_by = currentUsername;
                        companyExist.deleted_at = DateTime.Now;
                    }
                    else
                    {
                        TempData["Error"] = "Company Not Found";
                        return RedirectToAction("Index");
                    }
                }
                db.SaveChanges();
                TempData["Success"] = $"Company {mode}d successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error while {mode}ing company: {ex.Message}";
                return RedirectToAction("Index");
            }
            ViewBag.Mode = mode;
            return View(companyViewModel);
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