using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using TaskManager.Data;
using TaskManager.Models;
using User = TaskManager.Models.ApplicationUser;
namespace TaskManager.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UsersController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            var users = from user in db.Users
                        orderby user.UserName
                        select user;
            ViewBag.UsersList = users;
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }
            return View();
        }

        

        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public IActionResult Delete(string id)
        {
            var is_Admin = User.IsInRole("Admin");
            var is_User = id == _userManager.GetUserId(User);
            if (is_Admin || is_User)
            {
                var user = db.Users.Include("Projects.Tasks.Comments").Include("UserProjects").Include("Tasks.Comments").Include("Comments").Where(u => u.Id == id).FirstOrDefault();


                // Delete user comments
                foreach (var comment in user.Comments)
                {
                    db.Comments.Remove(comment);
                }
                // Delete user articles
                foreach (var task in user.Tasks)
                {
                    db.Tasks.Remove(task);
                }

                foreach (var usproj in user.UserProjects)
                {
                    db.UserProjects.Remove(usproj);
                }

                foreach (var proj in user.Projects)
                {
                    db.Projects.Remove(proj);
                }

                db.Users.Remove(user);
                db.SaveChanges();

                if (is_User) return RedirectToAction("Identity/Account/Logout");
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Error! Acces denied";
                ViewBag.Message = TempData["message"];
                return RedirectToAction("/Home/Index");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = db.Users.Find(id);
            if (user == null)
            {
                TempData["message"] = "Database error!";
                return RedirectToAction("Index");
            }

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = roles;
            ViewBag.UserCurent = _userManager.GetUserId(User);

            return View(user);

        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Edit(string id)
        {
            User user = db.Users.Find(id);
            if (user == null)
            {
                TempData["message"] = "Database error!";
                return RedirectToAction("Index");
            }
            else
            {

                user.AllRoles = GetAllRoles();

                var roleNames = await _userManager.GetRolesAsync(user); // Lista de roluri

                var currentUserRole = _roleManager.Roles.Where(r => roleNames.Contains(r.Name)).Select(r => r.Id).First(); // Selectam un singur rol
                ViewBag.UserRole = currentUserRole;

                return View(user);
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            User user = db.Users.Find(id);
            if (user == null)
            {
                TempData["message"] = "Database error!";
                return RedirectToAction("Index");
            }
            else
            {

                user.AllRoles = GetAllRoles();


                if (ModelState.IsValid)
                {
                    user.UserName = newData.UserName;
                    user.Email = newData.Email;
                    user.FirstName = newData.FirstName;
                    user.LastName = newData.LastName;
                    user.PhoneNumber = newData.PhoneNumber;

                    var roles = db.Roles.ToList();
                    foreach (var role in roles)
                    {
                        // Scoatem rolurile anterioare
                        await _userManager.RemoveFromRoleAsync(user, role.Name);
                    }
                    // Adaugam noul rol
                    var roleName = await _roleManager.FindByIdAsync(newRole);
                    await _userManager.AddToRoleAsync(user, roleName.ToString());

                    db.SaveChanges();

                }
                return RedirectToAction("Index");
            }
        }

        


        [NonAction]
        public IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles select role;

            foreach (var role in roles)
            {
                if (role.Name.ToString().ToLower() != "editor")
                    selectList.Add(new SelectListItem{ Value = role.Id.ToString(), Text = role.Name.ToString() });
            }
            return selectList;
        }
    }
}