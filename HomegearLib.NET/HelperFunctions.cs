using System;

namespace HomegearLib
{
    public static class HomegearHelpers
    {
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            System.DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return epoch.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        public static long DateTimeToUnixTimeStamp(DateTime time)
        {
            System.DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return (long)time.ToUniversalTime().Subtract(epoch).TotalSeconds;
        }
    }
}
