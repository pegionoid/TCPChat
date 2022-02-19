﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Server : MonoBehaviour
{
    [SerializeField] public InputField _ipaddress;
    [SerializeField] public InputField _port;
    [SerializeField] public Button _listenButton;
    [SerializeField] public ScrollRect ChatLogSpace;
    [SerializeField] public GameObject chatprefab;
    private List<Socket> listClient;
    private List<ChatData> displayedChats;
    private List<ChatData> receivedChats;

    private Socket server = null;
    private bool isListen = false;
    
    //非同期データ受信のための状態オブジェクト
    private class AsyncStateObject
    {
        public Socket Socket;
        public byte[] ReceiveBuffer;
        public MemoryStream ReceivedData;

        public AsyncStateObject(System.Net.Sockets.Socket soc)
        {
            this.Socket = soc;
            this.ReceiveBuffer = new byte[1024];
            this.ReceivedData = new System.IO.MemoryStream();
        }
    }

    void Awake()
    {
        listClient = new List<Socket>();
        displayedChats = new List<ChatData>();
        receivedChats = new List<ChatData>();
        foreach (Transform n in ChatLogSpace.gameObject.transform.Find("Viewport/Content"))
        {
            GameObject.Destroy(n.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //SendAll(Time.frameCount.ToString());
    }

    private void OnGUI()
    {
        
        if(receivedChats.Count > 0)
        {
            GameObject chatlogspace = ChatLogSpace.gameObject;
            GameObject content = chatlogspace.transform.Find("Viewport/Content").gameObject;
            LayoutGroup layoutGroup = content.GetComponent<LayoutGroup>();
            lock (receivedChats)
            {
                foreach (ChatData c in receivedChats)
                {
                    if (displayedChats.Contains(c))
                    {
                        continue;
                    }
                    GameObject chat = Instantiate(chatprefab);
                    chat.transform.GetComponent<Chat>().SetData(c);
                    chat.transform.SetParent(content.transform, false);
                    displayedChats.Add(c);
                }
                receivedChats.Clear();
            }
            layoutGroup.CalculateLayoutInputVertical();
            layoutGroup.SetLayoutVertical();
            StartCoroutine("ScrollToBottom", ChatLogSpace);
        }
        
    }

    private void OnDestroy()
    {
        if(!(server is null))
        {
            server.Shutdown(SocketShutdown.Both);
            server.Close();
            server.Dispose();
            server = null;
        }
    }

    public void OnListenButtonClicked()
    {
        if(server is null)
        {
            _ipaddress.interactable = false;
            _port.interactable = false;
            _listenButton.GetComponentInChildren<Text>().text = "Stop";
            // 指定したポートを開く
            Listen(_ipaddress.text, int.Parse(_port.text));
        }
        else
        {
            server.Close();
            server.Dispose();
            server = null;

            _ipaddress.interactable = true;
            _port.interactable = true;
            _listenButton.GetComponentInChildren<Text>().text = "Listen";
        }
        
    }

    //public void ScrollToBottom(ScrollRect sr)
    //{
    //    Canvas.ForceUpdateCanvases();
    //    sr.gameObject.SetActive(true);
    //    sr.verticalNormalizedPosition = 0f;
    //    sr.verticalScrollbar.value = 0;
    //    Canvas.ForceUpdateCanvases();
    //}

    public IEnumerator ScrollToBottom(ScrollRect sr)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        sr.verticalNormalizedPosition = 0f;
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
            Debug.Log("Closed: " + so.Socket.RemoteEndPoint);
            lock (listClient)
            {
                listClient.Remove(so.Socket);
            }
            return;
        }

        // 今接続した人とのネットワークストリームを取得
        if(len > 0)
        {
            so.ReceivedData.Write(so.ReceiveBuffer, 0, len);
            if (so.Socket.Available == 0)
            {
                // 最後まで受信した時
                // 受信したデータを文字列に変換
                string str = Encoding.UTF8.GetString(so.ReceivedData.ToArray());
                ChatData c = JsonUtility.FromJson<ChatData>(str);
                c.ClientEndPoint = (IPEndPoint)so.Socket.RemoteEndPoint;
                Debug.Log("Received[" + so.Socket.RemoteEndPoint + "] : " + str);
                lock (receivedChats)
                {
                    receivedChats.Add(c);
                }
                //lock(listClient)
                //{
                //    SendAll(str, listClient);
                //}

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

        
    }

    private void SendAll(string message)
    {
        //Debug.Log($"[{transform.name}]SendAll : {message}");
        byte[] messagebyte = Encoding.UTF8.GetBytes(message);
        
        foreach (var client in listClient)
        {
            if (!client.Connected)
            {
                Debug.Log($"[{transform.name}]Disconnected : {client.RemoteEndPoint}");
                lock (listClient)
                {
                    listClient.Remove(client);
                }
                client.Close();
                continue;
            }
            client.BeginSend(messagebyte,
                         0,
                         messagebyte.Length,
                         SocketFlags.None,
                         DoSendMessageCallBack,
                         client);
        }
    }

    private void DoSendMessageCallBack(IAsyncResult ar)
    {
        Socket client = (Socket)ar.AsyncState;

        client.EndSend(ar);
    }
}
