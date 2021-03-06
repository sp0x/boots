﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Donut.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Models.Account;

namespace Netlyt.Service
{
    public interface IUserService
    {
        Task CreateUser(User model, string password, ApiAuth appAuth);
        User GetByApiKey(ApiAuth appAuth);
        User GetUserByEmail(string modelEmail);
        User GetUsername(string modelEmail);
        User GetUserByLogin(string email, string password);
        User GetUserByUsername(string username);
        string VerifyUser(string toString);
        User CreateUser(User user, ApiRateLimit quota);
        void CreateIfMissing(User user);
        ICollection<object> GetApiKeysAnonimized(User user);
        ICollection<ApiAuth> GetApiKeys(User user);
        User GetByCloudNodeToken(string authToken);
        Token IssueNewToken();
    }
}