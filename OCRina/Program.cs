using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
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


            // CONVERT PDF TO IMAGE SO WE CAN UPLOAD IT
            MagickReadSettings imsettings = new MagickReadSettings();
            // Settings the density to 300 dpi will create an image with a better quality
            imsettings.Density = new PointD(300, 300);
            imsettings.FrameIndex = 0; // first page
            imsettings.FrameCount = 1; // number of pages
            imsettings.Format = MagickFormat.Pdf;            
            List<string> imageFiles = new List<string>();

            // note that this "collection" is a bunch of pages in a pdf, and actually only 1 because we are limiting it to page 1.
            using (MagickImageCollection images = new MagickImageCollection())
            {
                // Add all the pages of the pdf file to the collection
                images.Read("OLPDFS\\43159162.pdf", imsettings);

                int page = 1;
                foreach (MagickImage image in images)
                {                                                                                
                    image.Format = MagickFormat.Png;                    
                    string fileName = "OLPDFS\\43159162-p" + page + ".png";
                    image.Write(fileName);
                    imageFiles.Add(fileName);
                    System.Console.WriteLine("PDF converted to image " + fileName);
                    page++;
                }
            }
            Console.WriteLine("Finished converting PDFS. Total made: " + imageFiles.Count);

            // set up REST client
            var restClient = new RestServiceClient();
            restClient.Proxy.Credentials = CredentialCache.DefaultCredentials;
            restClient.ApplicationId = "OLopalus";
            restClient.Password = "ko1r3mHnGGMI3YdrSUCZc0MJ";
            TextFieldProcessingSettings settings = new TextFieldProcessingSettings();            
            //var sourceFile = @"C:\\wkspaces\\ABBYcloud\\Picture_samples\\English\\\Scanned_documents\Picture_010.tif";
            var sourceFile =  Path.Combine(Environment.CurrentDirectory, imageFiles.First()); // fix this to process multiple?
            var outputFilePath = Environment.CurrentDirectory + "\\output.xml";

            settings.CustomOptions = "region=1561,957,2291,1158";
            settings.Language = "english";


            // call the REST service
            var task = restClient.ProcessTextField(sourceFile, settings);
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
                restClient.DownloadResult(task, outputFilePath);
                Console.WriteLine("Download completed.");
            }
            else
            {
                Console.WriteLine("Error while processing the task");
                return;
            }

            XElement x = XElement.Load(outputFilePath);
            XNamespace xsi = "@link";
            var result = x.Element(xsi+"field").Element(xsi+"value").Value;
            Console.WriteLine("Extracted Value: " + result);                 

        } 
    }

    [Serializable]
    public class ABBYYOutputXml
    {
        [XmlElement(ElementName = "document", Namespace = "@link")]
        public virtual abbydocument document { get; set; }
    }

    public class abbydocument
    {
        [XmlElement(ElementName = "field", Namespace = "@link")]
        public virtual abbyfield field { get; set; }
    }

    public class abbyfield        
    {
        [XmlElement(ElementName = "value")]
        public virtual abbyvalue value { get; set; }

        [XmlAttribute(AttributeName = "left")]
        public virtual string left { get; set; }
        [XmlAttribute(AttributeName = "top")]
        public virtual string top { get; set; }
        [XmlAttribute(AttributeName = "right")]
        public virtual string right { get; set; }
        [XmlAttribute(AttributeName = "bottom")]
        public virtual string bottom { get; set; }

    }

    public class abbyvalue
    {
        public string value { get; set; }
    }

}
