using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Models.Account;

namespace Netlyt.Service
{
    public interface IUserManagementService
    {
        IEnumerable<User> GetUsers();
        AuthenticationTicket ValidateHmacSession();
        void SetUserEmail(User user, string newEmail);
        Task<bool> AddRolesToUser(User user, IEnumerable<string> newRoles);
        Task<Tuple<IdentityResult, User>> CreateUser(RegisterViewModel model);
        Task<ApiAuth> GetCurrentApi();
        Task<User> GetCurrentUser();
        Task<IEnumerable<string>> GetRoles(User src);
        Task<User> GetUser(ClaimsPrincipal user);
        Task<AuthenticationTicket> InitializeHmacSession();
        void InitializeUserSession(ClaimsPrincipal httpContextUser);
        void AddUser(User user);
        Task<User> GetUser(string id);
        Organization GetOrganization(User user);
    }
}