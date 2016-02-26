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
using OneLegal.OCRina;

namespace OCRina
{
    class Program
    {        

        static void Main(string[] args)
        {
            var template = new OcrTemplate
            {
                CourtId = 1,
                DocumentType = "Case Management Statement",
                OcrTemplateId = 1
            };

            var caseNumberRegion = new OcrRegion(1561, 957, 2291, 1158, OcrRegionName.CaseNumber);
            var plaintiffRegion = new OcrRegion(150, 830, 1640, 1010, OcrRegionName.Plaintiff);
            var attorneyRegion = new OcrRegion(150, 200, 1640, 590, OcrRegionName.Attorney);
            var hearingDateRegion = new OcrRegion(150, 1220, 2291, 1350, OcrRegionName.HearingDate);
            template.Regions.Add(caseNumberRegion);
            template.Regions.Add(plaintiffRegion);
            template.Regions.Add(attorneyRegion);
            template.Regions.Add(hearingDateRegion);

            FileInfo pdfFile = new FileInfo("OLPDFS\\43159219.pdf");


            var pngFile = CloudOcrService.ConvertPdfToPng(pdfFile);

            var results = CloudOcrService.ExtractTemplateFromPdfAsync(pdfFile, template);

            foreach (var str in results.Result)
            {
                
            }
            Console.WriteLine(results.Result.Select(s => s));
            foreach (var region in template.Regions)
            {
                Console.WriteLine(region.RegionName.ToString() + ": " + CloudOcrService.ExtractRegionFromPng(pngFile, region));
            }
            Console.ReadLine();
        }

 
    }    
}
