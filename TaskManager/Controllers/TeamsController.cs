using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Web;
using TaskManager.Data;
using TaskManager.Models;
using Microsoft.AspNetCore.Identity;

namespace TaskManager.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public TeamsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        // GET: Teams
        [Authorize(Roles = "User, Organizer, Admin")]
        public ActionResult Index()
        {
            var teams = from team in db.Teams
                        select team;
            var currentUserId = _userManager.GetUserId(User);
            ApplicationUser user = db.Users.Find(currentUserId);

            ViewBag.Teams = teams;
            ViewBag.User = user;
            if (User.IsInRole("Admin")) ViewBag.isAdmin = 1;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }

            return View();
        }

        [Authorize(Roles = "User, Organizer, Admin")]
        public ActionResult Show(int id)
        {
            Team team = db.Teams.Find(id);
            //ICollection<ApplicationUser> Users = team.TeamUsers;
            var currentUserId = _userManager.GetUserId(User);
            ApplicationUser user = db.Users.Find(currentUserId);
            if (team.TeamUsers.Contains(user) || User.IsInRole("Admin"))
            {
                if (team.OrganizerId == currentUserId) ViewBag.isOrg = 1;
                if (User.IsInRole("Admin")) ViewBag.isAdmin = 1;

                var projects = from project in db.Projects
                               where project.TeamId == id
                               select project;
                ViewBag.Projects = projects;

                ApplicationUser organizer = db.Users.Find(team.OrganizerId);
                ViewBag.Organizer = organizer;

                return View(team);
            }
            else return RedirectToAction("Index");
        }

        [Authorize(Roles = "User, Organizer, Admin")]
        public ActionResult New()
        {
            Team team = new Team();
            team.TUsers = GetAllUsers();
            return View(team);
        }
        [HttpPost]
        [Authorize(Roles = "User, Organizer, Admin")]
        public ActionResult New(Team team)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    team.TeamUsers = new Collection<ApplicationUser>();
                    foreach (var selectedUserId in team.TeamUsersId)
                    {
                        ApplicationUser dbUser = db.Users.Find(selectedUserId);
                        team.TeamUsers.Add(dbUser);
                    }

                    var currentUserId = _userManager.GetUserId(User);
                    ApplicationUser user = db.Users.Find(currentUserId);
                    if (!team.TeamUsers.Contains(user)) team.TeamUsers.Add(user);

                    team.OrganizerId = currentUserId;
                    db.Teams.Add(team);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    team.TUsers = GetAllUsers();
                    return View(team);
                }
            }
            catch (Exception e)
            {
                team.TUsers = GetAllUsers();
                return View(team);
            }
        }

        [Authorize(Roles = "Organizer, Admin")]
        public ActionResult Edit(int id)
        {
            Team team = db.Teams.Find(id);
            var currentUserId = _userManager.GetUserId(User);
            if (team.OrganizerId == currentUserId || User.IsInRole("Admin"))
            {
                team.TUsers = GetAllUsers();

                List<string> currentSelection = new List<string>();
                foreach (var userAux in team.TeamUsers)
                {
                    currentSelection.Add(userAux.Id);
                }

                team.TeamUsersId = currentSelection.ToArray();

                return View(team);
            }
            else return RedirectToAction("Index");
        }

        [HttpPut]
        [Authorize(Roles = "Organizer, Admin")]
        public ActionResult Edit(int id, Team requestTeam)
        {
            try
            {
                Team team = db.Teams.Find(id);
                if (ModelState.IsValid)
                {
                    foreach (ApplicationUser currentUser in team.TeamUsers.ToList())
                    {
                        team.TeamUsers.Remove(currentUser);
                    }

                    foreach (var selectedUserId in requestTeam.TeamUsersId)
                    {
                        ApplicationUser dbUser = db.Users.Find(selectedUserId);
                        team.TeamUsers.Add(dbUser);
                    }

                    team.TeamName = requestTeam.TeamName;
                    db.SaveChanges();

                    return RedirectToAction("Show/" + id);
                }
                else
                {
                    requestTeam.TUsers = GetAllUsers();
                    return View(requestTeam);
                }
            }
            catch (Exception e)
            {
                requestTeam.TUsers = GetAllUsers();
                return View(requestTeam);
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Organizer, Admin")]
        public ActionResult Delete(int id)
        {
            Team team = db.Teams.Find(id);
            var currentUserId = _userManager.GetUserId(User);
            if (team.OrganizerId == currentUserId || User.IsInRole("Admin"))
            {
                TempData["message"] = "Echipa " + team.TeamName + " a fost stearsa din baza de date";

                db.Teams.Remove(team);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public IEnumerable<SelectListItem> GetAllUsers()
        {
            var selectList = new List<SelectListItem>();
            var users = from u in db.Users
                        select u;

            foreach (var user in users)
            {
                selectList.Add(new SelectListItem
                {
                    Value = user.Id.ToString(),
                    Text = user.UserName.ToString()
                });
            }
            return selectList;
        }
    }
}