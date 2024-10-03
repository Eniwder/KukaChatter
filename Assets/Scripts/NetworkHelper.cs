using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkHelper
{
    static string serverUrl = "http://localhost:" + ConfigLoader.GetPort() + "/"; // ExpressサーバーのURL

    public static async Task<HttpContent> PostJsonAsync(string path, string jsonContent)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(
                    jsonContent,
                    System.Text.Encoding.UTF8,
                    "application/json"
                );
                client.Timeout = TimeSpan.FromSeconds(5); // localだし5秒でタイムアウト
                HttpResponseMessage response = await client.PostAsync(serverUrl + path, content);
                return response.Content;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"通信エラー: {ex.Message}");
            return null;
        }
    }
}
