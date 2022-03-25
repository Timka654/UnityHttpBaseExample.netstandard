using Appfox.Unity.AspNetCore.Phantom;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatSample : MonoBehaviour
{
    [SerializeField] private TMP_InputField endPointText;

    [SerializeField] private Toggle autoReconnectToogle;

    [SerializeField] private GameObject textRowPrefab;

    [SerializeField] private GameObject chatContent;

    [SerializeField] private GameObject chatContentBody;

    [SerializeField] private GameObject connectBtn;

    [SerializeField] private GameObject sendBtn;

    [SerializeField] private TMP_InputField sendContentText;

    private ChatExample chatEx;

    // Start is called before the first frame update
    void Start()
    {
        connectBtn.GetComponent<Button>().onClick.AddListener(connectBtn_onClick);
        sendBtn.GetComponent<Button>().onClick.AddListener(sendBtn_onClick);

        endPointText.text = "https://localhost:7068/hubs/chatHub";
        UpdateStateBtn();
    }

    private void UpdateStateBtn() { connectBtn.GetComponentInChildren<TextMeshProUGUI>().text = (chatEx?.CurrentState ?? Appfox.Unity.AspNetCore.Phantom.HubConnectionState.Disconnected) == Appfox.Unity.AspNetCore.Phantom.HubConnectionState.Disconnected ? "connect" : "disconnect"; }

    // Update is called once per frame
    void Update()
    {

    }

    public void connectBtn_onClick()
    {
        if (chatEx == null)
        {
            chatEx = new ChatExample();

            chatEx.OnStateChanged += ChatEx_onStateChanged;

            chatEx.Handle<List<MessageStruct>>("initMessages", data =>
            {
                foreach (Transform item in chatContentBody.transform)
                {
                    Destroy(item.gameObject);
                }

                foreach (var item in data)
                {
                    AddContent(item);
                }
            });

            chatEx.Handle<DateTime, string>("receiveMessage", (date, content) =>
            {
                AddContent(date, content);
            });
        }

        chatEx.SetData(endPointText.text, autoReconnectToogle.isOn ? WSRetryPolicy.CreateStaticPolicy(TimeSpan.FromSeconds(10)) : WSRetryPolicy.CreateNone());

        if (chatEx.CurrentState == HubConnectionState.Disconnected)

            chatEx.ConnectAsync(client =>
            {
                UpdateStateBtn();
            });
        else
            chatEx.DisconnectAsync(_ =>
            {
                Debug.Log("Success disconnected");
            });
    }

    private void ChatEx_onStateChanged(HubConnectionState obj)
    {
        UpdateStateBtn();
    }

    private void AddContent(MessageStruct s) => AddContent(s.createTime, s.content);

    private void AddContent(DateTime createTime, string content)
    {
        var row = GameObject.Instantiate(textRowPrefab);

        row.GetComponent<TextMeshProUGUI>().text = $"{createTime} - {content}";

        row.transform.parent = chatContentBody.transform;
    }

    public void sendBtn_onClick()
    {
        chatEx.SendAsync("SendMessage", sendContentText.text);
        sendContentText.text = default;
    }
}

public class MessageStruct
{
    public DateTime createTime { get; set; }
    public string content { get; set; }
}