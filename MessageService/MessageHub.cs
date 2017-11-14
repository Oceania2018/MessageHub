using MessageService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
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
                ChannelTitle = "Public Channel",
                Target = MessageTarget.Channel,
                Command = MessageCommand.UserJoinedChannel,
                Message = new MessageContent { Text = $"{currentUser.FullName} joined this channel." },
                Sender = new User { }
            });

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var connection = dc.Connections.Find(Context.ConnectionId);
            dc.Connections.Remove(connection);
            dc.SaveChanges();

            // remove from public channel
            /*var userInPublicChannel = dc.ChannelUsers.FirstOrDefault(x => x.ChannelId == Constants.PUBLIC_CHANNEL_ID && x.UserId == connection.CreatedUserId);
            dc.ChannelUsers.Remove(userInPublicChannel);
            dc.SaveChanges();*/

            var currentUser = dc.Users.Find(connection.CreatedUserId);

            SendToChannel(new MessageResponse
            {
                ChannelId = Constants.PUBLIC_CHANNEL_ID,
                Target = MessageTarget.Channel,
                Command = MessageCommand.UserLeftChannel,
                Message = new MessageContent { Text = $"{currentUser.FullName} left this channel." }
            });

            return base.OnDisconnectedAsync(exception);
        }
        
        private void SendToChannel(MessageResponse response)
        {
            var connections = (from cu in dc.ChannelUsers
                               join conn in dc.Connections on cu.UserId equals conn.CreatedUserId
                               where cu.ChannelId == response.ChannelId
                               select conn.Id).ToList();

            if (response.Target == MessageTarget.User)
            {
                connections.Remove(Context.ConnectionId);
            }

            connections.ForEach(connectionId =>
            {
                response.Sender = GetCurrentUser();
                response.Time = DateTime.UtcNow;
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
                Target = MessageTarget.User
            });

            return Task.CompletedTask;
        }

        public Task InitChannel(string userId)
        {
            var user = GetCurrentUser();
            var user2 = dc.Users.Find(userId);

            var channel = (from cu1 in dc.ChannelUsers
                          join cu2 in dc.ChannelUsers on cu1.ChannelId equals cu2.ChannelId
                          join c in dc.Channels on cu1.ChannelId equals c.Id
                          where cu1.UserId == userId && cu2.UserId == user.Id && c.Limit == 2
                          select c).FirstOrDefault();

            if(channel == null)
            {
                channel = new Channel { Id = Guid.NewGuid().ToString(), Title = $"{user.FullName}, {user2.FullName}", Limit = 2 };
                dc.Channels.Add(channel);
                dc.ChannelUsers.Add(new ChannelUser { ChannelId = channel.Id, UserId = userId, CreatedTime = DateTime.UtcNow, CreatedUserId = user.Id });
                dc.ChannelUsers.Add(new ChannelUser { ChannelId = channel.Id, UserId = user.Id, CreatedTime = DateTime.UtcNow, CreatedUserId = user.Id });
                dc.SaveChanges();
            }

            SendToChannel(new MessageResponse
            {
                ChannelId = channel.Id,
                ChannelTitle = channel.Title,
                Target = MessageTarget.Channel,
                Command = MessageCommand.ChannelCreated,
                Message = new MessageContent { Text = $"{user.FullName} started a session with you." }
            });

            return Task.CompletedTask;
        }

        public Task GetChannels()
        {
            var channels = (from uc in dc.ChannelUsers
                           join c in dc.Channels on uc.ChannelId equals c.Id
                           where uc.UserId == GetCurrentUser().Id
                           select c).ToList();

            Clients.Client(Context.ConnectionId).InvokeAsync("received", new MessageResponse
            {
                Target = MessageTarget.User,
                Command = MessageCommand.RetrieveAllChannelsForCurrent,
                Time = DateTime.UtcNow,
                Data = channels
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
                Target = MessageTarget.Channel
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
                Target = MessageTarget.Channel
            });

            return Task.CompletedTask;
        }
    }
}
