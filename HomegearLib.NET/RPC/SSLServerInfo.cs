using System;
using System.Security;

namespace HomegearLib.RPC
{
    public class SSLServerInfo
    {
        private SecureString _username = new SecureString();
        public SecureString Username { get { return _username; } set { _username = value; } }

        private SecureString _password = new SecureString();
        public SecureString Password { get { return _password; } set { _password = value; } }

        private String _certificatePath = "";
        public String CertificatePath { get { return _certificatePath; } set { _certificatePath = value; } }

        private SecureString _certificatePassword = new SecureString();
        public SecureString CertificatePassword { get { return _certificatePassword; } set { _certificatePassword = value; } }

        public SSLServerInfo(String hostname, String certificatePath, SecureString certificatePassword)
        {
            _certificatePath = certificatePath;
            _certificatePassword = certificatePassword;
        }

        public SSLServerInfo(String certificatePath, SecureString certificatePassword, SecureString username, SecureString password)
        {
            _certificatePath = certificatePath;
            _certificatePassword = certificatePassword;
            _username = username;
            _password = password;
        }

        public SSLServerInfo(String certificatePath, String certificatePassword)
        {
            _certificatePath = certificatePath;
            SetCertificatePasswordFromString(certificatePassword);
        }

        public SSLServerInfo(String certificatePath, String certificatePassword, String username, String password)
        {
            _certificatePath = certificatePath;
            SetCertificatePasswordFromString(certificatePassword);
            SetUsernameFromString(username);
            SetPasswordFromString(password);
        }

        public void SetUsernameFromString(String username)
        {
            _username = GetSecureString(username);
        }

        public void SetPasswordFromString(String password)
        {
            _password = GetSecureString(password);
        }

        public void SetCertificatePasswordFromString(String password)
        {
            _certificatePassword = GetSecureString(password);
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
