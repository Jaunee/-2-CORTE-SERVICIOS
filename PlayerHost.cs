using System;
using System.Collections.Concurrent;
using UnityEngine;

public class PlayerHost : MonoBehaviour
{
    public UDPProtocol protocolUDP;

    [SerializeField] private GameObject playerPrefab;
    public Transform multiplayerTransform;
    public Transform spawnPosition;

    private ConcurrentQueue<Vector3> positionQueue = new ConcurrentQueue<Vector3>();
    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private bool isPaused = false;

    void Start()
    {
        if (protocolUDP == null)
        {
            Debug.LogError("UDPProtocol reference is missing!");
            return;
        }

        protocolUDP.OnConnected += () => mainThreadActions.Enqueue(PlayerConnection);
        protocolUDP.OnDataReceived += ReceivePosition;
        protocolUDP.OnClientDisconnected += () => mainThreadActions.Enqueue(RemovePlayer);
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();
        }

        if (protocolUDP.isServer)
        {
            if (!isPaused)
            {
                while (positionQueue.TryDequeue(out Vector3 newPosition))
                {
                    if (multiplayerTransform != null)
                        multiplayerTransform.position = newPosition;
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SendPosition();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                protocolUDP.Disconnect();
            }
        }
    }

    void PlayerConnection()
    {
        Debug.Log("Instanciando jugador remoto...");
        multiplayerTransform = Instantiate(playerPrefab, spawnPosition.position, Quaternion.identity).transform;
    }

    void RemovePlayer()
    {
        Debug.Log("Jugador desconectado. Destruyendo objeto.");
        if (multiplayerTransform != null)
        {
            Destroy(multiplayerTransform.gameObject);
            multiplayerTransform = null;
        }
    }

    public void SendPosition()
    {
        if (multiplayerTransform == null)
        {
            Debug.LogError("Jugador no instanciado.");
            return;
        }

        Vector3 position = multiplayerTransform.position;
        string positionData = $"{position.x};{position.y};{position.z}";
        protocolUDP.SendData(positionData);
    }

    public void ReceivePosition(string positionData)
    {
        if (multiplayerTransform == null)
        {
            Debug.LogWarning("Posición recibida, pero el jugador no está instanciado.");
            return;
        }

        try
        {
            string[] values = positionData.Split(';');
            if (values.Length != 3) return;

            float x = float.Parse(values[0]);
            float y = float.Parse(values[1]);
            float z = float.Parse(values[2]);

            positionQueue.Enqueue(new Vector3(x, y, z));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al parsear la posición: {ex.Message}");
        }
    }

    void OnGUI()
    {
        if (protocolUDP.isServer)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Servidor activo: {protocolUDP.isServerRunning}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Cliente conectado: {protocolUDP.isConnected}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Juego en pausa: {isPaused}");
        }
    }

    // 🔘 Métodos públicos para los botones UI

    public void StartProtocol()
    {
        protocolUDP.StartUDP("127.0.0.1", 12345);
    }

    public void StopServerButton()
    {
        protocolUDP.StopServer();
    }

    public void KickClientButton()
    {
        protocolUDP.KickClient();
    }

    public void DisconnectClientButton()
    {
        protocolUDP.Disconnect();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Debug.Log(isPaused ? "Juego en pausa" : "Juego reanudado");
    }
}
