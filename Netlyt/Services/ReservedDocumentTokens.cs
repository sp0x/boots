namespace Netlyt.Services
{
    public static class ReservedDocumentTokens
    {
        public const string FbUserToken = "__fb_token";

        public static string GetUserSocialNetworkTokenName(string socnet)
        {
            return $"__socn_{socnet}";
        }
    }
}