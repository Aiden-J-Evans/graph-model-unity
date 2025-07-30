using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public class EncryptDBPasswordEditor : EditorWindow
{
    private string passwordInput = "";

    [MenuItem("Tools/Encrypt DB Password")]
    public static void ShowWindow()
    {
        GetWindow<EncryptDBPasswordEditor>("Encrypt DB Password");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enter your DB password below", EditorStyles.boldLabel);
        passwordInput = EditorGUILayout.PasswordField("Password", passwordInput);

        GUILayout.Space(10);
        if (GUILayout.Button("Encrypt and Save"))
        {
            if (string.IsNullOrEmpty(passwordInput))
            {
                EditorUtility.DisplayDialog("Error", "Password cannot be empty.", "OK");
                return;
            }

            EncryptAndSave(passwordInput);
            EditorUtility.DisplayDialog("Success", "Encrypted password and key saved to StreamingAssets.", "OK");
            passwordInput = "";
        }
    }

    private void EncryptAndSave(string plainPassword)
    {
        using (Aes aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            byte[] key = aes.Key;
            byte[] iv = aes.IV;

            byte[] encrypted;
            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                string json = JsonUtility.ToJson(new DBConfig { dbPassword = plainPassword });
                sw.Write(json);
                sw.Flush();
                cs.FlushFinalBlock();
                encrypted = ms.ToArray();
            }

            string streamingPath = Path.Combine(Application.streamingAssetsPath);
            Directory.CreateDirectory(streamingPath);

            File.WriteAllBytes(Path.Combine(streamingPath, "db_config.enc"), encrypted);
            File.WriteAllText(Path.Combine(streamingPath, "db_key.txt"),
                $"{System.Convert.ToBase64String(key)}\n{System.Convert.ToBase64String(iv)}");

            AssetDatabase.Refresh();
        }
    }

    [System.Serializable]
    private class DBConfig
    {
        public string dbPassword;
    }
}
