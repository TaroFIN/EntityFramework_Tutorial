namespace Pelican.Utility
{
    public class ECPay
    {
        public static string HashKey { get; set; }
        public static string HashIV { get; set; }
    }

    public class FacebookLoginKey
    {
        public static string AppId { get; set; }
        public static string AppSecret { get; set; }
    }

    public class MicrosoftLoginKey
    {
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
    }
}
