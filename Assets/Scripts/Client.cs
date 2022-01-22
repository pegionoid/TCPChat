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

    private TcpClient tcpClient;
    
    // Start is called before the first frame update
    void Start()
    {
        StartConnection(_serveripaddress, _serverport);
    }

    // Update is called once per frame
    void Update()
    {
        Send(Time.frameCount.ToString(),tcpClient);
    }

    public void StartConnection(string host, int port)
    {
        IPAddress ip;

        Debug.Log("ipaddress:" + host + " port:" + port);
        // ホストがIPアドレス形式じゃなかったら終了
        if (!IPAddress.TryParse(host, out ip))
        {
            Debug.Log("server address is invalid.");
            return;
        }

        tcpClient = new TcpClient(AddressFamily.InterNetwork);

        tcpClient.BeginConnect(ip, port, DoConnectTcpClientCallBack, tcpClient);
    }

    // サーバへの接続処理
    private void DoConnectTcpClientCallBack(IAsyncResult ar)
    {
        tcpClient = (TcpClient)ar.AsyncState;

        var stream = tcpClient.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);

        // 接続が切れるまで送受信を繰り返す
        while (tcpClient.Connected)
        {
            while (!reader.EndOfStream)
            {
                // 一行分の文字列を受け取る
                var str = reader.ReadLine();
                Byte[] vs = Encoding.UTF8.GetBytes(str);
                Debug.Log("Received[" + tcpClient.Client.RemoteEndPoint + "] : " + str);
                
            }

            //// 1000μs待って、接続状態が保留中、読取可、切断の場合
            //if (server.Client.Poll(1000, SelectMode.SelectRead))
            //{
            //    // かつ、クライアントからの読取可能データ量がZEROの場合
            //    if (server.Client.Available == 0)
            //    {
                    
            //    }
            //}
        }
        // クライアントが終了状態と判断し、切断
        Debug.Log("Disconnect: " + tcpClient.Client.RemoteEndPoint);
    }

    private void Send(String massage, TcpClient server)
    {
        if (!server.Connected)
        {
            //Debug.Log("server Disconnected");
            return;
        }

        using (var stream = server.GetStream())
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine(massage);
            }
        }
    }
}
