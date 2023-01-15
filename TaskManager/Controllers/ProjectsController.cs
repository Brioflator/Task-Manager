using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using TaskManager.Data;
using TaskManager.Models;
using TaskManager.Data;
using TaskManager.Models;
using Task = TaskManager.Models.Task;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManager.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public ProjectsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllTeams()
        {
            var selectList = new List<SelectListItem>();
            var currentUserId = _userManager.GetUserId(User);
            if (User.IsInRole("Admin"))
            {
                var teams = from t in db.Teams
                            select t;
                foreach (var team in teams)
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = team.TeamId.ToString(),
                        Text = team.TeamName.ToString()
                    });
                }
            }
            else
            {
                var teams = from t in db.Teams
                            where t.OrganizerId == currentUserId
                            select t;
                foreach (var team in teams)
                {
                    selectList.Add(new SelectListItem
                    {
                        Value = team.TeamId.ToString(),
                        Text = team.TeamName.ToString()
                    });
                }
            }

            return selectList;
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult Index()
        {
            var userid = _userManager.GetUserId(User);
            var projs = db.UserProjects.Where(userpr => userpr.UserId == userid).Select(c => c.ProjectId);
            List<int?> proj_ids = projs.ToList();

            var projects = db.Projects.Include("User").Where(proj => proj_ids.Contains(proj.ProjectId)).OrderBy(c => c.ProjectName);

            if (!projects.Any()) ViewBag.Projects = 0;
            else ViewBag.Projects = projects;


            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult IndexAdmin()
        {
            var projects = db.Projects.Include("User");
            ViewBag.Projects = projects;

            if (TempData.ContainsKey("message")) ViewBag.Message = TempData["message"];

            return View();
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            Project project = new Project();
            return View(project);
        }



        [HttpPost]
        public async Task<IActionResult> NewAsync(Project project)
        {
            var user_id = _userManager.GetUserId(User);
            if (ModelState.IsValid)
            {
                project.UserId = user_id;
                UserProject project_data = new UserProject();
                project_data.UserId = user_id;
                project_data.ProjectId = project.ProjectId;
                project_data.Project = project;

                db.UserProjects.Add(project_data);
                await db.SaveChangesAsync();
                TempData["message"] = "The project wad added";
                return RedirectToAction("Index");
            }
            else return View(project);
        }


        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Show([FromForm] Task task)
        {
            if (DateTime.Compare(task.TaskDateStart, task.TaskDateEnd) >= 0)        //verificare daca datile sunt bune
            {
                TempData["message"] = "Data de inceput este dupa data de final!";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Show/" + task.ProjectId);
            }
            if (ModelState.IsValid)
            {
                db.Tasks.Add(task);
                db.SaveChanges();
                return Redirect("/Projects/Show/" + task.ProjectId);
            }
            else
            {
                Project project = db.Projects.Include("Tasks").Include("User").Where(proj => proj.ProjectId == task.ProjectId).First();
                return View(project);
            }
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id)
        {
            var userid = _userManager.GetUserId(User);
            var users = db.UserProjects.Where(userproj => userproj.ProjectId == id).Select(c => c.UserId);

            List<string?> user_ids = users.ToList();

            var project = db.Projects.Include("Tasks.User").Include("User").Where(proj => proj.ProjectId == id).First();

            if (user_ids.Contains(userid) || User.IsInRole("Admin"))
            {
                return View(project);
            }
            else
            {
                TempData["message"] = "Error! Nu ai acces";
                ViewBag.Message = TempData["message"];
                return RedirectToAction("Index");
            }
        }


        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id, Project requestProject)
        {
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                TempData["message"] = "Database error!";
                return View(requestProject);

            }
            else
            {
                var userid = _userManager.GetUserId(User);
                if (project.UserId == userid || User.IsInRole("Admin"))
                {
                    if (ModelState.IsValid)
                    {
                        project.ProjectName = requestProject.ProjectName;
                        project.ProjectDescription = requestProject.ProjectDescription;
                        TempData["message"] = "Proiectul a fost modificat";
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return View(requestProject);
                    }
                }
                else
                {
                    TempData["message"] = "Error! Nu ai acces";
                    ViewBag.Message = TempData["message"];
                    return RedirectToAction("Index");
                }
            }
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            var userid = _userManager.GetUserId(User);
            Project project = db.Projects.Include("Tasks")
                                        .Where(proj => proj.ProjectId == id)
                                        .First();
            if (project.UserId == userid || User.IsInRole("Admin"))
            {
                return View(project);
            }
            else
            {
                TempData["message"] = "Error! Nu ai acces";
                ViewBag.Message = TempData["message"];
                return Redirect("/Show/" + id);
            }

        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult Delete(int id)
        {
            Project project = db.Projects.Include("Tasks.Comments").Where(proj => proj.ProjectId == id).First();
            if (project == null)
            {
                TempData["message"] = "Database error!";
            }
            else
            {
                var userid = _userManager.GetUserId(User);
                if (project.UserId == userid || User.IsInRole("Admin"))
                {
                    db.Projects.Remove(project);
                    db.SaveChanges();
                    TempData["message"] = "Project has been deleted.";
                }
                else
                {
                    TempData["message"] = "Error! Nu ai acces";
                    ViewBag.Message = TempData["message"];
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult Users(int id)
        {
            var userid = _userManager.GetUserId(User);
            var users = db.UserProjects.Include("User").Where(user => user.ProjectId == id);
            List<string?> users_ids = users.Select(c => c.UserId).ToList();
            if (users_ids.Contains(userid) || User.IsInRole("Admin"))
            {

                var users_search = db.Users.Where(a => 1 == 0);
                var search = "";
                // MOTORUL DE CAUTARE
                //REZULTATE IN ORDINE LEXICOGRAFICA
                if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
                {
                    search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();
                    
                    users_search = db.Users.Where(usn => usn.UserName.Contains(search) && !users_ids.Contains(usn.Id) && !usn.UserName.EndsWith("@test.com")) .OrderBy(a => a.UserName);
                }

                var project = db.Projects.Find(id);
                ViewBag.Users = users;
                ViewBag.Project = project;
                ViewBag.AllUsers = users_search;
                return View();
            }
            else
            {
                TempData["message"] = "Error! Acces denied.";
                ViewBag.Message = TempData["message"];
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Users(int id, [FromForm] UserProject requestUser)
        {
            var userid = _userManager.GetUserId(User);
            var project = db.Projects.Find(id);
            if (project == null)
            {
                TempData["message"] = "Database error!";
                return RedirectToAction("Index");
            }
            else
            {
                if (userid == project.UserId || User.IsInRole("Admin"))
                {
                    UserProject userProject = new UserProject();
                    userProject.ProjectId = id;
                    userProject.UserId = requestUser.UserId;
                    db.UserProjects.Add(userProject);
                    db.SaveChanges();
                    return Redirect("/Projects/Users/" + project.ProjectId);
                }
                else
                {
                    TempData["message"] = "Error! Nu ai acces";
                    ViewBag.Message = TempData["message"];
                    return RedirectToAction("Index");
                }
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult UserDelete(int id, string rmvuser, int rmvproject)
        {
            var userid = _userManager.GetUserId(User);
            var users = db.UserProjects.Include("User").Where(user => user.ProjectId == rmvproject);
            List<string?> users_ids = users.Select(c => c.UserId).ToList();
            if ((users_ids.Contains(userid) && (rmvuser == userid || users_ids.Contains(userid)))
                    || User.IsInRole("Admin"))
            {
                UserProject user_project = db.UserProjects.Find(id, rmvuser, rmvproject);
                var project_id = user_project.ProjectId;
                if (user_project == null)
                {
                    TempData["message"] = "Database error!";
                    return Redirect("/Projects/Users/" + project_id);
                }
                else
                {
                    db.UserProjects.Remove(user_project);
                    db.SaveChanges();
                    if (user_project.UserId == _userManager.GetUserId(User))
                    {
                        TempData["message"] = "You have left the project.";
                        return RedirectToAction("Index");

                    }
                    else
                    {
                        TempData["message"] = "User has been removed.";
                        return Redirect("/Projects/Users/" + project_id);
                    }
                }
            }
            else
            {
                TempData["message"] = "Error! Nu ai acces";
                ViewBag.Message = TempData["message"];
                return RedirectToAction("Index");
            }
        }

    }   
}