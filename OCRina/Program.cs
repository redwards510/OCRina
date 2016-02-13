using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Abbyy;
using Abbyy.CloudOcrSdk;
using TaskStatus = Abbyy.CloudOcrSdk.TaskStatus;
using System.Xml.Serialization;
using ImageMagick;

namespace OCRina
{
    class Program
    {        

        static void Main(string[] args)
        {
            var pdfFile = new FileInfo(args[2]);
            if (!pdfFile.Exists)
            {
                Console.WriteLine("File not found.");
                return;
            }
            var pngFile = ConvertPdfToPng(pdfFile);           
            var outputXmlFile = CallOcrWebService(pngFile);
            var result = GetOcrTextFromResultXml(outputXmlFile);
            Console.WriteLine("Extracted Value: " + result);                 

        }

        private static string CallOcrWebService(FileInfo pngFile)
        {
            var restClient = new RestServiceClient
            {
                Proxy = {Credentials = CredentialCache.DefaultCredentials},
                ApplicationId = "OLopalus",
                Password = "ko1r3mHnGGMI3YdrSUCZc0MJ"
            };
            var textFieldProcessingSettings = new TextFieldProcessingSettings
            {
                CustomOptions = "region=1561,957,2291,1158",
                Language = "english"
            };

            var outputXmlFile = Path.Combine(pngFile.DirectoryName, GetFileNameWithoutExtension(pngFile) + ".xml");

            // call the REST service
            var task = restClient.ProcessTextField(pngFile.ToString(), textFieldProcessingSettings);
            System.Threading.Thread.Sleep(4000);
            task = restClient.GetTaskStatus(task.Id);
            Console.WriteLine(String.Format("Task status: {0}", task.Status));
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
                Console.WriteLine(String.Format("Task status: {0}", task.Status));
            }
            if (task.Status == TaskStatus.Completed)
            {
                Console.WriteLine("Processing completed.");
                restClient.DownloadResult(task, outputXmlFile);
                Console.WriteLine("Download completed.");
            }
            else
            {
                Console.WriteLine("Error while processing the task");
                return outputXmlFile;
            }
            return outputXmlFile;
        }

        private static string GetOcrTextFromResultXml(string outputXmlFile)
        {
            // Parse Xml output file for extracted value
            XElement x = XElement.Load(outputXmlFile);
            XNamespace xsi = "@link";
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

        public static string GetFileNameWithoutExtension(FileInfo file)
        {
            return file.Name.Remove(file.Name.IndexOf(file.Extension, StringComparison.CurrentCultureIgnoreCase));
        }
    }    
}
