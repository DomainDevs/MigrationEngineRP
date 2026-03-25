using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class MigrationProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public string StepName { get; set; } = string.Empty;
        public MigrationJob? Job { get; set; }
    }
}