using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class TcpServer : MonoBehaviour
{
    [SerializeField] public string _ipaddress;
    [SerializeField] public int _port;
    private List<TcpClient> listClient;

    private TcpListener tcpListener;

    // Start is called before the first frame update
    void Start()
    {
        listClient = new List<TcpClient>();
        // 指定したポートを開く
        Listen(_ipaddress, _port);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // ソケット接続準備、待機
    public void Listen(string host, int port)
    {
        IPAddress ip;

        Debug.Log("ipaddress:" + host + " port:" + port);
        // ホストがIPアドレス形式じゃなかったら終了
        if (!IPAddress.TryParse(host, out ip))
        {
            Debug.Log("host address is invalid.");
            return;
        }

        // IPアドレスとポート番号でTCP通信の受け口（TCPListener）を初期化
        tcpListener = new TcpListener(ip, port);

        // TCP通信の受信待ちを開始
        tcpListener.Start();

        // TCP通信を受信したときのコールバック関数を設定
        tcpListener.BeginAcceptSocket(DoAcceptTcpClientCallback, tcpListener);
    }

    // クライアントからの接続処理
    private void DoAcceptTcpClientCallback(IAsyncResult ar)
    {
        var listener = (TcpListener)ar.AsyncState;
        var client = listener.EndAcceptTcpClient(ar);
        lock(listClient)
        {
            listClient.Add(client);
        }
        Debug.Log("Connect: " + client.Client.RemoteEndPoint);
        Debug.Log("ReceiveBufferSize: " + client.Client.ReceiveBufferSize);
        Debug.Log("SendBufferSize: " + client.Client.SendBufferSize);

        // 接続が確立したら次の人を受け付ける
        listener.BeginAcceptSocket(DoAcceptTcpClientCallback, listener);

        // 今接続した人とのネットワークストリームを取得
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);

        // 接続が切れるまで送受信を繰り返す
        while (client.Connected)
        {
            while (!reader.EndOfStream)
            {
                // 一行分の文字列を受け取る
                var str = reader.ReadLine();
                Byte[] vs = Encoding.UTF8.GetBytes(str);
                Debug.Log("Received[" + client.Client.RemoteEndPoint + "] : " + str);
                foreach(var c in listClient)
                {
                    if (c == client) continue;
                    c.GetStream().Write(vs, 0, vs.Length);
                }
            }

            // 1000μs待って、接続状態が保留中、読取可、切断の場合
            if (client.Client.Poll(1000, SelectMode.SelectRead))
            {
                // かつ、クライアントからの読取可能データ量がZEROの場合
                if (client.Client.Available == 0)
                {
                    // クライアントが終了状態と判断し、切断
                    Debug.Log("Disconnect: " + client.Client.RemoteEndPoint);
                    lock(listClient)
                    {
                        listClient.Remove(client);
                    }
                    client.Close();
                    break;
                }
            }
        }
    }
}
