using Appfox.Unity.AspNetCore.Phantom;
using Appfox.Unity.AspNetCore.WS.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatExample : WSClient
{
    protected override WSRetryPolicy GetReconnectPolicy() => policy;

    protected override string GetUrl() => url;

    string url;
    WSRetryPolicy policy;

    public event Action<HubConnectionState> OnStateChanged = s => { };

    protected override void StateChanged(HubConnectionState state)
    {
        OnStateChanged(state);
        base.StateChanged(state);
    }

    public void SetData(string url, WSRetryPolicy policy)
    {
        this.url = url;
        this.policy = policy;
    }

}
