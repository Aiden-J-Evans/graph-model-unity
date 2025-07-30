using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class DBConfigLoader
{
    public static string LoadDecryptedPassword()
    {
        string basePath = Application.streamingAssetsPath;

        string encPath = Path.Combine(basePath, "db_config.enc");
        string keyPath = Path.Combine(basePath, "db_key.txt");

        byte[] encryptedData = File.ReadAllBytes(encPath);
        string[] keyLines = File.ReadAllLines(keyPath);

        if (keyLines.Length < 2)
        {
            Debug.LogError("Invalid key file format.");
            return null;
        }

        byte[] key = System.Convert.FromBase64String(keyLines[0]);
        byte[] iv = System.Convert.FromBase64String(keyLines[1]);

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(encryptedData))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                string json = sr.ReadToEnd();
                var config = JsonUtility.FromJson<DBConfig>(json);
                return config.dbPassword;
            }
        }
    }

    [System.Serializable]
    private class DBConfig
    {
        public string dbPassword;
    }
}
