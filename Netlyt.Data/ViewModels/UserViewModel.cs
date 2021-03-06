using System.Collections.Generic;

namespace Netlyt.Data.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<ApiAuthViewModel> ApiKeys { get; set; }

        public UserViewModel()
        {
            ApiKeys = new List<ApiAuthViewModel>();
        }
    }
}