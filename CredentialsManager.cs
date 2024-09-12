using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PeerReviewClient
{
    public class CredentialsManager
    {
        // 32 bytes for AES-256
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("rQ7bjJd0fjGvd1ToB3rRg4A9zTzJgkUE");
        // 16 bytes for AES
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("f5jKZkdfRi4guH1c");

        private readonly string _filePath;

        public CredentialsManager(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Ottiene le credenziali, se esistono, dal file.
        /// </summary>
        public Credentials GetCredentials()
        {
            var credentials = GetCredentialFromFile(_filePath);
            var isCredentialsFound = credentials != null;

            if (credentials == null)
            {
                // File non trovato. Chiedo tramite console
                credentials = GetCredentialsFromConsole();
            }
            else
            {
                credentials.isCredentialFileExist = true;
            }

            return credentials;

        }

        /// <summary>
        /// Legge le credenziali dal file di configurazione.
        /// </summary>
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

        public static Credentials GetCredentialsFromConsole()
        {
            string mail;
            string password;
            string? courseID;
            int role;

            while (true)
            {
                Console.Write("Inserisci la tua mail: ");
                mail = Console.ReadLine();

                Console.Write("Inserisci la tua password: ");
                password = ReadPassword();

                Console.Write("Inserisci l'ID del tuo corso o il codice fornito se è la prima volta che ti colleghi: ");
                courseID = Console.ReadLine();

                Console.Write($"Inserisci il tuo ruolo ({(int)PeerReviewRole.student} studente, {(int)PeerReviewRole.teacher} docente): ");
                var tRole = Console.ReadLine();
                var isValidRole = int.TryParse(tRole, out role);

                // Check if email is valid
                var isValiMail = new EmailAddressAttribute().IsValid(mail);

                if (string.IsNullOrEmpty(mail) || string.IsNullOrEmpty(password) || isValiMail == false || string.IsNullOrEmpty(courseID) || isValidRole == false)
                {
                    Console.WriteLine("Mail, ruolo o password non validi, riprova.");
                }
                else
                {
                    break;
                }
            }

            return new Credentials
            {
                email = mail,
                password = password,
                courseID = courseID,
                role = (PeerReviewRole)role
            };

        }

        /// <summary>
        /// Legge la password mascherando l'input.
        /// </summary>
        public static string ReadPassword()
        {
            StringBuilder password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(intercept: true); // intercept: true per non mostrare il carattere

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    Console.Write("*"); // Mostra un asterisco per ogni carattere digitato
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b"); // Cancella l'ultimo carattere e il relativo asterisco
                }
            }
            while (key.Key != ConsoleKey.Enter); // Continua fino a quando non si preme Invio

            Console.WriteLine(); // Sposta il cursore alla riga successiva
            return password.ToString();
        }

        /// <summary>
        /// Salva le credenziali in un file JSON.
        /// </summary>
        public bool SaveCredentials(Credentials credentials)
        {
            try
            {
                var json = JsonConvert.SerializeObject(credentials);
                File.WriteAllText(_filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore durante il salvataggio delle credenziali: " + ex.Message);
                return false;
            }
        }

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


    }

}
