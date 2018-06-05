using System;
using System.Security;

namespace HomegearLib.RPC
{
    public class SSLClientInfo
    {
        private bool _verifyCertificate = true;
        public bool VerifyCertificate { get { return _verifyCertificate; } set { _verifyCertificate = value; } }

        public SecureString Username;

        public SecureString Password;

        public string ClientCertificateFile;

        public SecureString CertificatePassword;

        public SSLClientInfo()
        {
        }

        public SSLClientInfo(SecureString username, SecureString password)
        {
            Username = username;
            Password = password;
        }

        public SSLClientInfo(SecureString username, SecureString password, bool verifyCertificate)
        {
            Username = username;
            Password = password;
            _verifyCertificate = verifyCertificate;
        }

        public SSLClientInfo(string username, string password)
        {
            SetUsernameFromString(username);
            SetPasswordFromString(password);
        }

        public SSLClientInfo(string username, string password, bool verifyCertificate)
        {
            SetUsernameFromString(username);
            SetPasswordFromString(password);
            _verifyCertificate = verifyCertificate;
        }

        public void SetUsernameFromString(string username)
        {
            Username = GetSecureString(username);
        }

        public void SetPasswordFromString(string password)
        {
            Password = GetSecureString(password);
        }

        public void SetCertificatePasswordFromString(string password)
        {
            CertificatePassword = GetSecureString(password);
        }

        unsafe SecureString GetSecureString(string value)
        {
            if (value.Length == 0) return new SecureString();

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
