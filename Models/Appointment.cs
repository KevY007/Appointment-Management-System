using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AMS
{
    public class Appointment
    {
        public int Id { get; set; }

        public Patient Patient { get; set; }

        public Doctor Doctor { get; set; }

        [Required]
        public int TimeSlot { get; set; }

        [Required, DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [DefaultValue(false)]
        public bool Completed { get; set; }

        [Required, MinLength(5)]
        public string Description { get; set; }
    }
}