using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MessageService.Models
{
    public class ChannelUser : BaseModel
    {
        [StringLength(36)]
        public String ChannelId { get; set; }
        [StringLength(36)]
        public String UserId { get; set; }
    }
}
