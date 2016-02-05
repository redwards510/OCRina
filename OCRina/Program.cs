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
            var restClient = new RestServiceClient();
            restClient.Proxy.Credentials = CredentialCache.DefaultCredentials;            
            restClient.ApplicationId = "OLopalus";            
            restClient.Password = "ko1r3mHnGGMI3YdrSUCZc0MJ";
            TextFieldProcessingSettings settings = new TextFieldProcessingSettings();
            //settings.
            var sourceFile = @"C:\\wkspaces\\ABBYcloud\\Picture_samples\\English\\\Scanned_documents\Picture_010.tif";
            var outputFilePath = @"C:\\wkspaces\\ABBYcloud\\output.xml";

            
            //settings.CustomOptions = "region=570,710,970,785";
            //settings.Language = "english";
            
            //// call the REST service
            //var task = restClient.ProcessTextField(sourceFile, settings);
            //System.Threading.Thread.Sleep(5000);
            //task = restClient.GetTaskStatus(task.Id);
            //Console.WriteLine(String.Format("Task status: {0}", task.Status));
            //while (task.IsTaskActive())
            //{
            //    // Note: it's recommended that your application waits
            //    // at least 2 seconds before making the first getTaskStatus request
            //    // and also between such requests for the same task.
            //    // Making requests more often will not improve your application performance.
            //    // Note: if your application queues several files and waits for them
            //    // it's recommended that you use listFinishedTasks instead (which is described
            //    // at http://ocrsdk.com/documentation/apireference/listFinishedTasks/).
            //    System.Threading.Thread.Sleep(5000);
            //    task = restClient.GetTaskStatus(task.Id);
            //    Console.WriteLine(String.Format("Task status: {0}", task.Status));
            //}
            //if (task.Status == TaskStatus.Completed)
            //{
            //    Console.WriteLine("Processing completed.");
            //    restClient.DownloadResult(task, outputFilePath);                
            //    Console.WriteLine("Download completed.");
            //}
            //else
            //{
            //    Console.WriteLine("Error while processing the task");
            //    return;                
            //}

            XElement x = XElement.Load(outputFilePath);
            XNamespace xsi = "@link";
            var result = x.Element(xsi+"field").Element(xsi+"value").Value;
            Console.WriteLine(result);

            var result2 = x.Descendants("value").FirstOrDefault();

            // CONVERT PDF TO IMAGE SO WE CAN UPLOAD IT
            MagickReadSettings imsettings = new MagickReadSettings();
            // Settings the density to 300 dpi will create an image with a better quality
            imsettings.Density = new PointD(300, 300);

            using (MagickImageCollection images = new MagickImageCollection())
            {
                // Add all the pages of the pdf file to the collection
                images.Read("Snakeware.pdf", imsettings);

                int page = 1;
                foreach (MagickImage image in images)
                {
                    // Write page to file that contains the page number
                    image.Write("Snakeware.Page" + page + ".png");
                    // Writing to a specific format works the same as for a single image
                    image.Format = MagickFormat.Ptif;
                    image.Write("Snakeware.Page" + page + ".tif");
                    page++;
                }
            }







            //XmlSerializer serializer = new XmlSerializer(typeof(abbydocument));
            //StreamReader reader = new StreamReader(outputFilePath);
            //var cereal = (abbydocument) serializer.Deserialize(reader);
            //reader.Close();

            //Console.WriteLine(cereal.field.value);            


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
