using Core.Entities;
using Core.Entities.IEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportEntities
{
    public class Report : BaseEntity
    {
        public enum TaskStatusType
        {
            Preparing, Completed
        }

        public string FilePath { get; set; }
        public DateTime ReportDate { get; set; }
        public TaskStatusType TaskStatus { get; set; }




    }
}