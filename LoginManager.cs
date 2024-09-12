using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace PeerReviewClient
{
    public class LoginManager
    {
        // 32 bytes for AES-256
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("rQ7bjJd0fjGvd1ToB3rRg4A9zTzJgkUE");
        // 16 bytes for AES
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("f5jKZkdfRi4guH1c");

        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static bool SaveLoginInfo(Credentials loginInfo, string filePath = "loginInfo.json")
        {
            try
            {
                loginInfo.password = Encrypt(loginInfo.password);
                string json = JsonConvert.SerializeObject(loginInfo, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return false;
        }

        public static bool DeleteLoginInfo(string filePath = "loginInfo.json")
        {
            var fileExists = File.Exists(filePath);
            if (fileExists)
            {
                File.Delete(filePath);
            }
            else
            {
                return true;
            }

            if (fileExists && File.Exists(filePath) == false)
            {
                return true;
            }

            return false;
        }

        public static Credentials GetCredentialFromFile(string filePath = "loginInfo.json")
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            string json = File.ReadAllText(filePath);
            var loadedLoginInfo = JsonConvert.DeserializeObject<Credentials>(json);
            loadedLoginInfo.password = Decrypt(loadedLoginInfo.password);
            var jsonRealPassword = JsonConvert.SerializeObject(loadedLoginInfo, Formatting.Indented);
            return JsonConvert.DeserializeObject<Credentials>(jsonRealPassword);
        }




    }
}
