using SocketClient;
using SocketCore.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Appfox.Unity.AspNetCore.Phantom
{
    internal class PhantomSocketNetworkClient : BaseSocketNetworkClient
    {
        public PhantomHubConnection connection;
    }
}
