using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace PhotoWebService
{ 
    class ImageDownloader
    {
        public List<ImageDownloaderOutput> Result { get; private set; } = new List<ImageDownloaderOutput>();

//===============================================================================================//

        public async Task DownloadImagesFromURI(string urii, int imagesCount, int maxThreadCount)
        {
            Result = new List<ImageDownloaderOutput>();
            CheckInput(imagesCount, maxThreadCount);
            Uri uri = CheckUri(urii);

            using (var cts = new CancellationTokenSource())
            {
                var token = cts.Token;
                cts.CancelAfter(Int32.Parse(ConfigurationManager.AppSettings["TIMEOUT"])*1000);

                var images = await ParseHtmlToImages(uri);

                var imagesCountToDownload = Math.Min(imagesCount, images.Count);
                var exceptions = new ConcurrentQueue<Exception>();

                CreateDirecory("images");
                var result = Parallel.For(0, imagesCountToDownload, new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount, CancellationToken = token }, i =>
                {
                    using (WebClient client = new WebClient())
                    {
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            var bytes = client.DownloadData(images[i].src);
                            token.ThrowIfCancellationRequested();
                            File.WriteAllBytes($"images/{i} {FileNameGenerator(images[i].src)}", bytes);
                            images[i].size = bytes.Length;

                            lock (Result)
                            {
                                AddToResult(images[i]);
                            }
                        }
                        catch (Exception e)
                        {
                            exceptions.Enqueue(new Exception(e.Message + images[i].src));
                        }
                    }
                });
                if (exceptions.Count != 0)
                {
                    throw new AggregateException(exceptions);
                }
            }
        }

//===============================================================================================//
        
        private void CheckInput(int images, int threads)
        {
            if (images < 1) throw new Exception("Incorrect images count");
            if (threads < 1) throw new Exception("Incorrect thread count");
        }
        private void CreateDirecory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        private void AddToResult(Image image)
        {
            string domain;
            try
            {
                Uri uri = CheckUri(image.src);
                domain = uri.DnsSafeHost;
            }
            catch
            {
                domain = "localhost";
            }

            var imagesItem = (from res in Result
                              where (res.Host == domain)
                              select res).FirstOrDefault();

            if (imagesItem == null)
            {
                Result.Add(new ImageDownloaderOutput
                {
                    Host = domain,
                    Images = new List<Image>
                    {
                        image
                    },
                });
            }
            else
            {
                Result[Result.IndexOf(imagesItem)].Images.Add(image);
            }
        }
        private async Task<List<Image>> ParseHtmlToImages(Uri uri)
        {
            var htmlCode = await GetHtmlCodeAsync(uri.AbsoluteUri);
            File.WriteAllText("file.txt", htmlCode);

            var imageTags = GetImgTags(htmlCode);

            var images = new List<Image>();
            foreach (var tag in imageTags)
            {
                string src = null;
                string alt = null;
                try
                {
                    src = GetImageAttribute(tag, "src", uri);
                    alt = GetImageAttribute(tag, "alt", uri);
                }
                catch
                {

                }

                if (src.Length > 0)
                {
                    var a = new Image
                    {
                        src = src,
                        alt = alt,
                        size = 0
                    };
                    images.Add(a);
                }
            }
            return images;
        }
        private Uri CheckUri(string urii)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(urii);
                return uri;
            }
            catch
            {
                throw new Exception("Incorrect uri");
            }
        }
        private async Task<string> GetHtmlCodeAsync(string uri)
        {
            string htmlCode = String.Empty;
            using (WebClient client = new WebClient())
            {
                try
                {
                    htmlCode = await client.DownloadStringTaskAsync(uri);
                }
                catch
                {
                    throw new Exception("Failed to download html page");
                }

            }
            return htmlCode;
        }
        private string FileNameGenerator(string source)
        {
            if (source[source.Length - 1] == '/')
            {
                source = source.Trim('/');
            }
            var name = source.Substring(source.LastIndexOf('/') + 1);
            name = new String(name.Where(Char.IsLetter).ToArray());
            return name + GetFileFormat(source);
        }
        private string GetFileFormat(string source)
        {
            var format = source.Substring(source.LastIndexOf("."));
            return format.Length < 6 ? format : ".png";
        }
        private string GetImageAttribute(string imageTag, string attribute, Uri uri)
        {
            imageTag.Trim(' ');
            var a = GetSubstringsBetween(imageTag, $"{attribute}=\"", "\"");
            if (a.Count == 0)
            {
                a = GetSubstringsBetween(imageTag, $"{attribute}='", "'");
                if (a.Count == 0)
                {
                    return String.Empty;
                }
            }
            var attributeLegth = attribute.Length + "='".Length;

            var src = a[0].Substring(attributeLegth, a[0].Length - attributeLegth - 1);

            if (src.Length == 0) return String.Empty;

            if (src[0] == '\\' || src[0] == '/')
            {
                if (src[1] == '\\' || src[1] == '/')
                {
                    src = uri.Scheme + ":" + src;
                }
                else
                {
                    src = "http://" + uri.DnsSafeHost + src;
                }
            }

            return src;
        }
        private List<string> GetImgTags(string htmlCode)
        {
            List<string> imgs = null;
            try
            {
                imgs = GetSubstringsBetween(htmlCode, "<img ", ">");
            }
            catch
            {
                throw new Exception($"Failed to parse string with \"<img\" \">\": {htmlCode} ");
            }

            if (imgs.Count == 0)
            {
                throw new Exception("No <img> tags in code");
            }
            return imgs;
        }
        private List<string> GetSubstringsBetween(string str, string startSubstring, string stopSubstring)
        {
            if (String.IsNullOrEmpty(str))
            {
                throw new Exception("The string to find may not be empty");
            }

            List<int> indexes = new List<int>();
            List<string> srcs = new List<string>();
            try
            {
                for (int index = 0; ; index += startSubstring.Length)
                {
                    index = str.IndexOf(startSubstring, index);
                    if (index == -1)
                        break;
                    indexes.Add(index);
                }

                foreach (var start in indexes)
                {
                    var closedBracket = str.IndexOf(stopSubstring, start + startSubstring.Length);
                    srcs.Add(str.Substring(start, closedBracket - start + 1));
                }
            }
            catch
            {
                throw new Exception($"Failed to parse string {str}");
            }

            return srcs;
        }
    }
}
