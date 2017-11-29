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
        private HubContext<MessageHub> hub = MessageHubInitializer.ServiceProvider.GetService(typeof(IHubContext<MessageHub>)) as HubContext<MessageHub>;

        /// <summary>
        /// Send message to recipient
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("send")]
        public bool Send([FromBody] MessageRequest request)
        {
            if (String.IsNullOrEmpty(request.ChannelId))
            {
                request.ChannelId = Constants.PUBLIC_CHANNEL_ID;
            }

            var connections = dc.Connections.Where(x => x.CreatedUserId == request.Recipient.Id).Select(x => x.Id).ToList();
            var response = new MessageResponse
            {
                Channel = dc.Channels.Find(request.ChannelId),
                Origin = request.Origin,
                Target = request.Target,
                Sender = request.Sender,
                Message = new MessageContent { Text = request.Message.Text }
            };

            connections.ForEach(connection =>
            {
                hub.Client(connection).InvokeAsync("received", response);
            });

            return true;
        }

        [HttpPost("UserStatus")]
        public IEnumerable<UserStatus> UserStatus([FromBody] IEnumerable<String> userIds)
        {
            return dc.Users.Select(x => new UserStatus { UserId = x.Id, IsOnline = true }).ToList();
        }
    }

    public class UserStatus
    {
        public string UserId { get; set; }
        public bool IsOnline { get; set; }
    }
}
