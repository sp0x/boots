using System; 
using System.IO; 
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Netlyt.Interfaces.Models;
using Netlyt.Service;

namespace Netlyt.Web.Middleware.Hmac
{
    public class HmacHandler : AuthenticationHandler<HmacOptions>
    {
        private readonly IMemoryCache _memoryCache; 
        //private string _iv;
        //private int _iterations;
        private byte[] _salt;
        private ApiService _apiService;
        private IUserManagementService _userService;
        private IHttpContextAccessor _contextAccessor;

        public HmacHandler(
            IOptionsMonitor<HmacOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IMemoryCache memoryCache,
            ApiService apiService,
            IUserManagementService userService,
            IHttpContextAccessor contextAccessor) : base(options, logger, encoder, clock)
        {
            // store custom services here...
            _memoryCache = memoryCache;
            _apiService = apiService;
            _userService = userService;
//            _iv = "9595948593968468"; //new byte[]{ 4,5,2,8,7,1,8,2,
//            //              9,7,3,4,8,2,9,3 };
//            _iterations = 200;
            _salt = new byte[] { }; //243, 133, 64, 76, 111, 136, 1, 78};
            _contextAccessor = contextAccessor;
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            return base.HandleChallengeAsync(properties);
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var apiId = _contextAccessor.HttpContext.Session.GetString("APP_API_ID");
            if (string.IsNullOrEmpty(apiId))
            {
                var authorization = Request.Headers["Authorization"];
                if (string.IsNullOrEmpty(authorization))
                {
                    return AuthenticateResult.Fail("No authorization header.");
                } 
                ApiAuth apiAuth;
                var valid = Validate(Request, out apiAuth);

                if (valid)
                {
                    _apiService.SetCurrentApi(apiAuth);
                    var ticket = await _userService.InitializeHmacSession();
                    return AuthenticateResult.Success(ticket);
                }
            }
            else
            {
                await RefreshAuth();
                var ticket = _userService.ValidateHmacSession();
                return AuthenticateResult.Success(ticket);
            }
            return AuthenticateResult.Fail("Authentication failed");
        }

        private async Task RefreshAuth()
        { 
            StringValues clientVersion;
            if (_contextAccessor.HttpContext.Request.Headers.TryGetValue("ClientVersion", out clientVersion))
            {
                //Do something with the version of netlyt client.. 
            }
            else
            {
                //This is a legacy version, decrypt the content. 
                var crApi = await _userService.GetCurrentApi();
                DecryptLegacyBody(crApi);
            }

        }

        //        protected Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        //        {
        //            return base.HandleUnauthorizedAsync(context);
        //        }

        private bool Validate(HttpRequest request, out ApiAuth apiAuth)
        {
            var header = request.Headers["Authorization"];
            var authenticationHeader = AuthenticationHeaderValue.Parse(header);
            if (Options.AuthenticationScheme.Equals(authenticationHeader.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                var rawAuthenticationHeader = authenticationHeader.Parameter;
                var authenticationHeaderArray = GetAuthenticationValues(rawAuthenticationHeader);

                if (authenticationHeaderArray != null)
                {
                    var appId = authenticationHeaderArray[0];
                    var requestTimeStamp = authenticationHeaderArray[1];
                    var nonce = authenticationHeaderArray[2];
                    var incomingBase64Signature = authenticationHeaderArray[3];
                    var body = "";
                    var isValidRequest = IsValidRequest(request, appId, incomingBase64Signature, nonce, requestTimeStamp,
                        out apiAuth, out body);
                    //If the request is valid, xor the body and write it
                    if (isValidRequest)
                    {
                        DecryptBody(apiAuth, body);
                    }
                    return isValidRequest;
                }
                else
                {
                    apiAuth = null;
                }
            }
            else
            {
                apiAuth = null;
            }

            return false;
        }

        /// <summary>
        /// Decrypts the body of the request
        /// </summary>
        /// <param name="apiAuth"></param>
        /// <param name="body"></param>
        private void DecryptBody(ApiAuth apiAuth, string body)
        {
            if (!string.IsNullOrEmpty(body))
            {
                body = XorString(body, apiAuth.AppSecret);
                byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
                Request.Body = new MemoryStream(bodyBytes);
            }
        }
        /// <summary>
        /// Decrypts the body of the request
        /// </summary>
        /// <param name="apiAuth"></param> 
        private void DecryptLegacyBody(ApiAuth apiAuth)
        {
            var contentBytes = ReadFully(Request.Body);
            var contentString = System.Text.Encoding.UTF8.GetString(contentBytes);
            contentBytes = null;
            if (!string.IsNullOrEmpty(contentString))
            {
                contentString = XorString(contentString, apiAuth.AppSecret);
                byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(contentString);
                Request.Body = new MemoryStream(bodyBytes);
            }
        }

        private bool IsValidRequest(HttpRequest req, string appId, string incomingBase64Signature, string nonce, string requestTimeStamp,
            out ApiAuth matchingApiAuth, out string body)
        {
            string requestContentBase64String = "";
            var absoluteUri = string.Concat(
                req.Scheme,
                "://",
                req.Host.ToUriComponent(),
                req.PathBase.ToUriComponent(),
                req.Path.ToUriComponent(),
                req.QueryString.ToUriComponent());
            string requestUri = WebUtility.UrlEncode(absoluteUri);
            string requestHttpMethod = req.Method;
            body = null;

            //App filter
            matchingApiAuth = _apiService.GetApi(appId);
            if (matchingApiAuth == null)//Options.AppId != AppId)
            {
                return false;
            }
            if (IsReplayRequest(nonce, requestTimeStamp))
            {
                return false;
            }

            byte[] hash = ComputeRequestBodyHash(req.Body, out body);

            if (hash != null)
            {
                requestContentBase64String = Convert.ToBase64String(hash);
            }


            var secretKeyBytes = Convert.FromBase64String(matchingApiAuth.AppSecret);

            string validRawSigniture = String.Format("{0}{1}{2}{3}{4}{5}", appId, requestHttpMethod, requestUri, requestTimeStamp, nonce, requestContentBase64String);
            byte[] validSigniture = Encoding.UTF8.GetBytes(validRawSigniture);

            using (HMACSHA256 hmac = new HMACSHA256(secretKeyBytes))
            {
                byte[] validHmacSigniture = hmac.ComputeHash(validSigniture);
                var validHmacSignitureString = Convert.ToBase64String(validHmacSigniture);
                //Check if the signiture that we received was the same as the one we generated
                var isValidRequest = (incomingBase64Signature.Equals(validHmacSignitureString, StringComparison.Ordinal));

                return isValidRequest;
            }

        }
        private string XorString(string text, string key) { 
            var result = new StringBuilder();
            for (int c = 0; c < text.Length; c++)
                result.Append((char)((uint)text[c] ^ (uint)key[c % key.Length]));

            return result.ToString();
        }
  
        private string[] GetAuthenticationValues(string rawAuthenticationHeader)
        {
            rawAuthenticationHeader = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(rawAuthenticationHeader));
            var credArray = rawAuthenticationHeader.Split(':');

            if (credArray.Length == 4)
            {
                return credArray;
            }
            else
            {
                return null;
            }
        }

