using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Netlyt.Data.ViewModels;

namespace Netlyt.Web.Models.ManageViewModels
{
    public class IndexViewModel
    {
        public string Id { get; set; }
        public string Username { get; set; }

        public bool IsEmailConfirmed { get; set; }
        public OrganizationViewModel Organization { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        public string StatusMessage { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}