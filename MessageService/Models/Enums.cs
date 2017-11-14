using System;
using System.Collections.Generic;
using System.Text;

namespace MessageService.Models
{
    public enum MessageTarget
    {
        System = 1,
        Channel = 2, 
        User = 3,

        Page = 11,
        Block = 12,
        View = 13,
        Entity = 14
    }

    public enum MessageCommand
    {
        UserConnected = 1,
        UserDisconnected = 2,

        UserJoinedChannel = 5,
        UserLeftChannel = 6,
        UserKickedOutFromChannel = 7,

        ChannelCreated = 11,
        ChannelRenamed = 12,
        ChannelDeleted = 13,
        RetrieveAllChannelsForCurrent = 14,

        UserTypingStarted = 21,
        UserTypingEnded = 22,
    }
}
