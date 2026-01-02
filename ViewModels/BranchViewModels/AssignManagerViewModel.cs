using ApexDrive.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApexDrive.ViewModels.BranchViewModels
{
    public class AssignManagerViewModel
    {
        public Branch Branch { get; set; }

        public List<AppUser> Managers { get; set; }

        [Required(ErrorMessage = "Please select a manager")]
        public string SelectedManagerId { get; set; }
    }
}
