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
    [Authorize]
    public class MessageHub : Hub
    {
        private MessageDbContext dc = new MessageDbContext(new DbContextOptions<MessageDbContext>());

        private User GetCurrentUser()
        {
            var claims = Context.User.Claims.Select(x => new { Type = x.Type.ToLower(), x.Value }).ToList();

            return new User
            {
                Id = claims.First(x => x.Type == "userid").Value,
                FirstName = claims.First(x => x.Type == "firstname").Value,
                LastName = claims.First(x => x.Type == "lastname").Value,
                Email = claims.First(x => x.Type == "email").Value
            };
        }

        public override Task OnConnectedAsync()
        {
            var currentUser = GetCurrentUser();

            dc.Connections.Add(new Connection {
                Id = Context.ConnectionId,
                CreatedUserId = currentUser.Id
            });

            // add to public channel
            if(!dc.ChannelUsers.Any(x => x.ChannelId == Constants.PUBLIC_CHANNEL_ID && x.UserId == currentUser.Id))
            {
                dc.ChannelUsers.Add(new ChannelUser
                {
                    ChannelId = Constants.PUBLIC_CHANNEL_ID,
                    UserId = currentUser.Id
                });
            }

            var userEntity = dc.Users.Find(currentUser.Id);
            if (userEntity == null)
            {
                dc.Users.Add(currentUser);
            }

            dc.SaveChanges();

            SendToChannel(new MessageResponse {
                ChannelId = Constants.PUBLIC_CHANNEL_ID,
                Scope = MessageScope.SystemToChannel,
                Message = new MessageContent { Text = $"{currentUser.FullName} joined this conversation." },
                Sender = new User { }
            });

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var connection = dc.Connections.Find(Context.ConnectionId);
            dc.Connections.Remove(connection);
            dc.SaveChanges();

            var currentUser = dc.Users.Find(connection.CreatedUserId);

            SendToChannel(new MessageResponse
            {
                ChannelId = Constants.PUBLIC_CHANNEL_ID,
                Scope = MessageScope.SystemToChannel,
                Message = new MessageContent { Text = $"{currentUser.FullName} left this conversation." }
            });

            return base.OnDisconnectedAsync(exception);
        }
        
        private void SendToChannel(MessageResponse response)
        {
            var connections = (from cu in dc.ChannelUsers
                               join conn in dc.Connections on cu.UserId equals conn.CreatedUserId
                               where cu.ChannelId == response.ChannelId
                               select conn.Id).ToList();

            connections.Remove(Context.ConnectionId);

            connections.ForEach(connectionId =>
            {
                Clients.Client(connectionId).InvokeAsync("received", response);
            });
        }

        public Task Received(MessageRequest request)
        {
            if(String.IsNullOrEmpty(request.ChannelId))
            {
                request.ChannelId = Constants.PUBLIC_CHANNEL_ID;
            }

            SendToChannel(new MessageResponse
            {
                ChannelId = request.ChannelId,
                Message = request.Message,
                Scope = MessageScope.UserToChannel,
                Sender = GetCurrentUser(),
                Time = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task Typing(MessageRequest request)
        {
            if (String.IsNullOrEmpty(request.ChannelId))
            {
                request.ChannelId = Constants.PUBLIC_CHANNEL_ID;
            }

            var currentUser = GetCurrentUser();

            request.Message = new MessageContent { Text = $"{currentUser.FullName} is typing..."};

            SendToChannel(new MessageResponse
            {
                ChannelId = request.ChannelId,
                Message = request.Message,
                Scope = MessageScope.TypingIndicator,
                Sender = currentUser,
                Time = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task Typed(MessageRequest request)
        {
            if (String.IsNullOrEmpty(request.ChannelId))
            {
                request.ChannelId = Constants.PUBLIC_CHANNEL_ID;
            }

            var currentUser = GetCurrentUser();

            request.Message = new MessageContent { Text = $"{currentUser.FullName} finished typing." };

            SendToChannel(new MessageResponse
            {
                ChannelId = request.ChannelId,
                Message = request.Message,
                Scope = MessageScope.TypeStoppedIndicator,
                Sender = currentUser,
                Time = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task OnlineUsers()
        {
            var connections = (from conn in dc.Connections
                               join u in dc.Users on conn.CreatedUserId equals u.Id
                               select new { u.Id, u.FirstName, u.LastName, u.FullName }).Distinct().ToList();

            return Task.CompletedTask;
        }
    }
}
