using PaymentNote.Models;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PaymentNote.Controllers
{
    public class LocationController : Controller
    {
        private readonly DbPaymentNoteEntities2  db = new DbPaymentNoteEntities2();

        // GET: Location
        public ActionResult Index()
        {
            var locations = db.Locations.Where(l => l.deleted != true).ToList();
            return View(locations);
        }

        public ActionResult Editor(string id, string mode)
        {
            ViewBag.Mode = mode;

            var viewModel = new LocationViewModel();

            if (mode == "Create")
            {
                return View(viewModel);
            }

            var location = db.Locations.Find(id);
            if(location == null)
            {
                TempData["ErrorMessage"] = "Location not found.";
                return RedirectToAction("Index");
            }

            var locationViewModel = new LocationViewModel
            {
                location_code = location.location_code,
                location_name = location.location_name,
                created_at = location.created_at,
                created_by = location.created_by,
                edited_at = location.edited_at,
                edited_by = location.edited_by,
                deleted_by = location.deleted_by,
                deleted_at = location.deleted_at,
                deleted = location.deleted ?? false
            };
            return View(locationViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editor(LocationViewModel viewModel, string mode)
        {
            try
            {
                var currentUsername = GetCurrentUsername();

                if (ModelState.IsValid)
                {
                    if(mode == "Create")
                    {
                        var existingLocation = db.Locations.FirstOrDefault(l => l.location_code == viewModel.location_code);

                        if(existingLocation != null  && existingLocation.deleted != true)
                        {
                            ModelState.AddModelError("location_code", "Location code already exists.");
                            return View(viewModel);
                        }

                        if(existingLocation != null && existingLocation.deleted == true)
                        {
                            existingLocation.location_name = viewModel.location_name;
                            existingLocation.deleted = false;
                            existingLocation.created_by = currentUsername;
                            existingLocation.created_at = DateTime.Now;
                            existingLocation.deleted_by = null;
                            existingLocation.deleted_at = null;

                            db.SaveChanges();
                            TempData["SuccessMessage"] = "Location recreated successfully.";
                            return RedirectToAction("Index");
                        }

                        var location = new Location
                        {
                            location_code = viewModel.location_code,
                            location_name= viewModel.location_name,
                            deleted = false,
                            created_by = currentUsername,
                            created_at = DateTime.Now
                        };
                        db.Locations.Add(location);
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Location created successfully.";
                        return RedirectToAction("Index");
                    }
                    else if(mode == "Edit")
                    {
                        var locationExist = db.Locations.Find(viewModel.location_code);
                        if(locationExist != null)
                        {
                            locationExist.location_name = viewModel.location_name;
                            locationExist.edited_by = currentUsername;
                            locationExist.edited_at = DateTime.Now;

                            db.SaveChanges();
                            TempData["SuccessMessage"] = "Location updated successfully.";
                            return RedirectToAction("Index");
                        }
                    }
                    else if(mode == "Delete")
                    {
                        var locationExist = db.Locations.Find(viewModel.location_code);
                        if(locationExist != null)
                        {
                            locationExist.deleted = true;
                            locationExist.deleted_by = currentUsername;
                            locationExist.deleted_at = DateTime.Now;
                            db.SaveChanges();
                            TempData["SuccessMessage"] = "Location deleted successfully.";
                            return RedirectToAction("Index");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
            }

            ViewBag.Mode = mode;
            return View(viewModel);
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