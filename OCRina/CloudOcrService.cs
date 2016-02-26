using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Xml.Linq;
using Abbyy.CloudOcrSdk;
using TaskStatus = Abbyy.CloudOcrSdk.TaskStatus;
using ImageMagick;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace OneLegal.OCRina
{
    public static class CloudOcrService
    {

        public static string ExtractRegionFromPng(FileInfo pngFile, OcrRegion region)
        {
            var xmlResult = CallOcrWebService(pngFile, region.TopLeftX, region.TopLeftY, region.BottomRightX, region.BottomRightY);
            return GetOcrTextFromResultXml(xmlResult);
        }

        public static string ExtractRegionFromPdf(FileInfo pdfFile, int topLeftX, int topLeftY, int bottomRightX, int bottomRightY)
        {
            FileInfo pngFile = ConvertPdfToPng(pdfFile);
            var xmlResult = CallOcrWebService(pngFile, topLeftX, topLeftY, bottomRightX, bottomRightY);
            return GetOcrTextFromResultXml(xmlResult);
        }

        public static async Task<IEnumerable<string>> ExtractTemplateFromPdfAsync(FileInfo pdfFile, OcrTemplate ocrTemplate)
        {
            FileInfo pngFile = ConvertPdfToPng(pdfFile);
            List<string> results = new List<string>();
            foreach (var region in ocrTemplate.Regions)
            {
                var xml = await CallOcrWebServiceAsync(pngFile, region.TopLeftX, region.TopLeftY, region.BottomRightX, region.BottomRightY);
                results.Add(xml);
            }            
            return results;
        }

        private static Task<string> CallOcrWebServiceAsync(FileInfo pngFile, int topLeftX, int topLeftY, int bottomRightX, int bottomRightY)
        {
            return System.Threading.Tasks.Task.Run(() => CallOcrWebService(pngFile, topLeftX, topLeftY, bottomRightX, bottomRightY));
        }

        private static string CallOcrWebService(FileInfo pngFile, int topLeftX, int topLeftY, int bottomRightX, int bottomRightY)
        {
            var restClient = new RestServiceClient
            {
                Proxy = { Credentials = CredentialCache.DefaultCredentials },
                ApplicationId = ConfigurationManager.AppSettings["ApplicationId"].ToString(),
                Password = ConfigurationManager.AppSettings["Password"].ToString()
            };

            var textFieldProcessingSettings = new TextFieldProcessingSettings
            {
                CustomOptions = "region=" + topLeftX + "," + topLeftY + "," + bottomRightX + "," + bottomRightY,
                Language = "english"
            };

            if (pngFile.DirectoryName == null)
                throw new Exception("png file directory name is blank");

            var outputXmlFile = Path.Combine(pngFile.DirectoryName, GetFileNameWithoutExtension(pngFile) + ".xml");

            // call the REST service
            var task = restClient.ProcessTextField(pngFile.ToString(), textFieldProcessingSettings);
            System.Threading.Thread.Sleep(4000);
            task = restClient.GetTaskStatus(task.Id);
            //Console.WriteLine("Task status: {0}", task.Status);
            while (task.IsTaskActive())
            {
                // Note: it's recommended that your application waits
                // at least 2 seconds before making the first getTaskStatus request
                // and also between such requests for the same task.
                // Making requests more often will not improve your application performance.
                // Note: if your application queues several files and waits for them
                // it's recommended that you use listFinishedTasks instead (which is described
                // at http://ocrsdk.com/documentation/apireference/listFinishedTasks/).
                System.Threading.Thread.Sleep(4000);
                task = restClient.GetTaskStatus(task.Id);
                //Console.WriteLine("Task status: {0}", task.Status);
            }
            if (task.Status == TaskStatus.Completed)
            {
                //Console.WriteLine("Processing completed.");
                restClient.DownloadResult(task, outputXmlFile);
                //Console.WriteLine("Download completed.");
            }
            else
            {
                //Console.WriteLine("Error while processing the task");
                return outputXmlFile;
            }
            return outputXmlFile;
        }

        private static string GetOcrTextFromResultXml(string outputXmlFile)
        {
            // Parse Xml output file for extracted value
            XElement x = XElement.Load(outputXmlFile);
            XNamespace xsi = "@link";
            if (x.Element(xsi + "field") == null)
                throw new Exception("Invalid Xml file");

            var result = x.Element(xsi + "field").Element(xsi + "value").Value;
            return result;
        }

        public static FileInfo ConvertPdfToPng(FileInfo pdfFile)
        {
            if (pdfFile.DirectoryName == null)
                throw new ArgumentException("file directory was null. why?");

            MagickReadSettings imsettings = new MagickReadSettings
            {
                Density = new PointD(300, 300), // Settings the density to 300 dpi will create an image with a better quality
                FrameIndex = 0, // first page
                FrameCount = 1, // number of pages
                Format = MagickFormat.Pdf
            };

            const MagickFormat imageOutputFormat = MagickFormat.Png;
            var pngFilename = GetFileNameWithoutExtension(pdfFile) + "." + imageOutputFormat.ToString();
            var pngFile = new FileInfo(Path.Combine(pdfFile.DirectoryName, pngFilename));

            try
            {
                // note that this "collection" is a bunch of pages in a pdf, and actually only 1 because we are limiting it to page 1.
                using (var images = new MagickImageCollection())
                {
                    // Add all the pages of the pdf file to the collection
                    images.Read(pdfFile, imsettings);
                    foreach (var image in images)
                    {
                        image.Format = imageOutputFormat;
                        image.Write(pngFile);
                    }
                }
                return pngFile;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        private static string GetFileNameWithoutExtension(FileInfo file)
        {
            return file.Name.Remove(file.Name.IndexOf(file.Extension, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    public class OcrTemplate
    {
        public OcrTemplate()
        {
            this.Regions = new List<OcrRegion>();
        }

        public int OcrTemplateId { get; set; }

        public virtual List<OcrRegion> Regions { get; set; }

        public string DocumentType { get; set; }

        public int CourtId { get; set; }

    }

    public class OcrRegion
    {
        public int TopLeftX { get; }
        public int TopLeftY { get; }
        public int BottomRightX { get; }
        public int BottomRightY { get; }

        public readonly OcrRegionName RegionName;

        public OcrRegion(int topLeftX, int topLeftY, int bottomRightX, int bottomRightY, OcrRegionName regionName)
        {
            TopLeftX = topLeftX;
            TopLeftY = topLeftY;            
            BottomRightX = bottomRightX;
            BottomRightY = bottomRightY;
            RegionName = regionName;
        }
    }

    public enum OcrRegionName
    {
        Attorney,
        CaseNumber,
        Defendant,
        HearingDate,
        Plaintiff        
    }
}
