using System;
using System.Security;

namespace HomegearLib.RPC
{
    public class SslInfo
    {
        public bool VerifyHostname { get; set; } = true;

        public bool CheckCertificateRevocationStatus { get; set; } = false;

        public SecureString Username = new SecureString();

        public SecureString Password = new SecureString();

        public string ClientCertificateFile;

        public SecureString CertificatePassword = new SecureString();

        public string CaCertificate { get; set; }

        public SslInfo()
        {
        }

        public SslInfo(Tuple<SecureString, SecureString> usernamePassword)
        {
            Username = usernamePassword.Item1;
            Password = usernamePassword.Item2;
        }

        public SslInfo(Tuple<SecureString, SecureString> usernamePassword, bool verifyHostname, bool checkCertificateRevocationStatus)
        {
            Username = usernamePassword.Item1;
            Password = usernamePassword.Item2;
            VerifyHostname = verifyHostname;
            CheckCertificateRevocationStatus = checkCertificateRevocationStatus;
        }

        public SslInfo(Tuple<string, string> usernamePassword)
        {
            SetUsernameFromString(usernamePassword.Item1);
            SetPasswordFromString(usernamePassword.Item2);
        }

        public SslInfo(Tuple<string, string> usernamePassword, bool verifyHostname, bool checkCertificateRevocationStatus)
        {
            SetUsernameFromString(usernamePassword.Item1);
            SetPasswordFromString(usernamePassword.Item2);
            VerifyHostname = verifyHostname;
            CheckCertificateRevocationStatus = checkCertificateRevocationStatus;
        }

        public SslInfo(string clientCertificateFile, SecureString certificatePassword)
        {
            ClientCertificateFile = clientCertificateFile;
            CertificatePassword = certificatePassword;
        }

        public SslInfo(string clientCertificateFile, SecureString certificatePassword, bool verifyHostname, bool checkCertificateRevocationStatus)
        {
            ClientCertificateFile = clientCertificateFile;
            CertificatePassword = certificatePassword;
            VerifyHostname = verifyHostname;
            CheckCertificateRevocationStatus = checkCertificateRevocationStatus;
        }

        public SslInfo(string clientCertificateFile, string certificatePassword)
        {
            ClientCertificateFile = clientCertificateFile;
            SetCertificatePasswordFromString(certificatePassword);
        }

        public SslInfo(string clientCertificateFile, string certificatePassword, bool verifyHostname, bool checkCertificateRevocationStatus)
        {
            ClientCertificateFile = clientCertificateFile;
            SetCertificatePasswordFromString(certificatePassword);
            VerifyHostname = verifyHostname;
            CheckCertificateRevocationStatus = checkCertificateRevocationStatus;
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
