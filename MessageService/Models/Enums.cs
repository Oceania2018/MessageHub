using System;
using System.Collections.Generic;
using System.Text;

namespace MessageService.Models
{
    public enum MessageScope
    {
        SystemToAll = 1,
        SystemToChannel = 2,
        UserToChannel = 3
    }
}
