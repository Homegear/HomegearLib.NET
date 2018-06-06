using System;
using System.Security;

namespace HomegearLib.RPC
{
    public class SslInfo
    {
        private bool _verifyCertificate = true;
        public bool VerifyCertificate { get { return _verifyCertificate; } set { _verifyCertificate = value; } }

        public SecureString Username;

        public SecureString Password;

        public string ClientCertificateFile;

        public SecureString CertificatePassword;

        public SslInfo()
        {
        }

        public SslInfo(Tuple<SecureString, SecureString> usernamePassword)
        {
            Username = usernamePassword.Item1;
            Password = usernamePassword.Item2;
        }

        public SslInfo(Tuple<SecureString, SecureString> usernamePassword, bool verifyCertificate)
        {
            Username = usernamePassword.Item1;
            Password = usernamePassword.Item2;
            _verifyCertificate = verifyCertificate;
        }

        public SslInfo(Tuple<string, string> usernamePassword)
        {
            SetUsernameFromString(usernamePassword.Item1);
            SetPasswordFromString(usernamePassword.Item2);
        }

        public SslInfo(Tuple<string, string> usernamePassword, bool verifyCertificate)
        {
            SetUsernameFromString(usernamePassword.Item1);
            SetPasswordFromString(usernamePassword.Item2);
            _verifyCertificate = verifyCertificate;
        }

        public SslInfo(string clientCertificateFile, SecureString certificatePassword)
        {
            ClientCertificateFile = clientCertificateFile;
            CertificatePassword = certificatePassword;
        }

        public SslInfo(string clientCertificateFile, SecureString certificatePassword, bool verifyCertificate)
        {
            ClientCertificateFile = clientCertificateFile;
            CertificatePassword = certificatePassword;
            _verifyCertificate = verifyCertificate;
        }

        public SslInfo(string clientCertificateFile, string certificatePassword)
        {
            ClientCertificateFile = clientCertificateFile;
            SetCertificatePasswordFromString(certificatePassword);
        }

        public SslInfo(string clientCertificateFile, string certificatePassword, bool verifyCertificate)
        {
            ClientCertificateFile = clientCertificateFile;
            SetCertificatePasswordFromString(certificatePassword);
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
