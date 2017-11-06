using System;
using System.Collections.Generic;
using System.Text;

namespace MessageService.Models
{
    public class MessageResponse
    {
        public String ChannelId { get; set; }
        public MessageScope Scope { get; set; }
        public MessageContent Message { get; set; }
    }
}
