using MessageService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageService
{
    [AllowAnonymous]
    [Route("msg")]
    public class MessageController : ControllerBase
    {
        private MessageDbContext dc = new MessageDbContext(new DbContextOptions<MessageDbContext>());

        [HttpPost("send")]
        public bool Send([FromBody] MessageRequest request)
        {
            var hub = MessageHubInitializer.ServiceProvider.GetService(typeof(IHubContext<MessageHub>)) as HubContext<MessageHub>;

            var connections = dc.Connections.Where(x => x.CreatedUserId == request.Recipient.Id);

            hub.Clients.All.InvokeAsync("console", DateTime.UtcNow.ToLongTimeString());


            return true;
        }
    }
}
