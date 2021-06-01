using System.Collections.Generic;

namespace FindMyBatteries.ICloud
{
    public class LoginResult
    {
        public DsInfo? DsInfo { get; set; }
        public Dictionary<string, WebService>? WebServices { get; set; }
    }
}
