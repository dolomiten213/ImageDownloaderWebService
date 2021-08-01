using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string uri = null;
            int imagesCount = -1;
            var maxThreadCount = -1;
            try
            {
                Console.Write("url:");
                uri = Console.ReadLine();

                Console.Write("Images Count:");
                imagesCount = Int32.Parse(Console.ReadLine());

                Console.Write("Max thread count:");
                maxThreadCount = Int32.Parse(Console.ReadLine());
            }
            catch
            {
                Console.WriteLine("Incorrect input");
                return;
            }
            
            using HttpClient client = new HttpClient();
            string url = ConfigurationManager.AppSettings["SERVER_URI"];
            
            var json = JsonConvert.SerializeObject(new ImageDownloaderInput
            {
                Uri = uri ?? "https://www.google.com/search?q=popst+vs+put&oq=popst+vs+put&aqs=chrome..69i57j0i13l9.2288j0j1&sourceid=chrome&ie=UTF-8",
                ImagesCount = imagesCount,
                MaxThreadCount = maxThreadCount
            });
            var sendingData = new StringContent(json, Encoding.UTF8, "application/json");


            using var ans = await client.PostAsync(url, sendingData);           
            var data = JsonConvert.DeserializeObject <List<ImageDownloaderOutput>> (await ans.Content.ReadAsStringAsync());

            
            Console.WriteLine(ans.Headers);
            foreach (var a in data)
            {
                Console.WriteLine(a);
            }        
        }
    }
}
