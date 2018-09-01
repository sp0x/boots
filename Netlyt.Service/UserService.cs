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
using Netlyt.Service.Auth;
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
        private IRateService _rateService;

        public UserService(
            ApiService apiService,
            ILoggerFactory lfactory,
            OrganizationService orgService,
            //ModelService modelService,
            //ManagementDbContext context,
            IUsersRepository usersRepository,
            IDbContextScopeFactory contextScope,
            IFactory<ManagementDbContext> contextFactory,
            IRateService rateService)
        {
            if (lfactory != null) _logger = lfactory.CreateLogger("Netlyt.Service.UserService");
            _contextScope = contextScope;
            _apiService = apiService;
            _orgService = orgService;
            //_hasher = hasher;
            _hasher = Hashing.GetDefaultHasher();
            //_modelService = modelService;
            _context = contextFactory.Create();
            _userRepository = usersRepository;
            _rateService = rateService;
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

        public User GetUserByUsername(string username)
        {
            var user = _context.Users.FirstOrDefault(x => x.UserName == username);
            return user;
        }

        public string VerifyUser(string userId)
        {
            using (var ctxSrc = _contextScope.Create())
            {
                var user = _userRepository.GetById(userId).FirstOrDefault();
                return user?.Id;
            }

        }

        public User CreateUser(User user, ApiRateLimit quota)
        {
            using (var contextSrc = _contextScope.Create())
            {
                CreateIfMissing(user);
                _rateService.SetAvailabilityForUser(user?.UserName, quota);
                return user;
            }
        }

        public void CreateIfMissing(User user)
        {
            using (var contextSrc = _contextScope.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                if (!context.Users.Any(x => x.UserName == user.UserName))
                {
                    if (user.RateLimit != null) context.Rates.Add(user.RateLimit);
                    else throw new Exception("Rate limit is required for users.");
                    if (user.Organization != null)
                    {
                        if (!OrganizationService.IsNetlyt(user.Organization))
                        {
                            context.ApiKeys.Add(user.Organization.ApiKey);
                            context.Organizations.Add(user.Organization);
                        }
                        else
                        {
                            user.Organization = context.Organizations.FirstOrDefault(x => x.Id == user.Organization.Id);
                            user.OrganizationId = user.Organization.Id;
                        }
                    }
                    else throw new Exception("Organization is required for users.");

                    context.Users.Add(user);
                    context.SaveChanges();
                }
            }
        }
        public ICollection<object> GetApiKeysAnonimized(User user)
        {
            using (var contextSrc = _contextScope.Create())
            {
                return _userRepository.GetById(user.Id)?.FirstOrDefault()?.ApiKeys
                    .Select(k => { return AnonimyzeKey(k); })
                    .ToList();
            }
        }

        public ICollection<ApiAuth> GetApiKeys(User user)
        {
            using (var contextSrc = _contextScope.Create())
            {
                return _userRepository.GetById(user.Id)?.FirstOrDefault()?.ApiKeys
                    .Select(k => k.Api)
                    .ToList();
            }
        }


        private object AnonimyzeKey(ApiUser key)
        {
            return new
            {
                Api = new
                {
                    key.Api.AppId,
                    key.Api.AppSecret
                },
                ApiId = key.ApiId,
                key.UserId
            };
        }
    }
}