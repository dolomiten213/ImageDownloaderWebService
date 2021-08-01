using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class Image
    {
        public string src { get; set; } = null;

        public string alt { get; set; } = null;

        public Int32 size { get; set; } = 0;

        public override string ToString()
        {
            return $"    src={src}\n    alt={alt}\n    size={size}\n\n";
        }
    }
    public class ImageDownloaderOutput
    {
        public string Host { get; set; }

        public List<Image> Images { get; set; }

        public override string ToString()
        {
            var str = $"Host: {Host}\n";
            foreach (var i in Images)
            {
                str += i.ToString() + "\n";
            }
            return str + "\n";
        }
    }
    public class ImageDownloaderInput
    {
        public string Uri { get; set; }
        public int ImagesCount { get; set; }
        public int MaxThreadCount { get; set; }
    }
}