        private bool IsReplayRequest(string nonce, string requestTimeStamp)
        {
            var nonceInMemory = _memoryCache.Get(nonce);
            if (nonceInMemory != null)
            {
                return true;
            }

            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan currentTs = DateTime.UtcNow - epochStart;

            var serverTotalSeconds = Convert.ToUInt64(currentTs.TotalSeconds);
            var requestTotalSeconds = Convert.ToUInt64(requestTimeStamp);
            var diff = (serverTotalSeconds - requestTotalSeconds);

            if (diff > Options.MaxRequestAgeInSeconds)
            {
                return true;
            }
            _memoryCache.Set(nonce, requestTimeStamp, DateTimeOffset.UtcNow.AddSeconds(Options.MaxRequestAgeInSeconds));
            return false;
        }

        private byte[] ComputeRequestBodyHash(Stream body, out string contentString)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = null;
                var content = ReadFully(body);
                contentString = System.Text.Encoding.UTF8.GetString(content);
                //Debug.WriteLine(contentString);
                if (content.Length != 0)
                {
                    hash = md5.ComputeHash(content);
                }
                return hash;
            }
        }

        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                if (input.CanSeek)
                {
                    input.Position = 0;
                }
                return ms.ToArray();
            }
//            if (input.CanSeek)
//            {
//                input.Position = 0;
//            }
        }

        //        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        //        {
        //            var req = context.Request;
        //
        //            if (req.Headers.Authorization != null && authenticationScheme.Equals(req.Headers.Authorization.Scheme, StringComparison.OrdinalIgnoreCase))
        //            {
        //                var rawAuthzHeader = req.Headers.Authorization.Parameter;
        //
        //                var autherizationHeaderArray = GetAutherizationHeaderValues(rawAuthzHeader);
        //
        //                if (autherizationHeaderArray != null)
        //                {
        //                    var APPId = autherizationHeaderArray[0];
        //                    var incomingBase64Signature = autherizationHeaderArray[1];
        //                    var nonce = autherizationHeaderArray[2];
        //                    var requestTimeStamp = autherizationHeaderArray[3];
        //
        //                    var isValid = isValidRequest(req, APPId, incomingBase64Signature, nonce, requestTimeStamp);
        //
        //                    if (isValid.Result)
        //                    {
        //                        var currentPrincipal = new GenericPrincipal(new GenericIdentity(APPId), null);
        //                        context.Principal = currentPrincipal;
        //                    }
        //                    else
        //                    {
        //                        context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
        //                    }
        //                }
        //                else
        //                {
        //                    context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
        //                }
        //            }
        //            else
        //            {
        //                context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
        //            }
        //
        //            return Task.FromResult(0);
        //        }
        //
        //        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        //        {
        //            context.Result = new ResultWithChallenge(context.Result);
        //            return Task.FromResult(0);
        //        }
        //
        //        public bool AllowMultiple
        //        {
        //            get { return false; }
        //        }
        //
        //        private string[] GetAutherizationHeaderValues(string rawAuthzHeader)
        //        {
        //
        //            var credArray = rawAuthzHeader.Split(':');
        //
        //            if (credArray.Length == 4)
        //            {
        //                return credArray;
        //            }
        //            else
        //            {
        //                return null;
        //            }
        //
        //        }
    }
}
