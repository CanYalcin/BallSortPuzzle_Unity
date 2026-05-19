using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace HyperBase.Data
{
    /// <summary>
    /// AES-256-CBC encryption for save file security.
    /// Key: SHA-256(deviceUniqueIdentifier + ProjectSalt). IV: random, prepended to ciphertext.
    /// IMPORTANT: Change ProjectSalt to a unique value per project before shipping!
    /// </summary>
    public static class EncryptionHelper
    {
        // TODO: Replace with a unique random string before shipping.
        private const string ProjectSalt = "@Shape_Of_Void_Base";
        private const int    IvSize      = 16;

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            string raw   = SystemInfo.deviceUniqueIdentifier + ProjectSalt;
            using var sha = SHA256.Create();
            byte[] key   = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

            using var aes   = Aes.Create();
            aes.Key         = key;
            aes.Mode        = CipherMode.CBC;
            aes.Padding     = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var enc  = aes.CreateEncryptor();
            byte[] plain   = Encoding.UTF8.GetBytes(plainText);
            byte[] cipher  = enc.TransformFinalBlock(plain, 0, plain.Length);

            byte[] result  = new byte[IvSize + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0,      IvSize);
            Buffer.BlockCopy(cipher, 0, result, IvSize, cipher.Length);
            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return null;
            try
            {
                byte[] all = Convert.FromBase64String(cipherText);
                if (all.Length <= IvSize) return null;

                byte[] iv     = new byte[IvSize];
                byte[] cipher = new byte[all.Length - IvSize];
                Buffer.BlockCopy(all, 0,      iv,     0, IvSize);
                Buffer.BlockCopy(all, IvSize, cipher, 0, cipher.Length);

                string raw2   = SystemInfo.deviceUniqueIdentifier + ProjectSalt;
                using var sha2 = SHA256.Create();
                byte[] key2   = sha2.ComputeHash(Encoding.UTF8.GetBytes(raw2));

                using var aes2  = Aes.Create();
                aes2.Key        = key2;
                aes2.Mode       = CipherMode.CBC;
                aes2.Padding    = PaddingMode.PKCS7;
                aes2.IV         = iv;

                using var dec  = aes2.CreateDecryptor();
                byte[] plain   = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EncryptionHelper] Decryption failed: {e.Message}");
                return null;
            }
        }
    }
}
