using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading;
using UnityEngine.SceneManagement;

public abstract class ClinetBase : MonoBehaviour
{
    protected enum SocketStatus
    {
        None,
        Connecting,
        Connect,
        Diconnecting
    }

    [SerializeField] public InputField _ipaddress;
    [SerializeField] public InputField _port;
    [SerializeField] public ChatLogSpace chatLogSpace;
    [SerializeField] public InputField _message;
    [SerializeField] public Button _sendButton;

    protected List<ChatData> receivedChats;

    protected Socket server = null;

    // Start is called before the first frame update
    protected void Awake()
    {
        receivedChats = new List<ChatData>();
        foreach (IPAddress h in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            if (h.AddressFamily.Equals(AddressFamily.InterNetwork))
            {
                _ipaddress.text = h.ToString();
                return;
            }
        }
    }

    protected void OnGUI()
    {
        if (receivedChats.Count > 0)
        {
            lock (receivedChats)
            {
                chatLogSpace.Add(receivedChats);
                receivedChats.Clear();
            }
        }
    }

    protected void OnDestroy()
    {
        server?.Close();
        server?.Dispose();
        server = null;
    }

    public virtual void OnSendButtonClicked()
    {
        return;
    }

    public void OnTitleButtonClicked()
    {
        SceneManager.LoadScene("Scenes/Title");
        return;
    }

    protected virtual void DoReceiveMessageCallback(IAsyncResult ar)
    {
        AsyncStateObject so = (AsyncStateObject)ar.AsyncState;

        // 再び受信待ち
        so.Socket.BeginReceive(so.ReceiveBuffer,
                               0,
                               so.ReceiveBuffer.Length,
                               SocketFlags.None,
                               DoReceiveMessageCallback,
                               so);
    }

    protected void Send(ChatData chatData, Socket target)
    {
        if (target is null) return;
        if (!target.Connected)
        {
            Debug.Log($"[{transform.name}]server Disconnected");
            return;
        }

        byte[] messagebyte = Encoding.UTF8.GetBytes(JsonUtility.ToJson(chatData));
        target.BeginSend(messagebyte,
                         0,
                         messagebyte.Length,
                         SocketFlags.None,
                         DoSendMessageCallBack,
                         target);
    }

    protected void DoSendMessageCallBack(IAsyncResult ar)
    {
        Socket s = (Socket)ar.AsyncState;

        s.EndSend(ar);
    }

    
}
