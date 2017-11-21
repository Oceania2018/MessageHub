using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessageService.Models
{
    public class MessageResponse
    {
        public Channel Channel { get; set; }
        public MessageIndividual Origin { get; set; }
        public MessageIndividual Target { get; set; }
        public MessageCommand Command { get; set; }
        public MessageContent Message { get; set; }
        public User Sender { get; set; }
        public DateTime Time { get; set; }
        public Object Data { get; set; }
    }
}
