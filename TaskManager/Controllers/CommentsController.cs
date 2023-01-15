using Microsoft.AspNetCore.Mvc;
using Comment = TaskManager.Models.Comment;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    public class CommentsController : Controller
    {

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CommentsController(
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
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Delete(int id)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))  //NullReferenceException: Object reference not set to an instance of an object. ????????
            {
                db.Comments.Remove(comm);
                db.SaveChanges();
                TempData["message"] = "Comentariul e sters";

                return Redirect("/Tasks/Show/" + comm.TaskId);
            }
            else
            {
                TempData["message"] = "Error! Acces denied.";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Index");
            }

        }
        
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id)
        {

            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(comm);
            }
            else
            {
                TempData["message"] = "Error! Acces denied.";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Index");

            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))  
            {

                if (ModelState.IsValid)
                {
                    comm.Content = requestComment.Content;
                    comm.Date = DateTime.Now;

                    TempData["message"] = "Comentariul e modificat";
                    db.SaveChanges();
                    return Redirect("/Tasks/Show/" + comm.TaskId);
                }
                else
                {
                    return View(requestComment);
                }
            }
            else
            {
                TempData["message"] = "Error! Acces denied";
                ViewBag.Message = TempData["message"];
                return Redirect("/Projects/Index");
            }

        }


    }   
}