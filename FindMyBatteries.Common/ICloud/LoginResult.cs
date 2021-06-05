using System.Collections.Generic;

namespace FindMyBatteries.Common.ICloud
{
    public class LoginResult
    {
        public DsInfo? DsInfo { get; set; }
        public Dictionary<string, WebService>? WebServices { get; set; }
    }
}
