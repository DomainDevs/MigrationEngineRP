using System;

namespace Infrastructure.DTOs
{
    public class MigrationStepReportDto
    {
        public int StepId { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long RowsProcessed { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double DurationSeconds { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}