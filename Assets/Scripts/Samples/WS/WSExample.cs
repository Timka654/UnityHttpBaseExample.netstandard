using Appfox.Unity.AspNetCore.HTTP.Extensions;
using Appfox.Unity.AspNetCore.WS.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WSExample : WSClient
{
    protected override string GetUrl() => "https://localhost:1234";

    protected override WSRetryPolicy GetReconnectPolicy()
    {
        return WSRetryPolicy.CreateNone();
    }

    private static WSExample Instance;

    static WSExample()
    {
        Instance = new WSExample();
    }

    public WSExample() : base()
    {
        Handle("abc", () =>
        {
            Debug.Log($"abc1432");
        });
    }


    public static HubConnectionState State() => Instance.CurrentState;

    public static void Connect(Action<WSClient> action) => Instance.ConnectAsync(action);

}
