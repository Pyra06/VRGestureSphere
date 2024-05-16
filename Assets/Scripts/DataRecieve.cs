using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class DataRecieve : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client; 
    public int port = 5052;
    public bool startRecieving = true;
    [HideInInspector]
    public string rData;
    public string lData;
    private int dataCounter = 0;

    public void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (startRecieving)
        {
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            byte[] dataByte = client.Receive(ref anyIP);
            string receivedData = Encoding.UTF8.GetString(dataByte);

            string[] splitData = receivedData.Split(new string[] { "L", "R" }, StringSplitOptions.RemoveEmptyEntries);
            lData = splitData[0].Trim();
            rData = splitData[1].Trim();
            dataCounter = 0;
        }
    }

    private void OnApplicationQuit()
    {
        receiveThread.Abort();
    }
}
