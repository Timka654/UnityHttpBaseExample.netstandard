using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Appfox.Unity.AspNetCore.Phantom;
using SCL.Unity;
using UnityEngine;

namespace Appfox.Unity.AspNetCore.WS.Extensions
{
    public class WSClient : IDisposable
    {
        protected virtual string GetUrl() => "http://localhosts/hubs/hub";

        protected virtual WSRetryPolicy GetReconnectPolicy() => WSRetryPolicy.CreateNone();

        protected virtual string GetAccessToken() => string.Empty;

        PhantomHubConnection _hubConnection;

        protected WSClient()
        {
            _hubConnection = new PhantomHubConnectionBuilder()
                .WithUrl(GetUrl(), o => { o.AccessTokenProvider = () => Task.FromResult(GetAccessToken()); })
                .WithAutomaticReconnect(GetReconnectPolicy())
                .Build();

            _hubConnection.Closed += (e) => { ThreadHelper.InvokeOnMain(() => OnClosed(e)); return Task.CompletedTask; };

            _hubConnection.Reconnected += (e) => { ThreadHelper.InvokeOnMain(() => OnReconnected(e)); return Task.CompletedTask; };

            _hubConnection.Reconnecting += (e) => { ThreadHelper.InvokeOnMain(() => OnReconnecting(e)); return Task.CompletedTask; };
        }

        public async Task Connect()
        {
            try
            {
                await _hubConnection.StartAsync();
            }
            catch (Exception e)
            {
                ThreadHelper.InvokeOnMain(() => OnClosed(e));
            }
        }

        public async void ConnectAsync()
        {
            await Connect();
        }

        public async void ConnectAsync(Action<WSClient> afterConnect)
        {
            await Connect();
            ThreadHelper.InvokeOnMain(() => afterConnect(this));
        }

        public Task Disconnect() => _hubConnection.StopAsync();

        public async void DisconnectAsync()
        {
            await Disconnect();
        }

        public async void DisconnectAsync(Action<WSClient> afterDisconnect)
        {
            await Disconnect();
            ThreadHelper.InvokeOnMain(() => afterDisconnect(this));
        }

        public async void Dispose()
        {
            await _hubConnection.DisposeAsync();
        }

        public void Handle<T1>(string methodName, Action<T1> args)
        {
            _hubConnection.On<T1>(methodName, p1 =>
            {
                ThreadHelper.InvokeOnMain(() =>
               {
                   args(p1);
               });
            });
        }

        public void Handle(string methodName, Action args)
        {
            _hubConnection.On(methodName, () =>
            {
                ThreadHelper.InvokeOnMain(() =>
                {
                    args();
                });
            });
        }

        public async void SendAsync(string methodName)
        {
            await _hubConnection.SendAsync(methodName);
        }

        public async void SendAsync<TData>(string methodName, TData data)
        {
            await _hubConnection.SendAsync(methodName, data);
        }

        public async void SendAsync(string methodName, Action<WSClient> afterSend)
        {
            await _hubConnection.SendAsync(methodName);
            ThreadHelper.InvokeOnMain(() => afterSend(this));
        }

        public async void SendAsync<TData>(string methodName, TData data, Action<WSClient, TData> afterSend)
        {
            await _hubConnection.SendAsync(methodName, data);
            ThreadHelper.InvokeOnMain(() => afterSend(this, data));
        }

        public HubConnectionState CurrentState => _hubConnection.State;

        protected virtual void OnClosed(Exception ex)
        {
            if (ex != null)
                Debug.LogException(ex);
        }

        protected virtual void OnReconnecting(Exception ex)
        {

        }

        protected virtual void OnReconnected(string ex)
        {

        }
    }
}
