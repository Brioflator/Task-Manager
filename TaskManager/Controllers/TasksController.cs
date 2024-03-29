﻿using Microsoft.AspNetCore.Mvc;
using Task = TaskManager.Models.Task;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Controllers
{

    [Authorize(Roles = "User,Admin")]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public TasksController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            var userid = _userManager.GetUserId(User);
            Task task = db.Tasks.Include("Comments").Include("Project").Where(tsk => tsk.TaskId == id).First();
            if (userid == task.Project.UserId || User.IsInRole("Admin"))
            {
                db.Tasks.Remove(task);
                db.SaveChanges();
                TempData["message"] = "Taskul a fost sters";
                return Redirect("/Projects/Show/" + task.ProjectId);
            }
            else
            {
                TempData["message"] = "Error! Nu ai acces";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Index");
            }
        }

        
        public IActionResult Show(int id)
        {

            var task = db.Tasks
                .Include("Comments.User")
                .Include("User")
                .Include("Project")
                                .Where(tsk => tsk.TaskId == id).First();
            ViewBag.Users = db.UserProjects.Include("User")
                .Where(c => c.ProjectId == task.ProjectId).OrderBy(c => c.User.UserName); ///de sortat dupa nume .Select(c => c.UserId, c.User.UserName)
            if (CheckUser(task.ProjectId) || User.IsInRole("Admin"))
            {
                task.Statuses = GetAllStatuses();
                ViewBag.CurrentStatus = task.TaskStatus;
                return View(task);
            }
            else
            {
                TempData["message"] = "Error! Nu ai acces";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Index");
            }
        }

        [HttpPost]
        public IActionResult Show([FromForm] Comment comment)
        {
            Task task = db.Tasks.Include("Comments.User").Include("User").Include("Project").Where(tsk => tsk.TaskId == comment.TaskId).First();
            if (CheckUser(task.ProjectId) || User.IsInRole("Admin"))
            {
                comment.Date = DateTime.Now;
                comment.UserId = _userManager.GetUserId(User);
                if (ModelState.IsValid)
                {
                    db.Comments.Add(comment);
                    db.SaveChanges();
                    return Redirect("/Tasks/Show/" + comment.TaskId);

                }
                else
                {
                    ViewBag.Users = db.UserProjects.Include("User")
                        .Where(c => c.ProjectId == task.ProjectId).OrderBy(c => c.User.UserName);
                    task.Statuses = GetAllStatuses();
                    ViewBag.CurrentStatus = task.TaskStatus;
                    return View(task);
                }

            }
            else
            {
                TempData["message"] = "Error! Nu ai acces";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Index");
            }
        }
        [HttpPost]
        public IActionResult AddUser([FromForm] int TaskId, [FromForm] string userId)
        {
            var userid = _userManager.GetUserId(User);
            Task taskaux = db.Tasks.Include("Project").Where(a => a.TaskId == TaskId).First();
            if (userid == taskaux.Project.UserId || User.IsInRole("Admin"))
            {
                if (ModelState.IsValid && (userId != "Add a user for this task"))
                {
                    if (db.Tasks.Where(task => task.TaskId == TaskId && task.UserId == userId).Count() > 0)
                    {
                        TempData["message"] = "User already has this task";
                        TempData["messageType"] = "alert-danger";
                    }
                    else
                    {
                        Task task = db.Tasks.Find(TaskId);
                        if (task != null)
                        {
                            task.UserId = userId;
                            db.SaveChanges();

                            TempData["message"] = "User added to task";
                            TempData["messageType"] = "alert-success";
                        }
                        else
                        {
                            TempData["message"] = "Database error!";
                            TempData["messageType"] = "alert-danger";
                        }
                    }

                }
                else
                {
                    TempData["message"] = "Try again!";
                    TempData["messageType"] = "alert-danger";
                }

                return Redirect("/Tasks/Show/" + TaskId);
            }
            else
            {
                TempData["message"] = "Error! Nu ai acces";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Index");
            }
        }

        [HttpPost]
        public IActionResult ChangeStatus([FromForm] int TaskId, [FromForm] string newStatus)
        {
            var userid = _userManager.GetUserId(User);
            Task taskaux = db.Tasks.Where(a => a.TaskId == TaskId).First();
            if (CheckUser(taskaux.ProjectId) || User.IsInRole("Admin"))
            {
                if (ModelState.IsValid)
                {
                    Task task = db.Tasks.Find(TaskId);
                    if (task != null)
                    {
                        task.TaskStatus = newStatus;
                        db.SaveChanges();

                        TempData["message"] = "Changed status";
                        TempData["messageType"] = "alert-success";
                    }
                    else
                    {
                        TempData["message"] = "Database error!";
                        TempData["messageType"] = "alert-danger";
                    }
                }
                else
                {
                    TempData["message"] = "Try again!";
                    TempData["messageType"] = "alert-danger";
                }

                return Redirect("/Tasks/Show/" + TaskId);
            }
            else
            {
                TempData["message"] = "Error! Nu ai acces";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Index");
            }
        }
        public IActionResult Edit(int id)
        {

            Task task = db.Tasks.Include("Comments").Include("Project")
                                        .Where(tsk => tsk.TaskId == id)
                                        .First();
            if (task == null)
            {
                TempData["message"] = "Database error!";
                ViewBag.Message = TempData["message"];
                return View("/Projects/Index");
            }
            else
            {
                if (task.Project.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    if (TempData.ContainsKey("message"))
                    {
                        ViewBag.Message = TempData["message"];
                    }
                    ViewBag.CurrentStatus = task.TaskStatus;
                    return View(task);
                }
                else
                {
                    TempData["message"] = "Error! Nu ai acces";
                    ViewBag.Message = TempData["message"];
                    return Redirect("/Projects/Index");
                }
            }
        }

        [HttpPost]
        public IActionResult Edit(int id, Task requestTask)
        {
            Task task = db.Tasks.Find(id);

            if (task == null)
            {
                TempData["message"] = "Database error!";
                ViewBag.Message = TempData["message"];
                return View(requestTask);

            }
            else
            {
                Task task_aux = db.Tasks.Include("Project").Where(c => c.TaskId == id).First();

                if (task_aux.Project.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    if (DateTime.Compare(requestTask.TaskDateStart, requestTask.TaskDateEnd) >= 0)
                    {
                        TempData["messageerr"] = "Start date must be before end date";
                        return Redirect("/Tasks/Edit/" + id);
                    }

                    if (ModelState.IsValid)
                    {
                        task.TaskTitle = requestTask.TaskTitle;
                        task.TaskDescription = requestTask.TaskDescription;
                        task.TaskDateStart = requestTask.TaskDateStart;
                        task.TaskDateEnd = requestTask.TaskDateEnd;

                        TempData["message"] = "Task was updated!";
                        db.SaveChanges();
                        return Redirect("/Projects/Show/" + task.ProjectId);
                    }
                    else
                    {
                        return View(requestTask);
                    }
                }
                else
                {
                    TempData["message"] = "Error! Nu ai acces";
                    ViewBag.Message = TempData["message"];
                    return Redirect("/Projects/Index");
                }
            }

        }

        [NonAction]
        public bool CheckUser(int? proj_id)
        {
            if (proj_id == null) return false;
            
            var userid = _userManager.GetUserId(User);
            var users = db.UserProjects.Where(userpr => userpr.ProjectId == proj_id).Select(c => c.UserId);
            
            List<string?> user_ids = users.ToList();
            
            if (user_ids.Contains(userid)) return true;
            
            return false;
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllStatuses()
        {
            var statuses = new List<SelectListItem>();
            string[] all_statuses = { "not started","in progress","completed" };
            foreach (var status in all_statuses)
            {
                string aux;
                if (status.Length == 1)
                    aux = status.ToUpper();
                else
                    aux = char.ToUpper(status[0]) + status[1..];

                statuses.Add(new SelectListItem{Value = aux,Text = aux,});

            }
            return statuses;
        }
    }
}