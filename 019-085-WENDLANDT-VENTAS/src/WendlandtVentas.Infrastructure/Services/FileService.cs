using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Hosting;
using OpenXmlPowerTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using Syncfusion.Pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WendlandtVentas.Core.Interfaces;

namespace WendlandtVentas.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly IHostEnvironment _environment;

        public FileService(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public string DownloadPhoto(string url)
        {
            var uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/media/images");
            var thumbs = Path.Combine(_environment.ContentRootPath, "wwwroot/media/thumbs");

            var fileName = $"{Guid.NewGuid()}.jpg";

            var imageRoute = Path.Combine(uploads, fileName);
            using (var client = new WebClient())
            {
                client.DownloadFile(new Uri(url), imageRoute);
            }

            using (var originalImage = Image.Load(imageRoute))
            {
                var maxWidth = 800;
                if (originalImage.Width < maxWidth)
                    maxWidth = originalImage.Width;

                var imageResizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.Min,
                    Size = new Size(maxWidth),
                };
                var thumbResizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.Min,
                    Size = new Size(100),
                };

                var image = originalImage.Clone(x => x.Resize(imageResizeOptions).BackgroundColor(Rgba32.White));
                image.Save(Path.Combine(uploads, fileName));
                originalImage.Mutate(x => x.Resize(thumbResizeOptions).BackgroundColor(Rgba32.White));
                originalImage.Save(Path.Combine(thumbs, fileName));
            }

            return fileName;
        }

        public string SavePhoto(byte[] imageData, string folder, int maxWidth)
        {
            if (imageData.Length <= 0)
                return null;

            var uploads = Path.Combine(_environment.ContentRootPath, $"media/{folder}");
            var thumbs = Path.Combine(_environment.ContentRootPath, "media/thumbs");

            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            if (!Directory.Exists(thumbs))
                Directory.CreateDirectory(thumbs);

            var extension = ".jpg";
            var name = Guid.NewGuid() + extension;

            using (var originalImage = Image.Load(imageData))
            {
                if (originalImage.Width < maxWidth)
                    maxWidth = originalImage.Width;

                if (originalImage.Height < maxWidth)
                    maxWidth = originalImage.Height;

                var imageResizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.Min,
                    Size = new Size(maxWidth),
                };

                var thumbResizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.Min,
                    Size = new Size(100),
                };

                var image = originalImage.Clone(x => x.Resize(imageResizeOptions).BackgroundColor(Rgba32.White));
                image.Save(Path.Combine(uploads, name));
                originalImage.Mutate(x => x.Resize(thumbResizeOptions).BackgroundColor(Rgba32.White));
                originalImage.Save(Path.Combine(thumbs, name));
            }

            return name;
        }

        public void DeletePhotos(string[] filePaths)
        {
            var uploads = Path.Combine(_environment.ContentRootPath, "media/images");
            var uploadsThumbs = Path.Combine(_environment.ContentRootPath, "media/thumbs");
            foreach (var i in filePaths)
            {
                if (File.Exists(Path.Combine(uploads, i)))
                {
                    File.Delete(Path.Combine(uploads, i));
                    File.Delete(Path.Combine(uploadsThumbs, i));
                }
            }
        }

        public void DeleteFile(string fileName, string mediaFolder)
        {
            var uploads = Path.Combine(_environment.ContentRootPath, $"media/{mediaFolder}");
            File.Delete(Path.Combine(uploads, fileName));
        }

        public async Task<string> SavePhotoInFolder(byte[] photoData, string mediaFolder)
        {
            var uploads = Path.Combine(_environment.ContentRootPath, $"media/{mediaFolder}");

            var imageFormat = Image.DetectFormat(photoData);

            if (imageFormat == null)
                return null;

            var fileName = $"{Guid.NewGuid()}.{imageFormat.FileExtensions.First()}";

            using (var fileStream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
            {
                await fileStream.WriteAsync(photoData, 0, photoData.Length);
            }

            return fileName;
        }

        public string ReplaceWords(Dictionary<string, string> Words, string wordDocument)
        {
            try
            {

                var guid = Guid.NewGuid().ToString() + ".docx";
                var guidpdf = Guid.NewGuid().ToString() + ".pdf";

                guid = Path.Combine(_environment.ContentRootPath, $"wwwroot/resources/{guid}");
                wordDocument = Path.Combine(_environment.ContentRootPath, $"wwwroot/resources/{wordDocument}");


                using (var mainDoc = WordprocessingDocument.Open(@wordDocument, false))
                using (var resultDoc = WordprocessingDocument.Create(@guid,
                  WordprocessingDocumentType.Document))
                {
                    // copy parts from source document to new document
                    foreach (var part in mainDoc.Parts)
                        resultDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                    // perform replacements in resultDoc.MainDocumentPart
                    // ... 
                    resultDoc.Dispose();
                }

                using (WordprocessingDocument resultDoc = WordprocessingDocument.Open(guid, true))
                {
                    foreach (var palabra in Words)
                    {
                        try
                        {
                            TextReplacer.SearchAndReplace(wordDoc: resultDoc, search: "%" + palabra.Key + "%", replace: palabra.Value, matchCase: false);
                        }
                        catch (Exception err)
                        {
                            TextReplacer.SearchAndReplace(wordDoc: resultDoc, search: "%" + palabra.Key + "%", replace: "N/D", matchCase: false);
                        }
                    }
                    //WordDocument wd = new WordDocument(wordDocument, FormatType.Docx);
                    //DocToPDFConverter converter = new DocToPDFConverter();
                    //PdfDocument pdfDocument = converter.ConvertToPDF(wd);
                    //pdfDocument.Save(guidpdf);
                    //pdfDocument.Close(true);
                    //wd.Close();

                }
                return guidpdf;
            }
            catch (Exception err)
            {
                //_logger.LogError(err, "Error generando el documento");
                return "";
            }
        }

        public string GetTable(Dictionary<string, string> Words, string wordDocument)
        {
            try
            {
                DataTable table = new DataTable();
                var guid = Guid.NewGuid().ToString() + ".docx";
                var guidpdf = Guid.NewGuid().ToString() + ".pdf";

                guid = Path.Combine(_environment.ContentRootPath, $"wwwroot/resources/{guid}");
                wordDocument = Path.Combine(_environment.ContentRootPath, $"wwwroot/resources/{wordDocument}");


                using (var mainDoc = WordprocessingDocument.Open(@wordDocument, false))
                using (var resultDoc = WordprocessingDocument.Create(@guid,
                  WordprocessingDocumentType.Document))
                {
                    // copy parts from source document to new document
                    foreach (var part in mainDoc.Parts)
                        resultDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                    // perform replacements in resultDoc.MainDocumentPart
                    // ... 
                    resultDoc.Dispose();
                }

                using (WordprocessingDocument resultDoc = WordprocessingDocument.Open(guid, true))
                {
                    foreach (var palabra in Words)
                    {
                        try
                        {
                            TextReplacer.SearchAndReplace(wordDoc: resultDoc, search: "%" + palabra.Key + "%", replace: palabra.Value, matchCase: false);
                        }
                        catch (Exception err)
                        {
                            TextReplacer.SearchAndReplace(wordDoc: resultDoc, search: "%" + palabra.Key + "%", replace: "N/D", matchCase: false);
                        }
                    }                    
                }
                return guidpdf;
            }
            catch (Exception err)
            {
                return "";
            }
        }

    }
}