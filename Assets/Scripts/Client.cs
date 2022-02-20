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

public class Client : MonoBehaviour
{
    [SerializeField] public InputField _serveripaddress;
    [SerializeField] public InputField _serverport;
    [SerializeField] public Button _connectButton;
    [SerializeField] public ChatLogSpace chatLogSpace;
    [SerializeField] public InputField _message;
    [SerializeField] public Button _sendButton;

    private List<ChatData> receivedChats;

    private Socket server = null;

    void Awake()
    {

        receivedChats = new List<ChatData>();
    }

    // Update is called once per frame
    void Update()
    { 
    
    }

    private void OnGUI()
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

    private void OnDestroy()
    {
        server?.Close();
        server?.Dispose();
        server = null;
    }

    public void OnConnectButtonClicked()
    {
        if(server is null)
        {
            _serveripaddress.interactable = false;
            _serverport.interactable = false;
            _connectButton.GetComponentInChildren<Text>().text = "Disconnect";
            _message.interactable = true;
            _sendButton.interactable = true;
            // 指定したIPアドレス、ポートに接続する
            StartConnection(_serveripaddress.text, int.Parse(_serverport.text));
        }
        else
        {
            server.Close();
            server.Dispose();
            server = null;

            _serveripaddress.interactable = true;
            _serverport.interactable = true;
            _connectButton.GetComponentInChildren<Text>().text = "Connect";
            _message.interactable = false;
            _sendButton.interactable = false;
        }
    }

    public void StartConnection(string host, int port)
    {
        IPAddress ip;

        Debug.Log("ipaddress:" + host + " port:" + port);
        // ホストがIPアドレス形式じゃなかったら終了
        if (!IPAddress.TryParse(host, out ip))
        {
            Debug.Log($"[{transform.name}]server address is invalid.");
            return;
        }

        Debug.Log($"[{transform.name}]ipaddress:" + host + " port:" + port);

        // Connect用のSocketのモードを指定して初期化
        server = new Socket(ip.AddressFamily,
                              SocketType.Stream,
                              ProtocolType.Tcp);

        server.BeginConnect(ip, port, DoConnectTcpClientCallBack, server);
    }

    public void OnSendButtonClicked()
    {
        if (_message.text == "") return;
        Send(_message.text);
    }

    // サーバへの接続処理
    private void DoConnectTcpClientCallBack(IAsyncResult ar)
    {
        Socket s = (Socket)ar.AsyncState;
        
        try
        {
            s.EndConnect(ar);
        }
        catch (System.ObjectDisposedException)
        {
            // 閉じた時
            Debug.Log($"[{transform.name}]Closed: " + s.RemoteEndPoint);
            return;
        }

        Debug.Log($"[{transform.name}]Connect: " + server.RemoteEndPoint);
        Debug.Log($"[{transform.name}]ReceiveBufferSize: " + server.ReceiveBufferSize);
        Debug.Log($"[{transform.name}]SendBufferSize: " + server.SendBufferSize);

        // サーバからの受信を待つ
        AsyncStateObject so = new AsyncStateObject(server);
        server.BeginReceive(so.ReceiveBuffer,
                            0,
                            so.ReceiveBuffer.Length,
                            SocketFlags.None,
                            DoReceiveMessageCallback,
                            so);
    }

    // サーバからのメッセージ受信処理
    private void DoReceiveMessageCallback(IAsyncResult ar)
    {
        AsyncStateObject so = (AsyncStateObject)ar.AsyncState;

        int len = 0;
        try
        {
            len = so.Socket.EndReceive(ar);
        }
        catch
        {
            // 閉じた時
            Debug.Log($"[{transform.name}]Closed: " + so.Socket.RemoteEndPoint);
            return;
        }

        // クライアントからの読取可能データ量がZEROの場合
        if (len <= 0)
        {
            // クライアントが終了状態と判断し、切断
            Debug.Log($"[{transform.name}]Disconnected: " + so.Socket.RemoteEndPoint);
            return;
        }

        // サーバとのネットワークストリームを取得
        so.ReceivedData.Write(so.ReceiveBuffer, 0, len);
        if (so.Socket.Available == 0)
        {
            // 最後まで受信した時
            // 受信したデータを文字列に変換
            string str = Encoding.UTF8.GetString(so.ReceivedData.ToArray());
            ChatData c = JsonUtility.FromJson<ChatData>(str);
            Debug.Log($"[{transform.name}]Receive : {str}");
            lock(receivedChats)
            {
                receivedChats.Add(c);
            }
            so.ReceivedData.Close();
            so.ReceivedData = new MemoryStream();
        }
        // 再び受信待ち
        so.Socket.BeginReceive(so.ReceiveBuffer,
                               0,
                               so.ReceiveBuffer.Length,
                               SocketFlags.None,
                               DoReceiveMessageCallback,
                               so);
    }

    private void Send(String message)
    {
        if (server is null) return;
        if (!server.Connected)
        {
            Debug.Log($"[{transform.name}]server Disconnected");
            return;
        }

        ChatData c = new ChatData(null, message, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds());
        byte[] messagebyte = Encoding.UTF8.GetBytes(JsonUtility.ToJson(c));
        server.BeginSend(messagebyte,
                         0,
                         messagebyte.Length,
                         SocketFlags.None,
                         DoSendMessageCallBack,
                         server);
    }

    private void DoSendMessageCallBack(IAsyncResult ar)
    {
        Socket s = (Socket)ar.AsyncState;

        s.EndSend(ar);
    }
}
