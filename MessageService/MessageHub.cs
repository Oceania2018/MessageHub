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
                Channel = GetChannelDetail(Constants.PUBLIC_CHANNEL_ID),
                Origin = MessageIndividual.System,
                Target = MessageIndividual.Channel,
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

            var currentUser = dc.Users.Find(connection.CreatedUserId);

            SendToChannel(new MessageResponse
            {
                Origin = MessageIndividual.System,
                Target = MessageIndividual.Channel,
                Channel = GetChannelDetail(Constants.PUBLIC_CHANNEL_ID),
                Command = MessageCommand.UserLeftChannel,
                Message = new MessageContent { Text = $"{currentUser.FullName} left this channel." }
            });

            return base.OnDisconnectedAsync(exception);
        }

        private Channel GetChannelDetail(string channelId)
        {
            var channel = dc.Channels.Find(channelId);
            channel.Participators = from u in dc.Users
                                    join cu in dc.ChannelUsers on u.Id equals cu.UserId
                                    where cu.ChannelId == Constants.PUBLIC_CHANNEL_ID
                                    select u;

            channel.Participators = channel.Participators.ToList();

            return channel;
        }

        private void SendToChannel(MessageResponse response)
        {
            var connections = from cu in dc.ChannelUsers
                               join conn in dc.Connections on cu.UserId equals conn.CreatedUserId
                               where cu.ChannelId == response.Channel.Id
                               select conn;

            string currentUserId = GetCurrentUser().Id;

            if(response.Origin == MessageIndividual.User 
                || response.Command == MessageCommand.UserTypingStart
                || response.Command == MessageCommand.UserTypingEnd)
            {
                connections = connections.Where(x => x.CreatedUserId != currentUserId);
            }

            connections.ToList().ForEach(connection =>
            {
                response.Sender = GetCurrentUser();
                response.Time = DateTime.UtcNow;
                Clients.Client(connection.Id).InvokeAsync("received", response);
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
                Channel = GetChannelDetail(request.ChannelId),
                Message = request.Message,
                Origin = MessageIndividual.User,
                Target = MessageIndividual.Channel
            });

            return Task.CompletedTask;
        }

        public Task InitChannel(string userId)
        {
            var user = GetCurrentUser();
            var userTarget = dc.Users.Find(userId);

            var channel = (from cu1 in dc.ChannelUsers
                          join cu2 in dc.ChannelUsers on cu1.ChannelId equals cu2.ChannelId
                          join c in dc.Channels on cu1.ChannelId equals c.Id
                          where cu1.UserId == userId && cu2.UserId == user.Id && c.Limit == 2
                          select c).FirstOrDefault();

            if (channel == null)
            {
                channel = new Channel { Id = Guid.NewGuid().ToString(), Title = $"{user.FirstName}, {userTarget?.FirstName}", Limit = 2 };
                dc.Channels.Add(channel);
                dc.ChannelUsers.Add(new ChannelUser { ChannelId = channel.Id, UserId = userId, CreatedTime = DateTime.UtcNow, CreatedUserId = user.Id });
                dc.ChannelUsers.Add(new ChannelUser { ChannelId = channel.Id, UserId = user.Id, CreatedTime = DateTime.UtcNow, CreatedUserId = user.Id });
                dc.SaveChanges();
            }
            else
            {
                channel.Title = $"{user.FullName}, {userTarget?.FullName}";
            }

            var connections = dc.Connections.Where(x => x.CreatedUserId == user.Id).ToList();

            var response = new MessageResponse
            {
                Channel = GetChannelDetail(channel.Id),
                Origin = MessageIndividual.System,
                Target = MessageIndividual.User,
                Command = MessageCommand.ChannelInitialized,
                Message = new MessageContent { Text = $"{user.FullName} init a channel with {userTarget.FullName}." }
            };

            connections.ForEach(connection =>
            {
                response.Sender = GetCurrentUser();
                response.Time = DateTime.UtcNow;
                Clients.Client(connection.Id).InvokeAsync("received", response);
            });

            return Task.CompletedTask;
        }

        public Task GetChannels()
        {
            var channels = (from uc in dc.ChannelUsers
                           join c in dc.Channels on uc.ChannelId equals c.Id
                           where uc.UserId == GetCurrentUser().Id
                           select c).ToList();

            channels.ForEach(channel => {
                channel = GetChannelDetail(channel.Id);
                channel.Title = "Public Channel";
            });

            Clients.Client(Context.ConnectionId).InvokeAsync("received", new MessageResponse
            {
                Origin = MessageIndividual.System,
                Target = MessageIndividual.User,
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
                Channel = GetChannelDetail(request.ChannelId),
                Message = request.Message,
                Command = MessageCommand.UserTypingStart,
                Origin = MessageIndividual.System,
                Target = MessageIndividual.Channel
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
                Channel = GetChannelDetail(request.ChannelId),
                Message = request.Message,
                Command = MessageCommand.UserTypingEnd,
                Origin = MessageIndividual.System,
                Target = MessageIndividual.Channel
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
