using PaymentNote.Models;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PaymentNote.Controllers
{
    public class CurrencyController : Controller
    {
        private readonly DbPaymentNoteEntities1 db;

        public CurrencyController()
        {
            db = new DbPaymentNoteEntities1();
        }

        // GET: Currency
        public ActionResult Index()
        {
            var currencies = db.Currencies.Where(c => c.deleted != true).ToList();
            return View(currencies);
        }

        public ActionResult Editor(string mode, string id)
        {
            ViewBag.Mode = mode;
            if(mode == "Create")
            {
                return View(new CurrencyViewModel { deleted = false });
            }
            var currency = db.Currencies.Find(id);
            if (currency == null)
            {
                TempData["Error"] = "Company Not Found";
                return RedirectToAction("Index");
            }

            var currencyViewModel = new CurrencyViewModel
            {
                ccy_code = currency.ccy_code,
                ccy_description = currency.ccy_description,
                created_at = currency.created_at,
                created_by = currency.created_by,
                edited_at = currency.edited_at,
                edited_by = currency.edited_by,
                deleted_at = currency.deleted_at,
                deleted_by = currency.deleted_by,
                deleted = currency.deleted
            };
            return View(currencyViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editor(CurrencyViewModel curencyViewModel, string mode)
        {
            try
            {
                var currentUsername = GetCurrentUsername();
                if(mode == "Create")
                {
                    var currencyExist = db.Currencies.FirstOrDefault(c => c.ccy_code == curencyViewModel.ccy_code);
                    if(currencyExist != null && currencyExist.deleted == true)
                    {
                        currencyExist.ccy_description = curencyViewModel.ccy_description;
                        currencyExist.deleted = false;
                        currencyExist.created_at = DateTime.Now;
                        currencyExist.created_by = currentUsername;
                        currencyExist.edited_at = null;
                        currencyExist.edited_by = null;
                        currencyExist.deleted_at = null;
                        currencyExist.deleted_by = null;
                    }
                    else if(currencyExist != null && currencyExist.deleted != true)
                    {
                        TempData["Error"] = "Currency Code already exists";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        var currency = new Currency
                        {
                            ccy_code = curencyViewModel.ccy_code,
                            ccy_description = curencyViewModel.ccy_description,
                            created_at = DateTime.Now,
                            created_by = currentUsername,
                            deleted = false
                        };
                        db.Currencies.Add(currency);
                    }
                }
                else if(mode == "Edit")
                {
                    var currencyExist = db.Currencies.Find(curencyViewModel.ccy_code);
                    if (currencyExist != null)
                    {
                        currencyExist.ccy_code = curencyViewModel.ccy_code;
                        currencyExist.ccy_description = curencyViewModel.ccy_description;
                        currencyExist.edited_at = DateTime.Now;
                        currencyExist.edited_by = currentUsername;
                    }
                    else
                    {
                        TempData["Error"] = "Currency Not Found";
                        return RedirectToAction("Index");
                    }
                }
                else if(mode == "Delete")
                {
                    var currencyExist = db.Currencies.Find(curencyViewModel.ccy_code);
                    if(currencyExist != null)
                    {
                        currencyExist.deleted = true;
                        currencyExist.deleted_at = DateTime.Now;
                        currencyExist.deleted_by = currentUsername;
                    }
                    else
                    {
                        TempData["Error"] = "Currency Not Found";
                        return RedirectToAction("Index");
                    }
                }
                db.SaveChanges();
                TempData["Success"] = $"Currency {mode}d successfully";
                return RedirectToAction("Index");
            }
            catch(Exception ex)
            {
                TempData["Error"] = $"Error while {mode}ing company: {ex.Message}";
                return RedirectToAction("Index");
            }
            ViewBag.Mode = mode;
            return View(curencyViewModel);
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