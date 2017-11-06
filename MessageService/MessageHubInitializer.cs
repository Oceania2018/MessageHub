﻿using MessageService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageService
{
    public class MessageHubInitializer
    {
        internal static Action<User> GetUserProfile;

        public static void Init(Action<User> getUserProfile)
        {
            GetUserProfile = getUserProfile;
            
            MessageDbContext dc = new MessageDbContext(new DbContextOptions<MessageDbContext>());

            // Add default public channel
            if(!dc.Channels.Any(x => x.Id == Constants.PUBLIC_CHANNEL_ID))
            {
                dc.Channels.Add(new Channel { Id = Constants.PUBLIC_CHANNEL_ID });
                dc.SaveChanges();
            }
        }
    }
}