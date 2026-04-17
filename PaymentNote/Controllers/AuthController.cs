using PaymentNote.Models;
using PaymentNote.Services;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace PaymentNote.Controllers
{
    public class AuthController : Controller
    {
        private readonly DbPaymentNoteEntities1 _db;
        private readonly IUserServices _userServices;

        public AuthController()
        {
            _db = new DbPaymentNoteEntities1();
            _userServices = new UserServices(_db);
        }

        public AuthController(IUserServices userServices, DbPaymentNoteEntities1 db)
        {
            _userServices = userServices;
            _db = db;
        }

        // GET: Auth
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _userServices.InitializeAdmin();

            var user = _userServices.AuthenticateUser(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            System.Diagnostics.Debug.WriteLine($"Authentication successful for user: {user.username}");

            user.Last_Login = DateTime.Now;
            _db.SaveChanges();

            FormsAuthentication.SetAuthCookie(user.username, model.RememberMe);
            Session["User"] = user;
            Session["username"] = user.username;
            Session["department"] = user.Department;
            Session["Location"] = user.location;
            Session["IsAdmin"] = user.is_admin;

            if (user.is_admin == true)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();

            HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, "");
            cookie.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(cookie);
            return RedirectToAction("Login", "Auth");
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (!string.IsNullOrEmpty(oldPassword) || !string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(confirmPassword))
                {
                    TempData["Message"] = "Please fill in all fields";
                    return RedirectToAction("Index", "Home");
                }
                if (newPassword != confirmPassword)
                {
                    TempData["Message"] = "New password and confirmation do not match";
                    return RedirectToAction("Index", "Home");
                }
                string currentUsername = Session["Username"]?.ToString();


                var users = _db.Users.FirstOrDefault(u => u.username == currentUsername && u.deleted != true);
                if (users == null)
                {
                    TempData["Message"] = "User not found";
                    return RedirectToAction("Index", "Home");
                }

                if (!_userServices.VerifyPassword(users.password, oldPassword))
                {
                    TempData["Message"] = "Current is incorrect";
                    return RedirectToAction("Index", "Home");
                }
                users.password = _userServices.HashPassword(newPassword);
                users.Last_Login = DateTime.Now;
                _db.SaveChanges();
                TempData["Message"] = "Password changed successfully";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "An error occurred while changing the password: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }

        }
    }
}