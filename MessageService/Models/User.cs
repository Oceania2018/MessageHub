using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MessageService.Models
{
    public class User : BaseModel
    {
        public String Name { get; set; }
        public String Email { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String FullName { get { return $"{FirstName} {LastName}"; } }
    }
}
