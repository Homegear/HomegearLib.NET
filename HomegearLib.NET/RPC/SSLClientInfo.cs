using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;

namespace HomegearLib.RPC
{
    public class SSLClientInfo
    {
        private bool _verifyCertificate = true;
        public bool VerifyCertificate { get { return _verifyCertificate; } set { _verifyCertificate = value; } }

        private SecureString _username = new SecureString();
        public SecureString Username { get { return _username; } set { _username = value; } }

        private SecureString _password = new SecureString();
        public SecureString Password { get { return _password; } set { _password = value; } }

        public SSLClientInfo()
        {
        }

        public SSLClientInfo(SecureString username, SecureString password)
        {
            _username = username;
            _password = password;
        }

        public SSLClientInfo(SecureString username, SecureString password, bool verifyCertificate)
        {
            _username = username;
            _password = password;
            _verifyCertificate = verifyCertificate;
        }

        public SSLClientInfo(String callbackHostname, String username, String password)
        {
            SetUsernameFromString(username);
            SetPasswordFromString(password);
        }

        public SSLClientInfo(String callbackHostname, String username, String password, bool verifyCertificate)
        {
            SetUsernameFromString(username);
            SetPasswordFromString(password);
            _verifyCertificate = verifyCertificate;
        }

        public void SetUsernameFromString(String username)
        {
            _username = GetSecureString(username);
        }

        public void SetPasswordFromString(String password)
        {
            _password = GetSecureString(password);
        }

        unsafe SecureString GetSecureString(String value)
        {
            char[] chars = value.ToCharArray();

            SecureString secureString;
            fixed (char* pChars = chars)
            {
                secureString = new SecureString(pChars, chars.Length);
            }
            return secureString;
        }
    }
}
