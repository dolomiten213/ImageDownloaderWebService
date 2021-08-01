
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoWebService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ImageDownloaderController : ControllerBase
    {
        private readonly ILogger<ImageDownloaderController> _logger;

        public ImageDownloaderController(ILogger<ImageDownloaderController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<List<ImageDownloaderOutput>> Get(ImageDownloaderInput i)
        {
            _logger.LogInformation($"Getting request with:\n\t uri={i.Uri},\n\t imagesCount={i.ImagesCount},\n\t maxThreadCount={i.MaxThreadCount}");
            var headerKey = "Operation-Status";
            var imageDownloader = new ImageDownloader();
            
            try
            {
                await imageDownloader.DownloadImagesFromURI(i.Uri, i.ImagesCount, i.MaxThreadCount);
                Response.Headers.Add(headerKey, new StringValues("Success"));
                _logger.LogInformation("Parsing and donwloading success!");
            }
            
            catch(AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    _logger.LogWarning(ex.Message);
                }
                Response.Headers.Add(headerKey, new StringValues($"Couldn't recognize some ({e.InnerExceptions.Count}) img src attribute"));
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning($"{e.Message}, Waiting time exceeded");
                Response.Headers.Add(headerKey, new StringValues("Fail: Waiting time exceeded"));
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                Console.WriteLine(e.Message);
                Response.Headers.Add(headerKey, new StringValues(e.Message));
            }
            _logger.LogInformation($"Sending response with {headerKey} {Response.Headers[headerKey]}");
            
            return imageDownloader.Result;
        }
    }
    
}
