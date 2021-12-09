using Appfox.Unity.AspNetCore.HTTP.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    public Button httpSendBtn;

    public Button wsConnectBtn;

    // Start is called before the first frame update
    void Start()
    {
        httpSendBtn.onClick.AddListener(httpSendBtnClickHandle);
        wsConnectBtn.onClick.AddListener(wsSendBtnClickHandle);
    }

    private void httpSendBtnClickHandle()
    {
        BaseWebRequestsExample.PasswordSignInSample3(new Appfox.Unity.AspNetCore.HTTP.Extensions.Examples.PasswordSignInRequestModel()
        {
            LoginName = "1",
            Password = "1"
        }, r =>
        {
            Debug.Log($"IsSuccessStatusCode: {r.MessageResponse.IsSuccessStatusCode}, StatusCode: {r.MessageResponse.StatusCode}");
        });
    }

    private void wsSendBtnClickHandle()
    {
        WSExample.Connect(w =>
        {
            Debug.Log($"{Enum.GetName(typeof(HubConnectionState), w.CurrentState)}");

            if (w.CurrentState == HubConnectionState.Connected)
                w.SendAsync("send");
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
