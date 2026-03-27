using System.Collections.Generic;

namespace Infrastructure.DTOs
{
    public class MigrationJobReportDto
    {
        public string JobId { get; set; } = string.Empty;
        public List<MigrationStepReportDto> Steps { get; set; } = new();
    }
}