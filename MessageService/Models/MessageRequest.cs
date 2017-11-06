using System;
using System.Collections.Generic;
using System.Text;

namespace MessageService.Models
{
    public class MessageRequest
    {
        public String ChannelId { get; set; }
        public MessageContent Message { get; set; }
    }
}
