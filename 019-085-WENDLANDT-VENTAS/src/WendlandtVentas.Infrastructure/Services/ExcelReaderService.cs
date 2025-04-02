using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.Pdf;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models.OrderViewModels;
using WendlandtVentas.Core.Specifications.ClientSpecifications;

namespace WendlandtVentas.Infrastructure.Services
{
    public class ExcelReaderService : IExcelReadService
    {
        private readonly IAsyncRepository _asyncRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExcelReaderService(IAsyncRepository asyncRepository,
            UserManager<ApplicationUser> userManager)
        {
            _asyncRepository = asyncRepository;
            _userManager = userManager;
        }

        public async Task<List<Client>> ExtractData(XLWorkbook spreadSheetDocument)
        {
            try
            {
                var nonEmptyDataRows = spreadSheetDocument.Worksheet(1).RowsUsed();
                List<string> messages = new List<string>();

                var listGeneralData = new List<Client>();
                foreach (var rows in nonEmptyDataRows.Skip(2))
                {
                    //Llenado de datos generales 

                    var name = rows.Cell("A").Value.ToString();
                    var clasification = rows.Cell("B").Value.ToString();
                    var chanel = rows.Cell("C").Value.ToString();
                    var state = rows.Cell("D").Value.ToString();
                    var paymentMethod = rows.Cell("E").Value.ToString();
                    var rfc = rows.Cell("F").Value.ToString();
                    var seller = rows.Cell("G").Value.ToString();
                    var address = rows.Cell("H").Value.ToString();
                    var city = rows.Cell("I").Value.ToString();
                    var client = new Client(name);
                    if (!string.IsNullOrEmpty(clasification))
                        client.Classification = clasification.ToLower().Trim().Equals("oro") ? Classification.Gold : (clasification.ToLower().Trim().Equals("plata") ? Core.Entities.Enums.Classification.Silver : Core.Entities.Enums.Classification.Bronze);
                    if (!string.IsNullOrEmpty(chanel))
                        client.Channel = chanel.ToLower().Trim().Equals("venta directa") ? Channel.DirectSale : (chanel.ToLower().Trim().Equals("mayorista") ? Core.Entities.Enums.Channel.Wholesaler : Core.Entities.Enums.Channel.Distributor);

                    var stateRes = await _asyncRepository.GetAsync(new StateByNameSpecification(state));
                    if (stateRes != null)
                    {
                        client.StateId = stateRes.Id;
                    }
                    if (!string.IsNullOrEmpty(paymentMethod))
                        client.PayType = paymentMethod.ToLower().Trim().Equals("contado") ? Core.Entities.Enums.PayType.Cash : (paymentMethod.ToLower().Trim().Equals("especial") ? Core.Entities.Enums.PayType.Special : Core.Entities.Enums.PayType.Credit);
                    client.RFC = rfc;
                    var sellerRes = _userManager.Users.Where(c => c.Name.ToLower().Equals(seller)).ToList();
                    if (sellerRes.Any())
                    {
                        client.SellerId = sellerRes.First().Id;
                    }

                    //client.Address = address;
                    client.City = city;
                    listGeneralData.Add(client);
                }

                return listGeneralData;
            }
            catch (Exception err)
            {
                throw err;
            }
        }

