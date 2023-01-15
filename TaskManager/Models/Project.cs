using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TaskManager.Models
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }


        [Required(ErrorMessage = "Project name is required")]
        [StringLength(20, ErrorMessage = "Name can't be longer than 20 characters")]
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }

        //[Required(ErrorMessage = "Team is required")]
        //public int TeamId { get; set; }
        //public virtual Team Team { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<Task>? Tasks { get; set; }
        public virtual ICollection<UserProject>? UserProjects { get; set; }
        //[NotMapped]
        //public IEnumerable<SelectListItem>? Teams { get; set; }
    }
}