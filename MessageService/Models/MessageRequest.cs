using System;
using System.Collections.Generic;
using System.Text;

namespace MessageService.Models
{
    public class MessageRequest
    {
        public MessageIndividual Origin { get; set; }
        public MessageIndividual Target { get; set; }
        public String ChannelId { get; set; }
        public MessageContent Message { get; set; }
        public User Sender { get; set; }
        public User Recipient { get; set; }
    }
}
