using PaymentNote.Models;
using PaymentNote.Services;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace PaymentNote.Controllers
{
    public class UserDataController : Controller
    {
        public readonly DbPaymentNoteEntities1 db;
        public readonly IUserServices userServices;

        public UserDataController()
        {
            this.db = new DbPaymentNoteEntities1();
            this.userServices = new UserServices(this.db);
        }

        public UserDataController(DbPaymentNoteEntities1 db, IUserServices services)
        {
            this.db = db;
            this.userServices = services;
        }

        public ActionResult Index()
        {
            var users = db.Users.Where(u => u.deleted != true).Select(u => new userData
            {
                user_id = u.user_id,
                username = u.username,
                first_name = u.first_name,
                password = u.password,
                location = u.location,
                isAdmin = u.is_admin ?? false,
                deleted = (bool)u.deleted,
                Department = u.Department,
                LastLogin = u.Last_Login
            }).ToList();

            return View("~/Views/Users/Index.cshtml", users);
        }

        [HttpGet]
        public ActionResult Editor(int? id, string mode = "View")
        {
            try
            {
                DropDownList(); 

                var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "View" : mode;
                ViewBag.Mode = normalizedMode;

                if (string.Equals(normalizedMode, "Create", StringComparison.OrdinalIgnoreCase))
                {
                    return View("~/Views/Users/Editor.cshtml", new userData());
                }

                if (!id.HasValue)
                {
                    TempData["message"] = "User id is required.";
                    return RedirectToAction("Index");
                }

                var user = db.Users.FirstOrDefault(u => u.user_id == id.Value && u.deleted != true);
                if (user == null)
                {
                    TempData["message"] = "User not found.";
                    return RedirectToAction("Index");
                }

                var model = new userData
                {
                    user_id = user.user_id,
                    username = user.username,
                    first_name = user.first_name,
                    password = user.password,
                    location = user.location,
                    isAdmin = user.is_admin ?? false,
                    deleted = user.deleted ?? false,
                    Department = user.Department,
                    LastLogin = user.Last_Login
                };

                return View("~/Views/Users/Editor.cshtml", model);
            }
            catch (Exception ex)
            {
                TempData["message"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editor(userData userDataModel, string mode)
        {
            try
            {
                var normalizedMode = (mode ?? string.Empty).Trim();
                bool sessionCanManageAdmin =
                    (Session["IsAdmin"] is bool sessionIsAdmin && sessionIsAdmin) ||
                    (Session["is_admin"] is bool legacySessionIsAdmin && legacySessionIsAdmin);

                var isAdminValues = Request?.Form?.GetValues("isAdmin") ?? Request?.Form?.GetValues("is_admin");
                bool postedIsAdmin = isAdminValues != null && isAdminValues.Any(v =>
                    !string.IsNullOrWhiteSpace(v) &&
                    (v.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                     v.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                     v.Equals("1", StringComparison.OrdinalIgnoreCase)));

                switch (normalizedMode.ToLowerInvariant())
                {
                    case "create":
                        userDataModel.username = (userDataModel.username ?? string.Empty).Trim();

                        var existingUser = db.Users.FirstOrDefault(u => u.username == userDataModel.username);
                        if (existingUser != null && existingUser.deleted != true)
                        {
                            TempData["message"] = "Username already exists.";
                            TempData["ErrorMessage"] = "Username already exists.";
                            DropDownList();
                            ViewBag.Mode = "Create";
                            return View("~/Views/Users/Editor.cshtml", userDataModel);
                        }

                        // Validate required fields before inserting/updating.
                        if (string.IsNullOrEmpty(userDataModel.username) ||
                            string.IsNullOrEmpty(userDataModel.password))
                        {
                            TempData["message"] = "Username and Password are required.";
                            TempData["ErrorMessage"] = "Username and Password are required.";
                            DropDownList();
                            ViewBag.Mode = "Create";
                            return View("~/Views/Users/Editor.cshtml", userDataModel);
                        }

                        if (existingUser != null && existingUser.deleted == true)
                        {
                            existingUser.first_name = userDataModel.first_name ?? string.Empty;
                            existingUser.password = userServices.HashPassword(userDataModel.password);
                            existingUser.location = userDataModel.location;
                            existingUser.Department = userDataModel.Department;
                            existingUser.is_admin = sessionCanManageAdmin ? postedIsAdmin : false;
                            existingUser.deleted = false;
                            existingUser.deleted_at = null;
                            existingUser.deleted_by = null;
                            existingUser.edited_at = DateTime.Now;
                            existingUser.edited_by = Session["username"]?.ToString() ?? "System";
                        }
                        else
                        {
                            var newUser = new User
                            {
                                username = userDataModel.username,
                                first_name = userDataModel.first_name ?? string.Empty,
                                password = userServices.HashPassword(userDataModel.password),
                                location = userDataModel.location,
                                is_admin = sessionCanManageAdmin ? postedIsAdmin : false,
                                created_at = DateTime.Now,
                                created_by = Session["username"]?.ToString() ?? "System",
                                deleted = false,
                                Department = userDataModel.Department
                            };

                            db.Users.Add(newUser);
                        }

                        db.SaveChanges();
                        TempData["message"] = "User Created Successfully";
                        TempData["SuccessMessage"] = "User Created Successfully";
                        break;

                    case "edit":
                        if (userDataModel.user_id <= 0)
                        {
                            TempData["message"] = "Invalid User ID.";
                            TempData["ErrorMessage"] = "Invalid User ID.";
                            return RedirectToAction("Index");
                        }

                        // Fallback binding for legacy input names still used in the view/browser cache.
                        if (string.IsNullOrWhiteSpace(userDataModel.first_name))
                        {
                            userDataModel.first_name = Request["Long_Name"];
                        }
                        if (string.IsNullOrWhiteSpace(userDataModel.location))
                        {
                            userDataModel.location = Request["Location"];
                        }
                        if (string.IsNullOrWhiteSpace(userDataModel.password))
                        {
                            userDataModel.password = Request["Password"];
                        }
                        if (Request["is_admin"] != null)
                        {
                            bool parsedIsAdmin;
                            if (bool.TryParse(Request["is_admin"], out parsedIsAdmin))
                            {
                                userDataModel.isAdmin = parsedIsAdmin;
                            }
                        }

                        var userEdit = db.Users.Find(userDataModel.user_id);
                        if (userEdit == null || userEdit.deleted == true)
                        {
                            TempData["message"] = "User not found.";
                            TempData["ErrorMessage"] = "User not found.";
                            return RedirectToAction("Index");
                        }

                        userEdit.first_name = userDataModel.first_name;
                        userEdit.location = userDataModel.location;
                        userEdit.Department = userDataModel.Department;
                        userEdit.edited_at = DateTime.Now;
                        userEdit.edited_by = Session["username"]?.ToString() ?? "System";
                        // ✅ Hanya update isAdmin kalau yang login adalah admin
                        if (sessionCanManageAdmin)
                        {
                            userEdit.is_admin = postedIsAdmin;
                        }

                        // ✅ Hanya update password kalau diisi
                        if (!string.IsNullOrEmpty(userDataModel.password))
                        {
                            userEdit.password = userServices.HashPassword(userDataModel.password);
                        }

                        db.SaveChanges();
                        TempData["message"] = "User Updated Successfully";
                        TempData["SuccessMessage"] = "User Updated Successfully";
                        break;

                    case "delete":
                        if (userDataModel.user_id <= 0)
                        {
                            TempData["message"] = "Invalid User ID.";
                            TempData["ErrorMessage"] = "Invalid User ID.";
                            return RedirectToAction("Index");
                        }

                        var userDelete = db.Users.Find(userDataModel.user_id);
                        if (userDelete == null || userDelete.deleted == true)
                        {
                            TempData["message"] = "User not found.";
                            TempData["ErrorMessage"] = "User not found.";
                            return RedirectToAction("Index");
                        }

                        // ✅ Cegah hapus diri sendiri
                        if (userDelete.username == Session["username"]?.ToString())
                        {
                            TempData["message"] = "You cannot delete your own account.";
                            TempData["ErrorMessage"] = "You cannot delete your own account.";
                            return RedirectToAction("Index");
                        }

                        userDelete.deleted = true;
                        userDelete.deleted_at = DateTime.Now;
                        userDelete.deleted_by = Session["username"]?.ToString() ?? "System";
                        db.SaveChanges();
                        TempData["message"] = "User Deleted Successfully";
                        TempData["SuccessMessage"] = "User Deleted Successfully";
                        break;

                    default:
                        TempData["message"] = "Invalid mode.";
                        TempData["ErrorMessage"] = "Invalid mode.";
                        return RedirectToAction("Index");
                }

                return RedirectToAction("Index");
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var innerMsg = ex.InnerException?.InnerException?.Message
                               ?? ex.InnerException?.Message
                               ?? ex.Message;

                System.Diagnostics.Debug.WriteLine($"DB Error: {innerMsg}");
                TempData["message"] = "DB Error: " + innerMsg;
                TempData["ErrorMessage"] = "DB Error: " + innerMsg;

                DropDownList();
                ViewBag.Mode = mode;
                return View("~/Views/Users/Editor.cshtml", userDataModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                TempData["message"] = "Error: " + ex.Message;
                TempData["ErrorMessage"] = "Error: " + ex.Message;

                DropDownList();
                ViewBag.Mode = mode;
                return View("~/Views/Users/Editor.cshtml", userDataModel);
            }
        }
        [HttpPost]
        public JsonResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                {
                    return Json(new { success = false, message = "All fields are required" });
                }
                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "New password and confirmation do not match" });
                }
                string currentUsername = Session["username"]?.ToString();
                if (string.IsNullOrEmpty(currentUsername))
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                var user = db.Users.FirstOrDefault(u => u.username == currentUsername && u.deleted != true);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (!userServices.VerifyPassword(oldPassword, user.password))
                {
                    return Json(new { success = false, message = "Old password is incorrect" });
                }

                user.password = userServices.HashPassword(newPassword);
                user.edited_at = DateTime.Now;
                user.edited_by = currentUsername;

                db.SaveChanges();

                return Json(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        private void DropDownList()
        {
          try
            {
                ViewBag.Department = db.Departments
                    .Where(d => d.deleted == false || d.deleted == null)
                    .Select(d => new SelectListItem
                    {
                        Value = d.department_code,
                        Text = d.department_code
                    })
                    .ToList();
                ViewBag.Location = db.Locations
            .Where(l => l.deleted == false || l.deleted == null)
            .Select(l => new SelectListItem
            {
                Value = l.location_code,    // ⚠️ Ganti sesuai nama kolom di DB
                Text = l.location_name      // ⚠️ Ganti sesuai nama kolom di DB
            }).ToList();
            }
            catch (Exception ex)
            {
                TempData["message"] = "An error occurred while loading dropdown data: " + ex.Message;
            }
        }
    }
}