using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Donut.Models;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Repisitories;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Service
{
    public class UserService : IUserService
    {
        private ILogger _logger;
        private ApiService _apiService;
        private OrganizationService _orgService;
        //private ModelService _modelService;
        private ManagementDbContext _context;
        private IUsersRepository _userRepository;
        private IDbContextScopeFactory _contextScope;
        private IPasswordHasher<User> _hasher;

        public UserService(
            ApiService apiService,
            ILoggerFactory lfactory,
            OrganizationService orgService,
            //ModelService modelService,
            //ManagementDbContext context,
            IUsersRepository usersRepository,
            IDbContextScopeFactory contextScope,
            IFactory<ManagementDbContext> contextFactory,
            IPasswordHasher<User> hasher)
        {
            if (lfactory != null) _logger = lfactory.CreateLogger("Netlyt.Service.UserService");
            _contextScope = contextScope;
            _apiService = apiService;
            _orgService = orgService;
            _hasher = hasher;
            //_modelService = modelService;
            _context = contextFactory.Create();
            _userRepository = usersRepository;
        }
        
        
        public async Task CreateUser(User model, string password, ApiAuth appAuth)
        {
            var apiKey = _apiService.Generate();
            model.ApiKeys.Add(new ApiUser(model, apiKey));
            model.ApiKeys.Add(new ApiUser(model, appAuth));
            _context.Users.Add(model);
            _context.SaveChanges();
        }

        public User GetByApiKey(ApiAuth appAuth)
        {
            return _context.Users
                .Include(x => x.ApiKeys)
                .FirstOrDefault(u => u.ApiKeys.Any(x => x.ApiId == appAuth.Id));
        }

        public User GetUsername(string modelEmail)
        {
            return _context.Users.FirstOrDefault(x => x.Email == modelEmail);
        }

        public User GetUserByEmail(string modelEmail)
        {
            return _context.Users.FirstOrDefault(x => x.Email == modelEmail);
        }

        public User GetUserByLogin(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                return null;
            }

            var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verificationResult == PasswordVerificationResult.Success)
            {
                return user;
            }
            else
            {
                return null;
            }
        }
    }
}