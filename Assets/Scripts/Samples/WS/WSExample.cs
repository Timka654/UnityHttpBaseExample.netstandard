using Appfox.Unity.AspNetCore.Phantom;
using Appfox.Unity.AspNetCore.WS.Extensions;
using System;
using UnityEngine;

public class WSExample : WSClient
{
    public static string token;

    protected override string GetUrl() => "http://5.63.155.214/hubs/BattleHub";

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
        Handle<bool>("AuthorizeResult", (b) =>
        {
            Debug.Log(b);
        });
        Handle<LobbyRoom>("SetData", (lobbyroom) =>
        {
            Debug.Log(lobbyroom);
        });
    }

    public static HubConnectionState State() => Instance.CurrentState;

    public static void Connect(Action<WSClient> action) => Instance.ConnectAsync(action);

    protected override string GetAccessToken()
    {
        return token;
    }

}
