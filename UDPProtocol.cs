using System;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class UDPProtocol : MonoBehaviour, IProtocolUDP
{
    private UdpClient udp;
    private IPEndPoint remoteEndPoint;

    public bool isServerRunning = false;
    public bool isServer = false;
    public bool isConnected = false;

    bool IProtocolUDP.isServer { get => isServer; set => isServer = value; }

    public event Action OnConnected;
    public event Action<string> OnDataReceived;
    public event Action OnClientDisconnected;

    public void StartUDP(string ipAddress, int port)
    {
        if (isServer)
        {
            udp = new UdpClient(port);
            remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        }
        else
        {
            udp = new UdpClient();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        }

        udp.BeginReceive(ReceiveData, null);
        isServerRunning = true;

        if (!isServer)
        {
            SendData("HELLO");
        }
    }

    public void ReceiveData(IAsyncResult result)
    {
        byte[] receivedBytes = udp.EndReceive(result, ref remoteEndPoint);
        string receivedMessage = System.Text.Encoding.UTF8.GetString(receivedBytes);

        if (isServer)
        {
            if (receivedMessage == "HELLO" && !isConnected)
            {
                isConnected = true;
                SendData("WELCOME");
                Debug.Log("Client connected!");
                OnConnected?.Invoke();
            }
            else if (receivedMessage == "DISCONNECT")
            {
                isConnected = false;
                Debug.Log("Client disconnected.");
                OnClientDisconnected?.Invoke();
            }
            else
            {
                OnDataReceived?.Invoke(receivedMessage);
            }
        }
        else
        {
            if (receivedMessage == "WELCOME" && !isConnected)
            {
                isConnected = true;
                Debug.Log("Connected to the server!");
                OnConnected?.Invoke();
            }
            else if (receivedMessage == "KICK")
            {
                Debug.Log("You were kicked by the server.");
                udp.Close();
                isConnected = false;
            }
            else
            {
                OnDataReceived?.Invoke(receivedMessage);
            }
        }

        Debug.Log("Received from client: " + receivedMessage);

        if (udp.Client != null && udp.Client.IsBound)
            udp.BeginReceive(ReceiveData, null);
    }

    public void SendData(string message)
    {
        byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(message);
        udp.Send(sendBytes, sendBytes.Length, remoteEndPoint);
        Debug.Log("Sent to client: " + message);
    }

    public void Disconnect()
    {
        if (!isServer && isConnected)
        {
            SendData("DISCONNECT");
            udp.Close();
            isConnected = false;
            Debug.Log("Disconnected from server.");
        }
    }

    public void KickClient()
    {
        if (isServer && isConnected)
        {
            SendData("KICK");
            Debug.Log("Client has been kicked.");
            isConnected = false;
            OnClientDisconnected?.Invoke();
        }
    }

    public void StopServer()
    {
        if (isServer)
        {
            udp.Close();
            isServerRunning = false;
            Debug.Log("Servidor detenido.");
        }
    }
}