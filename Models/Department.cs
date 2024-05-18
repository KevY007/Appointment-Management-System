using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AMS
{
    public class Department
    {
        public int Id { get; set; }

        [Required, MinLength(5)]
        public string Name { get; set; }

        [Required, DisplayName("Charges / Appointment"), DefaultValue(1000)]
        public int AppointmentCost { get; set; }


        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }
    }
}