using Appfox.Unity.AspNetCore.Phantom.Network.Packets;
using SCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Appfox.Unity.AspNetCore.Phantom.PhantomHubConnection;

namespace Appfox.Unity.AspNetCore.Phantom.Network
{
    internal class PhantomNetworkClient
    {
        ClientOptions<PhantomSocketNetworkClient> clientOptions;

        internal SocketClient<PhantomSocketNetworkClient, ClientOptions<PhantomSocketNetworkClient>> client;

        public async Task InitializeClient(PhantomRequestResult data)
        {
            var connectUrl = new Uri(data.Url);

            var dnss = Dns.GetHostAddresses(connectUrl.Host);

            var dns = dnss.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) ?? dnss.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);

            if (dns == null)
                throw new Exception($"dns for {data.Url} not found");

            clientOptions = new ClientOptions<PhantomSocketNetworkClient>();

            var ip = IPAddress.Parse(dns.ToString());

            clientOptions.ProtocolType = System.Net.Sockets.ProtocolType.Tcp;
            clientOptions.AddressFamily = ip.AddressFamily;
            clientOptions.IpAddress = ip.ToString();
            clientOptions.Port = connectUrl.Port;

            clientOptions.ReceiveBufferSize = 1024;

            clientOptions.OnClientConnectEvent += ClientOptions_OnClientConnectEvent;
            clientOptions.OnClientDisconnectEvent += ClientOptions_OnClientDisconnectEvent;
            clientOptions.OnExceptionEvent += ClientOptions_OnExceptionEvent;

            clientOptions.AddPacket(1, new SessionPacket(phantomHubConnection));
            clientOptions.AddPacket(2, new InvokePacket());

            phantomHubConnection.Options.CipherProvider.SetProvider(clientOptions);

            client = new SocketClient<PhantomSocketNetworkClient, ClientOptions<PhantomSocketNetworkClient>>(clientOptions);

            await client.ConnectAsync();
        }

        internal int retryCount = 0;
        private PhantomHubConnection phantomHubConnection;

        public PhantomNetworkClient(PhantomHubConnection phantomHubConnection)
        {
            this.phantomHubConnection = phantomHubConnection;
        }

        private async void ClientOptions_OnClientDisconnectEvent(PhantomSocketNetworkClient client)
        {
            var oldState = phantomHubConnection.State;

            phantomHubConnection.State = HubConnectionState.Disconnected;

            if (oldState == HubConnectionState.Connected && !phantomHubConnection.ForceClosedState && phantomHubConnection.Options.RetryPolicy != null)
            {
                var elapse = phantomHubConnection.Options.RetryPolicy.NextRetryDelay(new RetryContext() { ElapsedTime = TimeSpan.Zero, PreviousRetryCount = retryCount, RetryReason = null });
                retryCount++;
                if (elapse.HasValue)
                {
                    await Task.Delay(elapse.Value);
                    reStartAsync();
                }
            }
            else
            {
                phantomHubConnection.SetState(HubConnectionState.Disconnected);
            }
        }
        private async void reStartAsync()
        {
            await phantomHubConnection.StartAsync();
        }

        private void ClientOptions_OnClientConnectEvent(PhantomSocketNetworkClient client)
        {
            client.connection = phantomHubConnection;
            SessionPacket.Send(client, phantomHubConnection.Path, phantomHubConnection.Session);

        }
        private void ClientOptions_OnExceptionEvent(Exception ex, PhantomSocketNetworkClient client)
        {
            Debug.LogError(ex.ToString());
        }

    }
}
