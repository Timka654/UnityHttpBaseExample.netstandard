using SocketCore.Utils;
using SocketCore.Utils.Buffer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Appfox.Unity.AspNetCore.Phantom
{
    internal class SessionResultPacket : IPacket<PhantomSocketNetworkClient>
    {
        public override void Receive(PhantomSocketNetworkClient client, InputPacketBuffer data)
        {
            byte state = data.ReadByte();
            switch (state)
            {
                case byte.MaxValue:
                    client.connection.SetState(HubConnectionState.Connected);
                    break;
                case 0:
                    client.connection.ForceClose(new Exception($"Current hub path not found"));
                    break;
                case 1:
                    client.connection.ForceClose(new Exception($"Cannot sign by current data"));
                    break;
                default:
                    break;
            }
            if (state == byte.MaxValue)
            {
                client.connection.SetState(HubConnectionState.Connected);
            }
        }

        public static void Send(PhantomSocketNetworkClient client, string path, string session)
        {
            var packet = new OutputPacketBuffer();
            packet.PacketId = 1;
            packet.WriteString16(path);
            packet.WriteString16(session);

            client.Send(packet);
        }
    }
}
