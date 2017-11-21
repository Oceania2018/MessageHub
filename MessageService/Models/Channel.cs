using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MessageService.Models
{
    public class Channel : BaseModel
    {
        [MaxLength(64)]
        public String Title { get; set; }
        /// <summary>
        /// Max paticipates
        /// </summary>
        public Int16 Limit { get; set; }
        [NotMapped]
        public IEnumerable<User> Participators { get; set; }
    }
}
