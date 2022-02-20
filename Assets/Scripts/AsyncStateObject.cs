using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//非同期データ受信のための状態オブジェクト
public class AsyncStateObject
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
