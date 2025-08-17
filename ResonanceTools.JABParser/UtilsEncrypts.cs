using System;
using System.Security.Cryptography;
using System.Text;

namespace ResonanceTools.JABParser;

public enum EncryptType
{
    Aes,
    Des
}

public class UtilsEncrypts
{
    /// <summary>
    /// Decodes the content using the specified encryption type and key.
    /// Exact reimplementation of Assembly-CSharp.HK.Core.Utils.UtilsEncrypts
    /// </summary>
    private const string secretKey = "GQDstcKsx0NHjPOuXOYg5MbeJ1XT0uFiwDVvVBrk";

    public static string DecodeContent(string iContent, EncryptType iType, string iKey = secretKey)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(iKey);
        switch (iType)
        {
            case EncryptType.Aes:
                byte[] aesKey = new byte[16];
                Array.Copy(keyBytes, aesKey, Math.Min(aesKey.Length, keyBytes.Length));
                return DecodeAes(iContent, aesKey);
            case EncryptType.Des:
                byte[] desKey = new byte[8];
                Array.Copy(keyBytes, desKey, Math.Min(desKey.Length, keyBytes.Length));
                return DecodeDes(iContent, desKey);
            default:
                throw new ArgumentException("Invalid encryption type");
        }
    }

    private static string DecodeAes(string iContent, byte[] iKey)
    {
        byte[] cipherBytes = Convert.FromBase64String(iContent);
        using (var aes = Aes.Create())
        {
            aes.Key = iKey;
            aes.IV = iKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var ms = new MemoryStream(cipherBytes))
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var sr = new MemoryStream())
            {
                cs.CopyTo(sr);
                return Encoding.UTF8.GetString(sr.ToArray());
            }
        }
    }

    private static string DecodeDes(string iContent, byte[] iKey)
    {
        byte[] cipherBytes = Convert.FromBase64String(iContent);
        using (var des = DES.Create())
        {
            des.Key = iKey;
            des.IV = iKey;
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.PKCS7;

            using (var ms = new MemoryStream(cipherBytes))
            using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read))
            using (var sr = new MemoryStream())
            {
                cs.CopyTo(sr);
                return Encoding.UTF8.GetString(sr.ToArray());
            }
        }
    }
}
