using SocketCore.Utils;
using SocketCore.Utils.Buffer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Appfox.Unity.AspNetCore.Phantom
{
    internal class InvokeResultPacket : IPacket<PhantomSocketNetworkClient>
    {
        public override void Receive(PhantomSocketNetworkClient client, InputPacketBuffer data)
        {
            client.connection.Invoke(data);
        }
    }
}
