using PaymentNote.Models;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PaymentNote.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly DbPaymentNoteEntities2 _db;

        public DepartmentController()
        {
            _db = new DbPaymentNoteEntities2();
        }
        // GET: Department
        public ActionResult Index()
        {
            var departments = _db.Departments.Where(d => d.deleted !=true).ToList();
            return View(departments);
        }

        public ActionResult Editor(string mode, string id)
        {
            ViewBag.Mode = mode;
            if(mode == "Create")
            {
                return View(new DepartmentViewModel { deleted = false });
            }
            var department = _db.Departments.Find(id);
            if(department == null)
            {
                TempData["Error"] = "Department not found.";
                return RedirectToAction("Index");
            }

            var departmentViewModel = new DepartmentViewModel
            {
                department_code = department.department_code,
                Role = department.Role,
                created_at = department.created_at,
                created_by = department.created_by,
                edited_by = department.edited_by,
                edited_at = department.edited_at,
                deleted_at = department.deleted_at,
                deleted = department.deleted
            };
            return View(departmentViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editor(DepartmentViewModel departmentViewModel, string mode)
        {
            try
            {
                var currentUsername = GetCurrentUsername();

                if(mode == "Create")
                {
                    var departmentExist = _db.Departments.FirstOrDefault(d => d.department_code == departmentViewModel.department_code);
                    if(departmentExist !=null && departmentExist.deleted == true)
                    {
                        departmentExist.Role = departmentViewModel.Role;
                        departmentExist.deleted = false;
                        departmentExist.created_by = currentUsername;
                        departmentExist.created_at = DateTime.Now;
                        departmentExist.edited_by = null;
                        departmentExist.edited_at = null;
                        departmentExist.deleted_at = null;
                        departmentExist.deleted = false;
                    }
                    else if(departmentExist != null && departmentExist.deleted != true)
                    {
                        TempData["ErrorMessage"] = "Departement Code already exists.";
                        ViewBag.Mode = mode;
                        return View(departmentViewModel);
                    }
                    else
                    {
                        var newDepartment = new Department
                        {
                            department_code = departmentViewModel.department_code,
                            Role = departmentViewModel.Role,
                            deleted = false,
                            created_by = currentUsername,
                            created_at = DateTime.Now
                        };
                        _db.Departments.Add(newDepartment);
                    }
                }
                else if(mode == "Edit")
                {
                    var departmentExist = _db.Departments.Find(departmentViewModel.department_code);
                    if(departmentExist != null)
                    {
                        departmentExist.department_code = departmentViewModel.department_code;
                        departmentExist.Role = departmentViewModel.Role;
                        departmentExist.edited_by = currentUsername;
                        departmentExist.edited_at = DateTime.Now;
                    }
                }
                else if(mode == "Delete")
                {
                    var departmentExist = _db.Departments.Find(departmentViewModel.department_code);
                    if(departmentExist != null)
                    {
                        departmentExist.deleted = true;
                        departmentExist.deleted_at = DateTime.Now;
                        departmentExist.edited_by = currentUsername;
                        departmentExist.edited_at = DateTime.Now;
                    }
                }
                _db.SaveChanges();
                TempData["Success"] = "Departement saved successfully.";
                return RedirectToAction("Index");
            }
            catch(Exception ex)
            {
                TempData["Error"] = "An error occurred while saving the departement: " + ex.Message;
            }
            ViewBag.Mode = mode;
            return View(departmentViewModel);
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