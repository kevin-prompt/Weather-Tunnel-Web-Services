using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WSHelpers
{
    public class ClassAud
    {
        // Internal State
        private string m_UserId;        // Common user id, e.g. email address, GUID
        private string m_Machine;       // Name of the machine application is on
        private string m_AppName;       // Name of the application method is under
        private string m_MethodName;    // Name of the method being called
        private string m_Parameters;    // Parameters used in method call.
        private byte[] m_ParametersRaw; // Parameters used in method call - encrypted.
        private string m_Source;        // Any information about the caller.
        private int m_Billing;          // Billing code.  Note: not part of the validation mark.
        private string mIV;             // This is the initialization vector for the encryption/decryption
        private string mKey;            // This is the key for the encryption/decryption

        // Constructors
        public ClassAud()
        {
            Clear();
            LoadKeys();

        }
        public ClassAud(string userId, string machine, string source, string app, string method, string parms, int billing)
        {
            Clear();
            LoadKeys();
            m_UserId = userId;
            m_Machine = machine;
            m_Source = source;
            m_AppName = app;
            m_MethodName = method;
            Parameters = parms;
            m_Billing = billing;
        }

        // Properties
        public string UserId
        {
            get { return m_UserId; }
            set { m_UserId = value; }
        }

        public string Machine
        {
            get { return m_Machine; }
            set { m_Machine = value; }
        }

        public string AppName
        {
            get { return m_AppName; }
            set { m_AppName = value; }
        }

        public string MethodName
        {
            get { return m_MethodName; }
            set { m_MethodName = value; }
        }

        public string Parameters
        {
            get { return m_Parameters; }
            // Sometimes the unicode signal character ends up in the parameter stream, but the DB will screen it out and cause validation issues with the hash.
            set { string holdParm = value.Substring(0, (value.Length < 1024 ? value.Length : 1024)); m_Parameters = holdParm.Replace("\uFEFF", ""); }
        }

        public byte[] ParametersRaw
        {
            get { return m_ParametersRaw; }
            set
            {
                m_ParametersRaw = value;
                Parameters = GetParametersRaw(value);
            }
        }

        public string GetParametersRaw(byte[] raw)
        {
            string holdParameters = DeRinjndaelThisThing(mIV, mKey, raw);
            return holdParameters.Remove(holdParameters.IndexOf("\0")); // remove any extra padding
        }

        public string Source
        {
            get { return m_Source; }
            set { m_Source = value; }
        }

        public int Billing
        {
            get { return m_Billing; }
            set { m_Billing = value; }
        }
        public byte[] Signature
        {
            get { return (mIV.Length > 0 && mKey.Length > 0) ? RinjndaelThisThing(mIV, mKey, SleeveHash(quicklook())) : new byte[0]; }
        }

        // Methods
        public bool Validate(byte[] mark)
        {
            try
            {
                // Take a hash of the existing object and check it against the digitally signed hash.
                // The digitally signed hash may be padded, so trim it to the expected size.
                string realHash = SleeveHash(quicklook());
                string suspectHash = DeRinjndaelThisThing(mIV, mKey, mark).Substring(0, realHash.Length);
                return realHash.Equals(suspectHash);
            }
            catch { return false; }
        }

        public void Encrypt(string parms)
        {
            m_ParametersRaw = RinjndaelThisThing(mIV, mKey, parms);
        }

        // Private Methods
        private void Clear()
        {
            m_UserId = "";
            m_Machine = "";
            m_Source = "";
            m_AppName = "";
            m_MethodName = "";
            m_Parameters = "";
            m_ParametersRaw = new byte[0];
            m_Billing = 0;
            mIV = "";
            mKey = "";
        }

        private void LoadKeys()
        {
            // The Initialization Vector should be 1/2 the key size
            // The Key size should be 256 bits (32 bytes).
            try
            {
                mIV = RoleEnvironment.GetConfigurationSettingValue("Audit_IV");
                mKey = RoleEnvironment.GetConfigurationSettingValue("Audit_Key");
            }
            catch { /*throw new ClassErr(ClassErr.ERR_CODES.ERR_CONFIG, this.ToString());*/ }
        }

        private byte[] RinjndaelThisThing(string iv, string key, string phrase)
        {
            // This method will encrypt the input phrase based on the iv and key
            // using the RinJndael algorithm.

            // Create a Rinjndael symmetric encryption algorithm 
            RijndaelManaged Rinjndael = new RijndaelManaged();

            // Populate Key/IV/phase
            byte[] IV128 = Encoding.ASCII.GetBytes(iv);        // This should be 128 bits
            byte[] key256 = Encoding.ASCII.GetBytes(key);      // This should be 256 bits
            byte[] toEncrypt = Encoding.ASCII.GetBytes(phrase);// This is the string to be encrypted

            // Set up Rinjndael parameters
            Rinjndael.Padding = PaddingMode.PKCS7;
            Rinjndael.Mode = CipherMode.CBC;
            Rinjndael.IV = IV128;
            Rinjndael.Key = key256;
            ICryptoTransform encryptor = Rinjndael.CreateEncryptor();

            // Encrypt the data
            MemoryStream msEncrypt = new MemoryStream();
            CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
            csEncrypt.FlushFinalBlock();
            // Get encrypted array of bytes.
            return msEncrypt.ToArray();
        }

        private string DeRinjndaelThisThing(string iv, string key, byte[] package)
        {
            // This method will decrypt the input phase using the iv and key
            // based on the RinJndael algorithm.

            // Create a Rinjndael symmetric encryption algorithm 
            RijndaelManaged Rinjndael = new RijndaelManaged();

            // Populate Key/IV/phase
            byte[] IV128 = Encoding.ASCII.GetBytes(iv);       // This should be 128 bits
            byte[] key256 = Encoding.ASCII.GetBytes(key);     // This should be 256 bits
            byte[] holdPhase = new byte[package.Length];

            // Set up Rinjndael parameters
            Rinjndael.Padding = PaddingMode.PKCS7;
            Rinjndael.Mode = CipherMode.CBC;
            Rinjndael.IV = IV128;
            Rinjndael.Key = key256;
            ICryptoTransform decryptor = Rinjndael.CreateDecryptor();

            //Decrypt the data
            MemoryStream msDecrypt = new MemoryStream(package);
            CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            csDecrypt.Read(holdPhase, 0, holdPhase.Length);
            return Encoding.ASCII.GetString(holdPhase);
        }

        private string SleeveHash(string sleeve)
        {
            UTF8Encoding textConverter = new UTF8Encoding();
            byte[] passBytes = textConverter.GetBytes(sleeve);
            byte[] theHash = new SHA384Managed().ComputeHash(passBytes);
            return System.Convert.ToBase64String(theHash);
        }

        private string quicklook()
        {
            return m_UserId + m_Machine + m_Source + m_AppName + m_MethodName + m_Parameters;
        }

    }
}