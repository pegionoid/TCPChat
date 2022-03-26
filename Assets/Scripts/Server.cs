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

public class Server : ClinetBase
{
    [SerializeField] public Button _listenButton;

    private List<Socket> listClient;
    
    private new void Awake()
    {
        base.Awake();
        listClient = new List<Socket>();
    }

    private new void OnDestroy()
    {
        foreach (Socket c in listClient)
        {
            c.Shutdown(SocketShutdown.Both);
        }
        listClient.Clear();
        base.OnDestroy();
    }

    public override void OnSendButtonClicked()
    {
        if (_message.text == "") return;
        ChatData c = new ChatData(((IPEndPoint)server.LocalEndPoint).ToString()
                                  , _message.text
                                  , new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds());
        lock (receivedChats)
        {
            receivedChats.Add(c);
        }
        lock (listClient)
        {
            SendAll(c);
        }
        _message.text = "";
    }
    
    public void OnListenButtonClicked()
    {
        if(server is null)
        {
            _ipaddress.interactable = false;
            _port.interactable = false;
            _listenButton.GetComponentInChildren<Text>().text = "Stop";
            _message.interactable = true;
            _sendButton.interactable = true;
            // 指定したポートを開く
            Listen(_ipaddress.text, int.Parse(_port.text));
        }
        else
        {
            foreach(Socket c in listClient)
            {
                c.Shutdown(SocketShutdown.Both);
            }
            listClient.Clear();
            server.Close();
            server.Dispose();
            server = null;

            _ipaddress.interactable = true;
            _port.interactable = true;
            _listenButton.GetComponentInChildren<Text>().text = "Listen";
            _message.interactable = false;
            _sendButton.interactable = false;
        }
        
    }

    // ソケット接続準備、待機
    private void Listen(string host, int port)
    {
        IPAddress ip;

        Debug.Log("ipaddress:" + host + " port:" + port);
        // ホストがIPアドレス形式じゃなかったら終了
        if (!IPAddress.TryParse(host, out ip))
        {
            Debug.Log("host address is invalid.");
            return;
        }

        IPEndPoint serverEndPoint = new IPEndPoint(ip, port);

        // Listen用のSocketのモードを指定して初期化
        server = new Socket(ip.AddressFamily,
                              SocketType.Stream,
                              ProtocolType.Tcp);

        // 初期化したSocketをIPアドレスとポートに紐づけ
        server.Bind(serverEndPoint);

        // 受信待ちを開始
        server.Listen(100);

        // 接続要求が来た時のコールバック関数を設定
        server.BeginAccept(DoAcceptTcpClientCallback, server);
    }

    // クライアントからの接続処理
    private void DoAcceptTcpClientCallback(IAsyncResult ar)
    {
        Socket listener = (Socket)ar.AsyncState;
        Socket client = null;
        try
        {
            client = listener.EndAccept(ar);
        }
        catch (System.ObjectDisposedException)
        {
            // 閉じた時
            Debug.Log($"[{transform.name}]Closed: " + listener.RemoteEndPoint);
            return;
        }

        lock (listClient)
        {
            listClient.Add(client);
        }
        Debug.Log("Connect: " + client.RemoteEndPoint);
        Debug.Log("ReceiveBufferSize: " + client.ReceiveBufferSize);
        Debug.Log("SendBufferSize: " + client.SendBufferSize);
        
        // 今接続した人からの受信を待つ
        AsyncStateObject so = new AsyncStateObject(client);
        client.BeginReceive(so.ReceiveBuffer,
                            0,
                            so.ReceiveBuffer.Length,
                            SocketFlags.None,
                            DoReceiveMessageCallback,
                            so);

        // 接続が確立したら次の人を受け付ける
        listener.BeginAccept(DoAcceptTcpClientCallback, listener);
    }

    // クライアントからのメッセージ受信処理
    protected override void DoReceiveMessageCallback(IAsyncResult ar)
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
            Debug.Log("Closed: " + so.Socket.RemoteEndPoint);
            lock (listClient)
            {
                listClient.Remove(so.Socket);
            }
            return;
        }

        // クライアントからの読取可能データ量がZEROの場合
        if (len <= 0)
        {
            // クライアントが終了状態と判断し、切断
            Debug.Log($"[{transform.name}]Disconnected: " + so.Socket.RemoteEndPoint);
            listClient.Remove(so.Socket);
            return;
        }

        // 今接続した人とのネットワークストリームを取得
        so.ReceivedData.Write(so.ReceiveBuffer, 0, len);
        if (so.Socket.Available == 0)
        {
            // 最後まで受信した時
            // 受信したデータを文字列に変換
            string str = Encoding.UTF8.GetString(so.ReceivedData.ToArray());
            ChatData c = JsonUtility.FromJson<ChatData>(str);
            c.ClientEndPoint = ((IPEndPoint)so.Socket.RemoteEndPoint).ToString();
            Debug.Log("Received[" + so.Socket.RemoteEndPoint + "] : " + str);
            lock (receivedChats)
            {
                receivedChats.Add(c);
            }
            lock (listClient)
            {
                SendAll(c);
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

    private void SendAll(ChatData chat)
    {
        foreach (var client in listClient)
        {
            Send(chat, client);
        }
    }
}
