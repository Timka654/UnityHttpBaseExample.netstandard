using Appfox.Unity.AspNetCore.Phantom.Network;
using Appfox.Unity.AspNetCore.Phantom.Network.Packets;
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

        internal PhantomConnectionOptions Options => options;

        internal Task<string> GetAccessToken() => (options.AccessTokenProvider ?? new Func<Task<string>>(() => Task.FromResult(default(string))))();

        public static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30.0);

        public static readonly TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds(15.0);

        public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(15.0);

        public TimeSpan ConnectionTimeout
        {
            get;
            set;
        } = DefaultConnectionTimeout;


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

        private PhantomNetworkClient network;

        private HubConnectionState state = HubConnectionState.Disconnected;

        public HubConnectionState State { get => state; internal set => state = value; }

        public PhantomHubConnection(PhantomConnectionOptions options)
        {
            this.options = options;

            this.network = new PhantomNetworkClient(this);
        }

        public async Task StartAsync()
        {
            if (state == HubConnectionState.Connected)
                return;

            try
            {
                if (!await connectLocker.WaitAsync(0))
                    return;

                forceClosed = false;
                state = HubConnectionState.Connecting;

                using (HttpClient hc = new HttpClient())
                {
                    hc.Timeout = HandshakeTimeout;

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

                        Session = data.Session;
                        Path = data.Path;

                        await network.InitializeClient(data);

                        await authLocker.WaitAsync();
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

            connectLocker.Release();
        }

        public async Task StopAsync()
        {
            ForceClose(null);

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            ForceClose(null);
        }

        bool forceClosed = false;

        internal bool ForceClosedState => forceClosed;

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

        public void On<T1, T2>(string methodName, Action<T1, T2> handle)
        {
            methodDelegates.Add($"{methodName.ToLower()}_2", (_) =>
            {
                try
                {
                    handle(_.ReadJson16<T1>(), _.ReadJson16<T2>());
                }
                catch (Exception ex)
                {
                    ForceClose(ex);
                }

                return Task.CompletedTask;
            });
        }

        public void On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handle)
        {
            methodDelegates.Add($"{methodName.ToLower()}_3", (_) =>
            {
                try
                {
                    handle(_.ReadJson16<T1>(), _.ReadJson16<T2>(), _.ReadJson16<T3>());
                }
                catch (Exception ex)
                {
                    ForceClose(ex);
                }

                return Task.CompletedTask;
            });
        }

        public void On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> handle)
        {
            methodDelegates.Add($"{methodName.ToLower()}_4", (_) =>
            {
                try
                {
                    handle(_.ReadJson16<T1>(), _.ReadJson16<T2>(), _.ReadJson16<T3>(), _.ReadJson16<T4>());
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

        public async Task SendAsync(string methodName, params object[] args)
        {
            if (state != HubConnectionState.Connected)
                throw new Exception($"Current state is {state}, must be {nameof(HubConnectionState.Connected)} for send");
            await Task.Run(() =>
            {
                var packet = new OutputPacketBuffer() { PacketId = 2 };

                packet.WriteString16(methodName);

                packet.WriteCollection(args, (p, item) => { p.WriteJson16(item); });

                network.client.Send(packet);
            });
        }

        internal void ForceClose(Exception err)
        {
            forceClosed = true;
            Closed(err);
            if (network.client?.GetState() == true)
                network.client.Disconnect();
            else
                SetState(HubConnectionState.Disconnected);
        }

        private SemaphoreSlim connectLocker = new SemaphoreSlim(1);

        private SemaphoreSlim authLocker = new SemaphoreSlim(0);

        internal void SetState(HubConnectionState state)
        {
            this.state = state;

            if (state == HubConnectionState.Connected)
            {
                network.retryCount = 0;
            }

            if (authLocker.CurrentCount == 0)
                authLocker.Release();
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
