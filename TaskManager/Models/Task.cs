using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TaskManager.Models
{
    public class Task
    {
        [Key]
        public int TaskId { get; set; }


        [Required(ErrorMessage = "Titlul taskului e obligatoriu")]
        [StringLength(100, ErrorMessage = "Titlul nu poate avea mai mult de 20 de caractere")]
        public string TaskTitle { get; set; }

        [Required(ErrorMessage = "Continutul articolului este obligatoriu")]
        [DataType(DataType.MultilineText)]
        public string TaskDescription { get; set; }
        public string TaskStatus { get; set; }
        public DateTime TaskDateStart { get; set; }
        public DateTime TaskDateEnd { get; set; }

        [Required(ErrorMessage = "Proiectul este obligatoriu")]
        public int ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<Comment>? Comments { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? Statuses { get; set; }

    }
}