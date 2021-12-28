using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Appfox.Unity.AspNetCore.Phantom
{
    public enum HubConnectionState
    {
        Disconnected,
        Connected,
        Connecting,
        Reconnecting
    }
}
