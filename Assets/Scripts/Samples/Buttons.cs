using Appfox.Unity.AspNetCore.HTTP.Extensions;
using Appfox.Unity.AspNetCore.Phantom;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    public Button httpSendBtn;

    public Button wsConnectBtn;

    Guid guidId;

    int i;

    void Start()
    {
        httpSendBtn.onClick.AddListener(httpSendBtnClickHandle);
        wsConnectBtn.onClick.AddListener(wsSendBtnClickHandle);
    }

    private static System.Random random = new System.Random();
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    private void wsSendBtnClickHandle()
    {
        WSExample.Connect(w =>
        {
            Debug.Log($"{Enum.GetName(typeof(HubConnectionState), w.CurrentState)}");

            if (w.CurrentState == HubConnectionState.Connected)
                w.SendAsync("Authorize", guidId);
        });
    }



    string userName1 = RandomString(10);
    string email1 = RandomString(3) + "@" + RandomString(3);
    string password1 = RandomString(5);

    string userName2 = RandomString(10);
    string email2 = RandomString(3) + "@" + RandomString(3);
    string password2 = RandomString(5);

    private void httpSendBtnClickHandle()
    {
        StartCoroutine(a());
    }

    IEnumerator a()
    {
        Reg(userName1, email1, password1);
        yield return new WaitForSeconds(2);
        Reg(userName2, email2, password2);
    }

    void Reg(string userName, string email, string password)
    {
        BaseWebRequestsExample.SignUp(new CreateAccountQueryModel()
        {
            userName = userName,
            email = email,
            password = password,
        }, r =>
        {
            Debug.Log($"IsSuccessStatusCode: {r.MessageResponse.IsSuccessStatusCode}, StatusCode: {r.MessageResponse.StatusCode}");
            Auth(userName, password);
        });
    }

    void Auth(string userName, string password)
    {
        BaseWebRequestsExample.PasswordSignInSample3(new Appfox.Unity.AspNetCore.HTTP.Extensions.Examples.PasswordSignInRequestModel()
        {
            UserName = userName,
            Password = password
        }, r =>
        {
            Debug.Log($"IsSuccessStatusCode: {r.MessageResponse.IsSuccessStatusCode}, StatusCode: {r.MessageResponse.StatusCode}");
            WSExample.token = r.Data;
            JoinLobby();
        });
    }

    void JoinLobby()
    {
        BaseWebRequestsExample.JoinLobby(1, r =>
        {
            Debug.Log($"IsSuccessStatusCode: {r.MessageResponse.IsSuccessStatusCode}, StatusCode: {r.MessageResponse.StatusCode}");

            guidId = new Guid(r.Data);
            if (i==0)
                wsSendBtnClickHandle();
            i++;
        });
    }

    /*IEnumerator a2()
    {
        yield return new WaitForSeconds(1);

        *//*BaseWebRequestsExample.PasswordSignInSample3(new Appfox.Unity.AspNetCore.HTTP.Extensions.Examples.PasswordSignInRequestModel()
        {
            UserName = "string1",
            Password = "string"
        }, r =>
        {

            Debug.Log($"IsSuccessStatusCode: {r.MessageResponse.IsSuccessStatusCode}, StatusCode: {r.MessageResponse.StatusCode}");
            a2();
        });*//*

        BaseWebRequestsExample.SignUp(new CreateAccountQueryModel()
        {
            userName = RandomString(10),
            email = RandomString(3) + "@" + RandomString(3),
            password = RandomString(5),
        }, r =>
        {
            Debug.Log($"IsSuccessStatusCode: {r.MessageResponse.IsSuccessStatusCode}, StatusCode: {r.MessageResponse.StatusCode}");
            WSExample.token = r.Data;
            a1();
        });
    }

    void a3()
    {
        BaseWebRequestsExample.JoinLobby(1, async r =>
        {
            Debug.Log($"IsSuccessStatusCode: {r.MessageResponse.IsSuccessStatusCode}, StatusCode: {r.MessageResponse.StatusCode}");

            guidId = new Guid(r.Data);

            wsSendBtnClickHandle();
        });
    }*/
}
