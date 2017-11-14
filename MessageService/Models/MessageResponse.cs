using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessageService.Models
{
    public class MessageResponse
    {
        public String ChannelId { get; set; }
        public String ChannelTitle { get; set; }
        public MessageTarget Target { get; set; }
        public MessageCommand Command { get; set; }
        public MessageContent Message { get; set; }
        public User Sender { get; set; }
        public DateTime Time { get; set; }
        public Object Data { get; set; }
    }
}
