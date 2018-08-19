using System.Collections.Generic;

namespace Netlyt.Web.ViewModels
{
    public class UserPreviewViewModel
    {
        public string Id { get; set; }

        public string Username { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}