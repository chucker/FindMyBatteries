namespace FindMyBatteries.Common.ICloud
{
    public class AuthOptionsResult
    {
        public TrustedPhoneNumber[]? TrustedPhoneNumbers { get; set; }
    }

    public partial class TrustedPhoneNumber
    {
        public string? NumberWithDialCode { get; set; }
        public string? PushMode { get; set; }
        public string? ObfuscatedNumber { get; set; }
        public long Id { get; set; }
    }
}
