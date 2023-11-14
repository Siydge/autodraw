using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Newtonsoft.Json.Linq;
using RestSharp.Authenticators.OAuth2;
using RestSharp;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Autodraw;

public partial class DevTest : Window
{
    public DevTest()
    {
        InitializeComponent();
        TestBenchmarking.Click += (object? sender, RoutedEventArgs e) => Benchmark();
        TestPopup.Click += TestPopup_Click;
        GenerateImage.Click += GenerateImage_ClickAsync;
    }

    private async void GenerateImage_ClickAsync(object? sender, RoutedEventArgs e)
    {
        var OpenAIKey = Config.getEntry("OpenAIKey");
        var prompt = "A male individual";
        if (OpenAIKey == null) { new MessageBox().ShowMessageBox("Error!", "You have not set up an API key!", "error"); return; }
        var options = new RestClientOptions("https://api.openai.com/v1/images/generations")
        {
            Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(OpenAIKey, "Bearer"),
        };

        var client = new RestClient(options);

        var request = new RestRequest()
        {
            Method = Method.Post,
            RequestFormat = DataFormat.Json
        };

        var param = new
        {
            prompt = prompt,
            n = 1,
            size = "1024x1024"
        };

        request.AddJsonBody(param);

        dynamic jsonResponse = JObject.Parse(client.Execute(request).Content);
        var URL = jsonResponse["data"][0]["url"].ToString();

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(URL);
            response.EnsureSuccessStatusCode();
            using (var fileStream = new FileStream(Config.FolderPath + "/temp.png", FileMode.Create))
            {
                await response.Content.CopyToAsync(fileStream);
            }
        }

        MainWindow.ImportImage(Config.FolderPath + "/temp.png");
        File.Delete(Config.FolderPath + "/temp.png");
    }

    private void TestPopup_Click(object? sender, RoutedEventArgs e)
    {
        new MessageBox().ShowMessageBox("Hi", "Loser");
    }

    public unsafe static SKBitmap TestImage(int width, int height)
    {
        SKBitmap returnbtmp = new(width, height, false);

        byte* srcPtr = (byte*)returnbtmp.GetPixels().ToPointer();

        Random rng = new();

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                *srcPtr++ = (byte)rng.Next(0,255);
                *srcPtr++ = (byte)rng.Next(0, 255);
                *srcPtr++ = (byte)rng.Next(0, 255);
                *srcPtr++ = (byte)rng.Next(0, 255);
            }
        }

        return returnbtmp;
    }

    private void Benchmark()
    {
        Stopwatch sw = Stopwatch.StartNew();

        SKBitmap small = new(64, 64, false);
        sw.Restart();
        ImageProcessing.Process(small, new ImageProcessing.Filters() { Invert = true });
        long TimeTookSmall = sw.ElapsedMilliseconds;

        BenchmarkResults.Text = "Results:\n64x64: " + TimeTookSmall.ToString();

        SKBitmap avg = new(384, 384, false);
        sw.Restart();
        ImageProcessing.Process(avg, new ImageProcessing.Filters() { Invert = true });
        long TimeTookAvg = sw.ElapsedMilliseconds;

        BenchmarkResults.Text = "Results:\n64x64: " + TimeTookSmall.ToString() + "\n384x384: " + TimeTookAvg.ToString();

        SKBitmap med = new(1024, 1024, false);
        sw.Restart();
        ImageProcessing.Process(med, new ImageProcessing.Filters() { Invert = true });
        long TimeTookMed = sw.ElapsedMilliseconds;

        BenchmarkResults.Text = "Results:\n64x64: " + TimeTookSmall.ToString() + "\n384x384: " + TimeTookAvg.ToString() + "\n1024x1024: " + TimeTookMed.ToString();

        SKBitmap large = new(3072, 3072, false);
        sw.Restart();
        ImageProcessing.Process(large, new ImageProcessing.Filters() { Invert = true });
        long TimeTookLarge = sw.ElapsedMilliseconds;

        sw.Reset();

        BenchmarkResults.Text = "Results:\n64x64: "+TimeTookSmall.ToString()+"\n384x384: "+TimeTookAvg.ToString() + "\n1024x1024: " + TimeTookMed.ToString() + "\n4096x4096: " + TimeTookLarge.ToString();

        small.Dispose();
        avg.Dispose();
        med.Dispose();
        large.Dispose();
    }
}