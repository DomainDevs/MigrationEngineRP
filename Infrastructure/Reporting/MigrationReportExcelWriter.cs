using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using Infrastructure.DTOs;

namespace Infrastructure.Reporting
{
    public class MigrationReportExcelWriter
    {
        private readonly string _outputFolder;
        public string OutputFolder => _outputFolder;

        public MigrationReportExcelWriter(string outputFolder)
        {
            if (string.IsNullOrWhiteSpace(outputFolder))
                throw new ArgumentException("Output folder path is required.", nameof(outputFolder));

            _outputFolder = outputFolder;
            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);
        }

        public string WriteJobReport(MigrationJobReportDto job, string fullPath)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (string.IsNullOrWhiteSpace(fullPath)) throw new ArgumentException("Full path is required.", nameof(fullPath));

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Migration Report");

            ws.Cell(1, 1).Value = "StepId";
            ws.Cell(1, 2).Value = "StepName";
            ws.Cell(1, 3).Value = "Status";
            ws.Cell(1, 4).Value = "RowsProcessed";
            ws.Cell(1, 5).Value = "StartTime";
            ws.Cell(1, 6).Value = "EndTime";
            ws.Cell(1, 7).Value = "DurationSeconds";
            ws.Cell(1, 8).Value = "Message";
            ws.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var step in job.Steps)
            {
                ws.Cell(row, 1).Value = step.StepId;
                ws.Cell(row, 2).Value = step.StepName;
                ws.Cell(row, 3).Value = step.Status;
                ws.Cell(row, 4).Value = step.RowsProcessed;
                ws.Cell(row, 5).Value = step.StartTime;
                ws.Cell(row, 6).Value = step.EndTime;
                ws.Cell(row, 7).Value = step.DurationSeconds;
                ws.Cell(row, 8).Value = step.Message;

                switch (step.Status.ToLower())
                {
                    case "success": ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightBlue; break;
                    case "warning": ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow; break;
                    case "failed": ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightCoral; break;
                }
                row++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(fullPath);
            return fullPath;
        }

        public string WriteAllJobsReport(List<MigrationJobReportDto> jobs)
        {
            if (jobs == null || jobs.Count == 0)
                throw new ArgumentException("No jobs provided.", nameof(jobs));

            var fileName = Path.Combine(_outputFolder, $"MigrationReport_AllJobs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            using var wb = new XLWorkbook();

            var summaryWs = wb.Worksheets.Add("Summary");
            summaryWs.Cell(1, 1).Value = "JobId";
            summaryWs.Cell(1, 2).Value = "TotalSteps";
            summaryWs.Cell(1, 3).Value = "SuccessCount";
            summaryWs.Cell(1, 4).Value = "WarningCount";
            summaryWs.Cell(1, 5).Value = "FailedCount";
            summaryWs.Cell(1, 6).Value = "TotalDurationSeconds";
            summaryWs.Row(1).Style.Font.Bold = true;

            int summaryRow = 2;

            foreach (var job in jobs)
            {
                var ws = wb.Worksheets.Add($"Job_{job.JobId}");
                ws.Cell(1, 1).Value = "StepId";
                ws.Cell(1, 2).Value = "StepName";
                ws.Cell(1, 3).Value = "Status";
                ws.Cell(1, 4).Value = "RowsProcessed";
                ws.Cell(1, 5).Value = "StartTime";
                ws.Cell(1, 6).Value = "EndTime";
                ws.Cell(1, 7).Value = "DurationSeconds";
                ws.Cell(1, 8).Value = "Message";
                ws.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (var step in job.Steps)
                {
                    ws.Cell(row, 1).Value = step.StepId;
                    ws.Cell(row, 2).Value = step.StepName;
                    ws.Cell(row, 3).Value = step.Status;
                    ws.Cell(row, 4).Value = step.RowsProcessed;
                    ws.Cell(row, 5).Value = step.StartTime;
                    ws.Cell(row, 6).Value = step.EndTime;
                    ws.Cell(row, 7).Value = step.DurationSeconds;
                    ws.Cell(row, 8).Value = step.Message;

                    switch (step.Status.ToLower())
                    {
                        case "success": ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightGreen; break;
                        case "warning": ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow; break;
                        case "failed": ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightCoral; break;
                    }
                    row++;
                }
                ws.Columns().AdjustToContents();

                summaryWs.Cell(summaryRow, 1).Value = job.JobId;
                summaryWs.Cell(summaryRow, 2).Value = job.Steps.Count;
                summaryWs.Cell(summaryRow, 3).Value = job.Steps.Count(s => s.Status.Equals("Success", StringComparison.OrdinalIgnoreCase));
                summaryWs.Cell(summaryRow, 4).Value = job.Steps.Count(s => s.Status.Equals("Warning", StringComparison.OrdinalIgnoreCase));
                summaryWs.Cell(summaryRow, 5).Value = job.Steps.Count(s => s.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
                summaryWs.Cell(summaryRow, 6).Value = job.Steps.Sum(s => s.DurationSeconds);
                summaryRow++;
            }

            summaryWs.Columns().AdjustToContents();
            wb.SaveAs(fileName);
            return fileName;
        }
    }
}