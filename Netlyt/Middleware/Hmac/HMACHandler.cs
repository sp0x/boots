using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using nvoid.Integration;
using nvoid.Security.Ciphers;
using Netlyt.Controllers;
using Netlyt.Service.Auth;

namespace Netlyt.Middleware.Hmac
{
    public class HmacHandler : AuthenticationHandler<HmacOptions>
    {
        private readonly IMemoryCache _memoryCache;
        private readonly RemoteDataSource<ApiAuth> _authSource;
        private string _iv;
        private int _iterations;
        private byte[] _salt;
        public HmacHandler(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _authSource = typeof(ApiAuth).GetDataSource<ApiAuth>();
            _iv = "9595948593968468"; //new byte[]{ 4,5,2,8,7,1,8,2,
                  //              9,7,3,4,8,2,9,3 };
            _iterations = 200;
            _salt = new byte[] { }; //243, 133, 64, 76, 111, 136, 1, 78};
        }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorization = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authorization))
            {
                return AuthenticateResult.Skip();
            }
            ApiAuth apiAuth;
            var valid = Validate(Request, out apiAuth);

            if (valid)
            {
                var claimsIdentity = new ClaimsIdentity("HMAC");
                var principal = new ClaimsPrincipal(claimsIdentity);
                var user = Context.User;
                var appApiId = Context.Session.GetString("APP_API_ID");
                if (appApiId == null)
                {
                    Context.Session.SetString("APP_API_ID", apiAuth.Id.Value); 
                    user.AddIdentity(claimsIdentity); 
                }
                Response.Headers.Add("APP_API_ID", apiAuth.Id.Value);
                var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), Options.AuthenticationScheme);
                return AuthenticateResult.Success(ticket);
            }

            return AuthenticateResult.Fail("Authentication failed");

        }

        protected override Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            return base.HandleUnauthorizedAsync(context);
        }

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
                        if (!string.IsNullOrEmpty(body))
                        {
                            var encoding = System.Text.Encoding.ASCII;
                            var iv = this._iv;
                            body = Decrypt(body, apiAuth.AppSecret);
                            byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
                            Request.Body = new MemoryStream(bodyBytes); 
                        }
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
            matchingApiAuth = _authSource.FindFirst(x=> x.AppId == appId);
            if (matchingApiAuth==null)//Options.AppId != AppId)
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
        //private string XorString(string text, string key) { 
        //    var result = new StringBuilder();
        //    for (int c = 0; c < text.Length; c++)
        //        result.Append((char)((uint)text[c] ^ (uint)key[c % key.Length]));

        //    return result.ToString();
        //}

        private string DecodeMessage(string text, string key)
        {
            var bytes = Convert.FromBase64String(text);
            var encoding = System.Text.Encoding.ASCII;
            var iv = this._iv;
            return Decrypt(text, key);
        }

        public byte[] Decode(string str)
        {
            var decbuff = Convert.FromBase64String(str);
            return decbuff;
        }

        //        private String DecryptRJ256(byte[] cypher, string KeyString, byte[] iv)
        //        {
        //            var sRet = "";
        //
        //            var encoding = new UTF8Encoding();
        //            var keyBytes = encoding.GetBytes(KeyString); 
        ////            var crypto = new RijndaelEnhanced(KeyString, IVString, 0, 0, 256, "SHA256", "", 200);
        ////            var ret = crypto.DecryptToBytes(cypher);
        //            using (var rj = new RijndaelManaged())
        //            {
        //                try
        //                { 
        //                    var derivedKey = new Rfc2898DeriveBytes(keyBytes, _salt , _iterations);
        //                    //AES.Key = key.GetBytes(AES.KeySize / 8);
        //                    rj.Mode = CipherMode.CBC;
        //                    rj.BlockSize = 128;
        //                    rj.Padding = PaddingMode.Zeros;
        //                    rj.Key = derivedKey.GetBytes(rj.KeySize / 8);
        //                    rj.IV = iv;
        //                    var ms = new MemoryStream(cypher);
        //
        //                    using (var cs = new CryptoStream(ms, rj.CreateDecryptor(), CryptoStreamMode.Read))
        //                    {
        //                        using (var sr = new StreamReader(cs))
        //                        {
        //                            sRet = sr.ReadLine();
        //                        }
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    ex = ex;
        //                }
        //                finally
        //                {
        //                    rj.Clear();
        //                }
        //            }
        //
        //            return sRet;
        //        }


        public String Decrypt(String text, String key)
        {
            try
            { 
                MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] keyBytes = System.Text.Encoding.ASCII.GetBytes(key);
                byte[] keyBytesHash = md5.ComputeHash(keyBytes);
                var keySize = 128;
                var passwordSize = keySize / 8;
                //pad key out to 32 bytes (256bits) if its too short
                if (keyBytesHash.Length < passwordSize)
                {
                    var paddedkey = new byte[passwordSize];
                    Buffer.BlockCopy(keyBytesHash, 0, paddedkey, 0, keyBytesHash.Length);
                    keyBytesHash = paddedkey;
                }

                var ivBytes = Encoding.ASCII.GetBytes(this._iv);
                var rawData = System.Convert.FromBase64String(text);
                using (AesManaged aes = new AesManaged())
                {

                    var ivhex = BitConverter.ToString(keyBytesHash).Replace("-", string.Empty);
                    var keyhex = BitConverter.ToString(ivBytes).Replace("-", string.Empty);

                    // Encrypt File
                    using (var ms = new MemoryStream())
                    {
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.None;
                        aes.KeySize = 128;
                        aes.BlockSize = 128;

                        var decryptor = aes.CreateEncryptor(keyBytesHash, ivBytes);
                        var result = PerformCryptography(decryptor, rawData);
                        var ascii = Encoding.ASCII.GetString(result);
                        var utf = Encoding.UTF8.GetString(result);
                        return ascii;
                    }
                }

            }
            catch (Exception ex)
            {
                ex = ex;
            }
            return "";
        }

        private byte[] PerformCryptography(ICryptoTransform cryptoTransform, byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
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
            if (input.CanSeek)
            {
                input.Position = 0;
            }
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