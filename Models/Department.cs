using System;
using System.Collections.Generic;
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
    }
}