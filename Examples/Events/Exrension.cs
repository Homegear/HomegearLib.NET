using System;
using System.Security;
namespace Events
{
    public static class Exrension
    {
        public static SecureString ToSecureString(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            var result = new SecureString();
            foreach (var c in source.ToCharArray())
                result.AppendChar(c);
            return result;
        }
    }
}