        public async Task<string> FillData(string path, OrderDetailsViewModel model)
        {
            var excelFile = "";
            if (model.TypeEnum == OrderType.Invoice)
                excelFile = "Facturación Wendlandt.xlsx";
            else
                excelFile = "Remisión Wendlandt.xlsx";

            var guidExcel = $"{Guid.NewGuid()}.xlsx";
            var guidPdf = $"{Guid.NewGuid()}.pdf";
            var pdfCreated = guidPdf;

            excelFile = Path.Combine(path, $"wwwroot/resources/{excelFile}");
            guidExcel = Path.Combine(path, $"wwwroot/resources/orders/{guidExcel}");
            guidPdf = Path.Combine(path, $"wwwroot/resources/orders/{guidPdf}");


            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                FileStream excelStream = new FileStream(excelFile, FileMode.Open, FileAccess.Read);
                IApplication application = excelEngine.Excel;
                IWorkbook workbook = application.Workbooks.Open(excelStream);
                IWorksheet worksheet = workbook.Worksheets[0];

                if (model.TypeEnum == OrderType.Invoice)
                {
                    worksheet.Range["H3"].Text = model.InvoiceCode;
                    worksheet.Range["R3"].Text = model.InvoiceCode;

                    worksheet.Range["H5"].Text = model.RemissionCode;
                    worksheet.Range["R5"].Text = model.RemissionCode;

                    worksheet.Range["I27"].Text = model.SubTotal;
                    worksheet.Range["I28"].Text = model.IVA;
                    worksheet.Range["I29"].Text = model.IEPS;
                    worksheet.Range["I30"].Text = model.Total;

                    worksheet.Range["S27"].Text = model.SubTotal;
                    worksheet.Range["S28"].Text = model.IVA;
                    worksheet.Range["S29"].Text = model.IEPS;
                    worksheet.Range["S30"].Text = model.Total;

                    worksheet.Range["H7"].Text = model.CreateDate.Day.ToString();
                    worksheet.Range["I7"].Text = model.CreateDate.Month.ToString();
                    worksheet.Range["J7"].Text = model.CreateDate.Year.ToString();
                    worksheet.Range["B9"].Text += model.Client.Name;
                    worksheet.Range["B10"].Text += model.Address.AddressLocation;
                    worksheet.Range["B11"].Text += model.Client.City;
                    worksheet.Range["H11"].Text += model.Client.RFC;

                    worksheet.Range["D27"].Text += string.IsNullOrEmpty(model.CollectionComment)
                        ? string.Empty
                        : model.CollectionComment.Length > 50
                        ? $"{model.CollectionComment.Substring(0, 50)}..."
                        : model.CollectionComment;
                    worksheet.Range["D29"].Text += string.IsNullOrEmpty(model.Comment)
                        ? string.Empty
                        : model.Comment.Length > 50
                        ? $"{model.Comment.Substring(0, 50)}..."
                        : model.Comment;
                    worksheet.Range["D31"].Text += $"{model.Weight} Kg.";

                    worksheet.Range["R7"].Text = model.CreateDate.Day.ToString();
                    worksheet.Range["S7"].Text = model.CreateDate.Month.ToString();
                    worksheet.Range["T7"].Text = model.CreateDate.Year.ToString();
                    worksheet.Range["L9"].Text += model.Client.Name;
                    worksheet.Range["L10"].Text += model.Address.AddressLocation;
                    worksheet.Range["L11"].Text += model.Client.City;
                    worksheet.Range["R11"].Text += model.Client.RFC;

                    worksheet.Range["N27"].Text += string.IsNullOrEmpty(model.CollectionComment) 
                        ? string.Empty 
                        : model.CollectionComment.Length > 50
                        ? $"{model.CollectionComment.Substring(0, 50)}..."
                        : model.CollectionComment;
                    worksheet.Range["N29"].Text += string.IsNullOrEmpty(model.Comment) 
                        ? string.Empty 
                        : model.Comment.Length > 50
                        ? $"{model.Comment.Substring(0, 50)}..."
                        : model.Comment;
                    worksheet.Range["N31"].Text += $"{model.Weight} Kg.";
                }
                else
                {
                    worksheet.Range["E3"].Text = model.RemissionCode;
                    worksheet.Range["O3"].Text = model.RemissionCode;

                    worksheet.Range["I30"].Text = model.TypeEnum == OrderType.Remission ? model.Total : model.SubTotal;
                    worksheet.Range["S30"].Text = model.TypeEnum == OrderType.Remission ? model.Total : model.SubTotal;

                    worksheet.Range["E5"].Text = model.CreateDate.Day.ToString();
                    worksheet.Range["G5"].Text = model.CreateDate.Month.ToString();
                    worksheet.Range["I5"].Text = model.CreateDate.Year.ToString();
                    worksheet.Range["E7"].Text += model.Client.Name;
                    worksheet.Range["E9"].Text += model.Address.AddressLocation;
                    worksheet.Range["E12"].Text += model.Client.City;
                    worksheet.Range["H12"].Text += model.Client.RFC;

                    worksheet.Range["D28"].Text += string.IsNullOrEmpty(model.CollectionComment)
                        ? string.Empty
                        : model.CollectionComment.Length > 50
                        ? $"{model.CollectionComment.Substring(0, 50)}..."
                        : model.CollectionComment;
                    worksheet.Range["D30"].Text += string.IsNullOrEmpty(model.Comment)
                        ? string.Empty
                        : model.Comment.Length > 50
                        ? $"{model.Comment.Substring(0, 50)}..."
                        : model.Comment;
                    worksheet.Range["D32"].Text += $"{model.Weight} Kg.";

                    worksheet.Range["O5"].Text = model.CreateDate.Day.ToString();
                    worksheet.Range["Q5"].Text = model.CreateDate.Month.ToString();
                    worksheet.Range["S5"].Text = model.CreateDate.Year.ToString();
                    worksheet.Range["O7"].Text += model.Client.Name;
                    worksheet.Range["O9"].Text += model.Address.AddressLocation;
                    worksheet.Range["O12"].Text += model.Client.City;
                    worksheet.Range["R12"].Text += model.Client.RFC;

                    worksheet.Range["N28"].Text += string.IsNullOrEmpty(model.CollectionComment)
                        ? string.Empty
                        : model.CollectionComment.Length > 50
                        ? $"{model.CollectionComment.Substring(0, 50)}..."
                        : model.CollectionComment;
                    worksheet.Range["N30"].Text += string.IsNullOrEmpty(model.Comment)
                        ? string.Empty
                        : model.Comment.Length > 50
                        ? $"{model.Comment.Substring(0, 50)}..."
                        : model.Comment;
                    worksheet.Range["N32"].Text += $"{model.Weight} Kg.";
                }

                int i = model.TypeEnum == OrderType.Remission ? 15 : 14;
                foreach (var product in model.Products)
                {
                    if (i < 30)
                    {
                        worksheet.Range[$"B{i}"].Text = product.Quantity.ToString();
                        worksheet.Range[$"C{i}"].Text = product.PresentationLiters;
                        worksheet.Range[$"G{i}"].Text = product.Price.ToString("C");
                        worksheet.Range[$"I{i}"].Text = product.Total.ToString("C");

                        worksheet.Range[$"L{i}"].Text = product.Quantity.ToString();
                        worksheet.Range[$"M{i}"].Text = product.PresentationLiters;
                        worksheet.Range[$"Q{i}"].Text = product.Price.ToString("C");
                        worksheet.Range[$"S{i}"].Text = product.Total.ToString("C");
                    }
                    i++;
                }

                //Saving the workbook as stream
                FileStream stream = new FileStream(guidExcel, FileMode.Create, FileAccess.ReadWrite);
                workbook.SaveAs(stream);
                await excelStream.DisposeAsync();
                await stream.DisposeAsync();
            }

            if (File.Exists(guidExcel))
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    FileStream excelStream = new FileStream(guidExcel, FileMode.Open, FileAccess.Read);
                    excelStream.Position = 0;
                    IWorkbook workbook = application.Workbooks.Open(excelStream);

                    //Initialize XlsIO renderer.
                    XlsIORenderer renderer = new XlsIORenderer();

                    //Convert Excel document into PDF document 
                    PdfDocument pdfDocument = renderer.ConvertToPDF(workbook);

                    Stream stream = new FileStream(guidPdf, FileMode.Create, FileAccess.ReadWrite);
                    pdfDocument.Save(stream);

                    await excelStream.DisposeAsync();
                    File.Delete(guidExcel);
                    await stream.DisposeAsync();
                }
            else
            {
                return null;
            }
            return guidPdf;
        }
    }
}