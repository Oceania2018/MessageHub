using MessageService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageService
{
    public class MessageHub : Hub
    {
        private MessageDbContext dc = new MessageDbContext(new DbContextOptions<MessageDbContext>());

        private string GetUserId()
        {
            string userId = Context.User.Claims
                .First(x => x.Type.Equals("userId", StringComparison.CurrentCultureIgnoreCase))
                .Value;
            
            return userId;
        }

        public override Task OnConnectedAsync()
        {
            string currentUserId = GetUserId();

            // create user profile if not exists
            var user = dc.Users.Find(currentUserId);
            if (user == null)
            {
                user = new User { Id = currentUserId };
                MessageHubInitializer.GetUserProfile(user);
                dc.Users.Add(user);
                dc.SaveChanges();
            }

            dc.Connections.Add(new Connection {
                Id = Context.ConnectionId,
                CreatedUserId = currentUserId
            });

            // add to public channel
            dc.ChannelUsers.Add(new ChannelUser {
                ChannelId = Constants.PUBLIC_CHANNEL_ID,
                UserId = currentUserId
            });

            dc.SaveChanges();

            SendToChannel(new MessageResponse {
                ChannelId = Constants.PUBLIC_CHANNEL_ID,
                Scope = MessageScope.SystemToChannel,
                Message = new MessageContent { Text = $"{currentUserId} joined this conversation." }
            });

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var connection = dc.Connections.Find(Context.ConnectionId);
            dc.Connections.Remove(connection);
            dc.SaveChanges();

            // remove from public channel
            var userInPublicChannel = dc.ChannelUsers.FirstOrDefault(x => x.ChannelId == Constants.PUBLIC_CHANNEL_ID && x.UserId == connection.CreatedUserId);
            dc.ChannelUsers.Remove(userInPublicChannel);
            dc.SaveChanges();

            SendToChannel(new MessageResponse
            {
                ChannelId = Constants.PUBLIC_CHANNEL_ID,
                Scope = MessageScope.SystemToChannel,
                Message = new MessageContent { Text = $"{connection.CreatedUserId} left this conversation." }
            });

            return base.OnDisconnectedAsync(exception);
        }
        
        private void SendToChannel(MessageResponse response)
        {
            var connections = (from cu in dc.ChannelUsers
                               join conn in dc.Connections on cu.UserId equals conn.CreatedUserId
                               where cu.ChannelId == response.ChannelId
                               select conn.Id).ToList();

            if (response.Scope == MessageScope.UserToChannel)
            {
                connections.Remove(Context.ConnectionId);
            }

            connections.ForEach(connectionId =>
            {
                Clients.Client(connectionId).InvokeAsync("received", response);
            });
        }

        public Task Received(MessageRequest request)
        {
            SendToChannel(new MessageResponse {
                ChannelId = request.ChannelId,
                Message = request.Message,
                Scope = MessageScope.UserToChannel
            });

            return Task.CompletedTask;
        }
    }
}
