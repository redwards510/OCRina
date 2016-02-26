using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneLegal.OCRina;

namespace OCRinaTest
{
    [TestClass]
    public class OCRinaTest
    {
        private OcrTemplate _template;
        [TestInitialize]
        public void TestInitialize()
        {
            _template = new OcrTemplate
            {
                CourtId = 1,
                DocumentType = "Case Management Statement",
                OcrTemplateId = 1                
            };
                         
            var caseNumberRegion = new OcrRegion(1561, 957, 2291, 1158, OcrRegionName.CaseNumber);
            var plaintiffRegion = new OcrRegion(150, 830, 1640, 1010, OcrRegionName.Plaintiff);
            var attorneyRegion = new OcrRegion(150, 200, 1640, 590, OcrRegionName.Attorney);
            var hearingDateRegion = new OcrRegion(150, 1220, 2291, 1350, OcrRegionName.HearingDate);
            _template.Regions.Add(caseNumberRegion);
            _template.Regions.Add(plaintiffRegion);
            _template.Regions.Add(attorneyRegion);
            _template.Regions.Add(hearingDateRegion);
        }

        [TestMethod]
        public void TestExtractTemplateRegions()
        {
            FileInfo pdfFile = new FileInfo("OLPDFS\\43159363.pdf");
            var pngFile = CloudOcrService.ConvertPdfToPng(pdfFile);
            var result = CloudOcrService.ExtractRegionFromPng(pngFile, _template.Regions.First(x => x.RegionName == OcrRegionName.CaseNumber));                
            Assert.AreEqual(result, "");
        }
    }
}
