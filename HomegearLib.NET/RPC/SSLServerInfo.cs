using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;

namespace HomegearLib.RPC
{
    public class SSLServerInfo
    {
        private SecureString _username = new SecureString();
        public SecureString Username { get { return _username; } set { _username = value; } }

        private SecureString _password = new SecureString();
        public SecureString Password { get { return _password; } set { _password = value; } }

        private string _certificatePath = "";
        public string CertificatePath { get { return _certificatePath; } set { _certificatePath = value; } }

        private SecureString _certificatePassword = new SecureString();
        public SecureString CertificatePassword { get { return _certificatePassword; } set { _certificatePassword = value; } }

        public SSLServerInfo(string certificatePath, SecureString certificatePassword)
        {
            _certificatePath = certificatePath;
            _certificatePassword = certificatePassword;
        }

        public SSLServerInfo(string certificatePath, SecureString certificatePassword, SecureString username, SecureString password)
        {
            _certificatePath = certificatePath;
            _certificatePassword = certificatePassword;
            _username = username;
            _password = password;
        }

        public SSLServerInfo(string certificatePath, string certificatePassword)
        {
            _certificatePath = certificatePath;
            SetCertificatePasswordFromString(certificatePassword);
        }

        public SSLServerInfo(string certificatePath, string certificatePassword, string username, string password)
        {
            _certificatePath = certificatePath;
            SetCertificatePasswordFromString(certificatePassword);
            SetUsernameFromString(username);
            SetPasswordFromString(password);
        }

        public void SetUsernameFromString(string username)
        {
            _username = GetSecureString(username);
        }

        public void SetPasswordFromString(string password)
        {
            _password = GetSecureString(password);
        }

        public void SetCertificatePasswordFromString(string password)
        {
            _certificatePassword = GetSecureString(password);
        }

        unsafe SecureString GetSecureString(string value)
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
