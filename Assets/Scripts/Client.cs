using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Client : MonoBehaviour
{
    [SerializeField] public string _serveripaddress;
    [SerializeField] public int _serverport;

    private Socket server;

    //非同期データ受信のための状態オブジェクト
    private class AsyncStateObject
    {
        public Socket Socket;
        public byte[] ReceiveBuffer;
        public MemoryStream ReceivedData;

        public AsyncStateObject(System.Net.Sockets.Socket soc, int buffersize)
        {
            this.Socket = soc;
            this.ReceiveBuffer = new byte[buffersize];
            this.ReceivedData = new System.IO.MemoryStream();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartConnection(_serveripaddress, _serverport);
    }

    // Update is called once per frame
    void Update()
    {
        //Send(Time.frameCount.ToString(), server);
    }

    private void OnDestroy()
    {
        server?.Dispose();
    }

    public void StartConnection(string host, int port)
    {
        IPAddress ip;

        // ホストがIPアドレス形式じゃなかったら終了
        if (!IPAddress.TryParse(host, out ip))
        {
            Debug.Log($"[{transform.name}]server address is invalid.");
            return;
        }

        Debug.Log($"[{transform.name}]ipaddress:" + host + " port:" + port);

        Socket s = new Socket(ip.AddressFamily,
                              SocketType.Stream,
                              ProtocolType.Tcp);

        s.BeginConnect(ip, port, DoConnectTcpClientCallBack, s);
    }

    // サーバへの接続処理
    private void DoConnectTcpClientCallBack(IAsyncResult ar)
    {
        try
        {
            server = (Socket)ar.AsyncState;

            server.EndConnect(ar);

            Debug.Log($"[{transform.name}]Connect: " + server.RemoteEndPoint);
            Debug.Log($"[{transform.name}]ReceiveBufferSize: " + server.ReceiveBufferSize);
            Debug.Log($"[{transform.name}]SendBufferSize: " + server.SendBufferSize);
            
            // サーバからの受信を待つ
            AsyncStateObject so = new AsyncStateObject(server, server.ReceiveBufferSize);
            server.BeginReceive(so.ReceiveBuffer,
                                0,
                                so.ReceiveBuffer.Length,
                                SocketFlags.None,
                                DoReceiveMessageCallback,
                                so);
        } catch (Exception e) {
            Debug.Log($"[{transform.name}]{e.ToString()}");
        }
        
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
        catch (System.ObjectDisposedException)
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
            Debug.Log($"[{transform.name}]Receive : {str}");
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

    private void Send(String message, Socket server)
    {
        if (!server.Connected)
        {
            Debug.Log($"[{transform.name}]server Disconnected");
            return;
        }
        
        byte[] messagebyte = Encoding.UTF8.GetBytes($"({transform.name}){message}");
        server.BeginSend(messagebyte,
                         0,
                         messagebyte.Length,
                         SocketFlags.None,
                         DoSendMessageCallBack,
                         server);
    }

    private void DoSendMessageCallBack(IAsyncResult ar)
    {
        Socket server = (Socket)ar.AsyncState;

        server.EndSend(ar);
    }
}
