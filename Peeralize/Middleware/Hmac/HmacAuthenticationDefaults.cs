namespace Peeralize.Middleware.Hmac
{
    public static class HmacAuthenticationDefaults
    {        
        /// <summary>
        /// The default value used for HmacAuthenticationOptions.AuthenticationScheme
        /// </summary>
        public const string AuthenticationScheme = "Hmac";
        /// <summary>
        /// 
        /// </summary>
        public const int MaxRequestAgeInSeconds = 300;
    }
}