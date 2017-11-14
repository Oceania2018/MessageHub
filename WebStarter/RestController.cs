using MessageService;
using MessageService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebStarter
{
    [AllowAnonymous]
    [Route("rest")]
    public class RestController : ControllerBase
    {
        private MessageDbContext dc = new MessageDbContext(new DbContextOptions<MessageDbContext>());

        /// <summary>
        /// Send message to recipient
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("send")]
        public bool Send([FromBody] MessageRequest request)
        {
            var hub = MessageHubInitializer.ServiceProvider.GetService(typeof(IHubContext<MessageHub>)) as HubContext<MessageHub>;

            var connections = dc.Connections.Where(x => x.CreatedUserId == request.Recipient.Id).Select(x => x.Id).ToList();
            var response = new MessageResponse
            {
                ChannelId = Constants.PUBLIC_CHANNEL_ID,
                Target = MessageTarget.System,
                Sender = request.Sender,
                Message = new MessageContent { Text = request.Message.Text }
            };

            connections.ForEach(connection => {
                hub.Client(connection).InvokeAsync("received", response);
            });
            
            return true;
        }
    }
}
