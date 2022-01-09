using Newtonsoft.Json;
using SCL;
using SocketCore.Extensions.Buffer;
using SocketCore.Utils.Buffer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Appfox.Unity.AspNetCore.Phantom
{
    public class PhantomHubConnection : IDisposable
    {
        public string Session { get; private set; }
        public string Path { get; private set; }

        private PhantomConnectionOptions options;

        public static readonly TimeSpan DefaultServerTimeout = TimeSpan.FromSeconds(30.0);

        public static readonly TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds(15.0);

        public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(15.0);

        public TimeSpan ServerTimeout
        {
            get;
            set;
        } = DefaultServerTimeout;


        public TimeSpan KeepAliveInterval
        {
            get;
            set;
        } = DefaultKeepAliveInterval;


        public TimeSpan HandshakeTimeout
        {
            get;
            set;
        } = DefaultHandshakeTimeout;

        public event Func<Exception, Task> Closed = (_) => Task.CompletedTask;
        public event Func<string, Task> Reconnected = (_) => Task.CompletedTask;
        public event Func<Exception, Task> Reconnecting = (_) => Task.CompletedTask;

        private HubConnectionState state = HubConnectionState.Disconnected;

        public HubConnectionState State => state;

        public PhantomHubConnection(PhantomConnectionOptions options)
        {
            this.options = options;
        }

        public async void startA()
        {
            await StartAsync();
        }

        public async Task StartAsync()
        {
            try
            {
                if (!await authLocker.WaitAsync(0))
                    return;

                forceClosed = false;
                state = HubConnectionState.Connecting;

                using (HttpClient hc = new HttpClient())
                {
                    string endUrl = string.Empty;

                    if (!string.IsNullOrWhiteSpace(Session))
                        endUrl += $"?session={Session}";

                    if (options.AccessTokenProvider != null)
                    {
                        if (string.IsNullOrWhiteSpace(endUrl))
                            endUrl = "?";
                        else
                            endUrl += $"&";

                        endUrl += $"access_token={ await options.AccessTokenProvider()}";
                    }

                    HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, options.Url + endUrl);

                    var response = await hc.SendAsync(msg);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();

                        var data = JsonConvert.DeserializeObject<PhantomRequestResult>(content);

                        await InitializeClient(data);
                    }
                    else
                    {
                        throw new Exception("cannot connected");
                    }
                }

            }
            catch (Exception ex)
            {
                if (authLocker.CurrentCount == 0)
                    authLocker.Release();

                state = HubConnectionState.Disconnected;
                await Closed(ex);
            }
        }

        private async Task InitializeClient(PhantomRequestResult data)
        {
            Session = data.Session;
            Path = data.Path;

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

            clientOptions.AddPacket(1, new SessionResultPacket());
            clientOptions.AddPacket(2, new InvokeResultPacket());

            options.CipherProvider.SetProvider(clientOptions);

            client = new SocketClient<PhantomSocketNetworkClient, ClientOptions<PhantomSocketNetworkClient>>(clientOptions);

            await client.ConnectAsync();

            await authLocker.WaitAsync();
        }

        private void ClientOptions_OnExceptionEvent(Exception ex, PhantomSocketNetworkClient client)
        {
            Debug.LogError(ex.ToString());
        }

        int retryCount = 0;

        private async void ClientOptions_OnClientDisconnectEvent(PhantomSocketNetworkClient client)
        {
            var oldState = state;

            state = HubConnectionState.Disconnected;

            if (oldState == HubConnectionState.Connected && !forceClosed && options.RetryPolicy != null)
            {
                var elapse = options.RetryPolicy.NextRetryDelay(new RetryContext() { ElapsedTime = TimeSpan.Zero, PreviousRetryCount = retryCount, RetryReason = null });
                retryCount++;
                if (elapse.HasValue)
                {
                    await Task.Delay(elapse.Value);
                    startA();
                }
            }
        }

        private void ClientOptions_OnClientConnectEvent(PhantomSocketNetworkClient client)
        {
            client.connection = this;
            SessionResultPacket.Send(client, Path, Session);

        }

        ClientOptions<PhantomSocketNetworkClient> clientOptions;

        SocketClient<PhantomSocketNetworkClient, ClientOptions<PhantomSocketNetworkClient>> client;

        public async Task StopAsync()
        {
            ForceClose(null);

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            disposed = true;
        }

        bool disposed = false;

        bool forceClosed = false;

        public async Task DisposeAsync()
        {
            await Task.Run(() => Dispose());
        }

        public void On(string methodName, Action handle)
        {
            methodDelegates.Add($"{methodName.ToLower()}_0", (_) =>
            {
                try
                {
                    handle();
                }
                catch (Exception ex)
                {
                    ForceClose(ex);
                }

                return Task.CompletedTask;
            });
        }

        public void On<T1>(string methodName, Action<T1> handle)
        {
            methodDelegates.Add($"{methodName.ToLower()}_1", (_) =>
            {
                try
                {
                    handle(_.ReadJson16<T1>());
                }
                catch (Exception ex)
                {
                    ForceClose(ex);
                }

                return Task.CompletedTask;
            });
        }

        public async Task SendAsync(string methodName)
        {
            await SendAsync(methodName, new object[] { });
        }

        public async Task SendAsync(string methodName, object[] args)
        {
            if (state != HubConnectionState.Connected)
                throw new Exception($"Current state is {state}, must be {nameof(HubConnectionState.Connected)} for send");
            await Task.Run(() =>
            {
                var packet = new OutputPacketBuffer() { PacketId = 2 };

                packet.WriteString16(methodName);

                packet.WriteCollection(args, (p, item) => { p.WriteJson16(item); });

                client.Send(packet);
            });
        }

        public async Task SendAsync<T1>(string methodName, T1 arg1)
        {
            await SendAsync(methodName, new object[] { arg1 });
        }

        internal void ForceClose(Exception err)
        {
            forceClosed = true;
            Closed(err);
            if (client?.GetState() == true)
                client.Disconnect();
            else
                SetState(HubConnectionState.Disconnected);
        }

        private SemaphoreSlim authLocker = new SemaphoreSlim(1);

        internal async void SetState(HubConnectionState state)
        {
            this.state = state;

            if (state == HubConnectionState.Connected)
            {
                retryCount = 0;

                if (authLocker.CurrentCount == 0)
                    authLocker.Release();
            }
        }

        private Dictionary<string, Func<InputPacketBuffer, Task>> methodDelegates = new Dictionary<string, Func<InputPacketBuffer, Task>>();

        internal async void Invoke(InputPacketBuffer packet)
        {
            using (var ip = new InputPacketBuffer())
            {
                packet.CopyTo(ip);

                ip.Position = 0;

                string methodName = ip.ReadString16().ToLower();

                if (methodDelegates.TryGetValue($"{methodName}_{ip.ReadInt32()}", out var func))
                {
                    await func(ip);
                }
                else
                {
                    ForceClose(new Exception($"Received method {methodName} not found with args"));
                }
            }
        }

        public class PhantomRequestResult
        {
            public string Path { get; set; }

            public string Session { get; set; }

            public string Url { get; set; }
        }
    }
}
