using System;
using System.Collections.Generic;
using System.Text;

namespace MessageService.Models
{
    public enum MessageIndividual
    {
        System = 1,
        Channel = 2,
        User = 3
    }

    public enum MessageCommand
    {
        UserConnected = 1,
        UserDisconnected = 2,

        UserJoinedChannel = 5,
        UserLeftChannel = 6,
        UserKickedOutFromChannel = 7,

        ChannelInitialized = 11,
        ChannelRenamed = 12,
        ChannelDeleted = 13,
        RetrieveAllChannelsForCurrent = 14,

        UserTypingStart = 21,
        UserTypingEnd = 22,
    }
}
