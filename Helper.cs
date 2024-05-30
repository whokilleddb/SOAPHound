using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

class Randomizer
{
    public static string GenerateRandomString(int length)
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder sb = new StringBuilder();
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            foreach (byte b in bytes)
            {
                sb.Append(validChars[b % validChars.Length]);
            }
        }
        return sb.ToString();
    }
}
class PostToUrl
{
    public static bool PostMessage(string url, string jsonStr)
    {
        try
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                // Bypass SSL certificate validation (for testing purposes only)
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    StringContent content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                    var task = Task.Run(() => client.PostAsync(url, content));
                    task.Wait();
                    HttpResponseMessage response = task.Result;

                    // Return true if the request was sent, even if the status code was in the 4xx range
                    return response.IsSuccessStatusCode || (response.StatusCode >= System.Net.HttpStatusCode.BadRequest && response.StatusCode < System.Net.HttpStatusCode.InternalServerError);
                }
            }
        }
        catch
        {
            // Return false for any other case (e.g., network issues, invalid URL)
            return false;
        }
    }

    public static string FetchCache(string url, string id)
    {
        try
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                // Bypass SSL certificate validation (for testing purposes only)
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    try
                    {
                        string url_id = url + "cache?id=" + id;
                        var task1 = Task.Run(() => client.GetAsync(url_id));
                        task1.Wait();
                        HttpResponseMessage response = task1.Result;
                        response.EnsureSuccessStatusCode();

                        var task2 = Task.Run(() => response.Content.ReadAsStringAsync());
                        task2.Wait();
                        string responseBody = task2.Result;
                        return responseBody;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        return null;
                    }
                }
            }
        } catch
        {
            return null;
        }
    }
}

public class Base64Encoder
{
    public static string EncodeToBase64(string plainText)
    {
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string DecodeFromBase64(string base64String)
    {
        try
        {
            byte[] decodedBytes = Convert.FromBase64String(base64String);
            string decodedString = Encoding.UTF8.GetString(decodedBytes);
            return decodedString;
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error: Invalid Base64 string. {ex.Message}");
            return null;
        }
    }
}