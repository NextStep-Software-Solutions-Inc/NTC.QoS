using MediatR;
using OfficeOpenXml;
using System.IO;

namespace NTC.QoS.API.Application.Features.File.Handlers.Commands;

internal sealed class DownloadFileRequestHandler : IRequestHandler<DownloadFileRequest, Stream>
{
    public async Task<Stream> Handle(DownloadFileRequest request, CancellationToken cancellationToken)
    {
        // Create a memory stream to hold the Excel file
        var memoryStream = new MemoryStream();

        // Use EPPlus to generate the Excel file
        using (var package = new ExcelPackage())
        {
            // Add a worksheet
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // Add some sample data
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Description";

            worksheet.Cells[2, 1].Value = request.FileId; // Example data
            worksheet.Cells[2, 2].Value = "Sample File";
            worksheet.Cells[2, 3].Value = "This is a sample description.";

            // Save the Excel package to the memory stream
            package.SaveAs(memoryStream);
        }

        // Reset the stream position to the beginning
        memoryStream.Position = 0;

        return memoryStream;
    }
}
