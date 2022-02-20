using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
    public void Start()
    {
        
    }

    public void Update()
    {
        
    }

    public void SetData(ChatData data)
    {
        transform.Find("ClientEndPoint").gameObject.GetComponent<Text>().text = data.ClientEndPoint?.ToString();
        transform.Find("Message").gameObject.GetComponent<Text>().text = data.Message;
        transform.Find("TimeStamp").gameObject.GetComponent<Text>().text = DateTimeOffset.FromUnixTimeSeconds(data.TimeStamp).LocalDateTime.ToString("yyyy/MM/dd HH:mm:ss");

    }

    public ChatData GetData()
    {
        String[] endpoint = transform.Find("ClientEndPoint").gameObject.GetComponent<Text>().text.Split(' ');
        IPEndPoint clientEndPoint = new IPEndPoint(long.Parse(endpoint[0]), int.Parse(endpoint[1]));
        String message = transform.Find("Message").gameObject.GetComponent<Text>().text;
        long timestamp = DateTime.Parse(transform.Find("TimeStamp").gameObject.GetComponent<Text>().text).ToBinary();

        return new ChatData(clientEndPoint, message, timestamp);
    }
}

[Serializable]
public class ChatData
{
    public IPEndPoint ClientEndPoint;
    public String Message;
    public long TimeStamp;

    public ChatData(EndPoint clientEndPoint, String message, long timeStamp)
    {
        this.ClientEndPoint = (IPEndPoint)clientEndPoint;
        this.Message = message;
        this.TimeStamp = timeStamp;
    }
}


