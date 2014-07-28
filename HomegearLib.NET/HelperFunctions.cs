using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public static class HomegearHelpers
    {
        public static DateTime UnixTimeStampToDateTime(Int32 unixTimeStamp)
        {
            System.DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return epoch.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        public static Int32 DateTimeToUnixTimeStamp(DateTime time)
        {
            System.DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return (Int32)time.ToUniversalTime().Subtract(epoch).TotalSeconds;
        }
    }
}
