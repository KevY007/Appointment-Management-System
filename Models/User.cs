using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace AMS
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name cannot be empty!"), MaxLength(100, ErrorMessage = "Max length for name is 100!"), MinLength(3, ErrorMessage = "Name must be at least 3 characters!")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Password cannot be empty!"), MinLength(5, ErrorMessage = "Password must be at least 5 characters!")]
        public string Password { get; set; }

        [Required, DefaultValue(UserType.Patient)]
        public UserType Type { get; set; }
    }

    public class Admin : User
    {
        public Admin()
        {
            Type = UserType.Admin;
        }
    }

    public class Doctor : User
    {
        public Doctor()
        {
            Type = UserType.Doctor;
        }
        public Department Department { get; set; }

        [DefaultValue(true)]
        public bool Available { get; set; }

        [DefaultValue(0)]
        public int Salary { get; set; }
    }

    public class Patient : User
    {
        public Patient()
        {
            Type = UserType.Patient;
        }

        [Required]
        public Gender Gender { get; set; }

        [Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required, MaxLength(100), MinLength(3)]
        public string Address { get; set; }

        [Required]
        public int Phone { get; set; }
    }

    public enum Gender
    {
        Male, Female
    }

    public enum UserType
    {
        Patient, Doctor, Admin
    }
}