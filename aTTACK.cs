using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LoadTester : MonoBehaviour
{
    [Header("Configuración del servidor")]
    [Tooltip("URL del servidor al que se enviarán las solicitudes")]
    public string serverUrl = "http://172.17.37.103:5005/server/1001204040/0";

    [Header("Parámetros de prueba")]
    [Tooltip("Número total de peticiones que se enviarán")]
    public int numberOfRequests = 10000;

    [Tooltip("Tiempo entre cada solicitud en segundos (por ejemplo, 0.01 = 10ms)")]
    public float intervalBetweenRequests = 0.01f;

    void Start()
    {
        if (!string.IsNullOrEmpty(serverUrl))
        {
            StartCoroutine(SendRequests());
        }
        else
        {
            Debug.LogWarning("Debes ingresar una URL válida del servidor en el Inspector.");
        }
    }

    IEnumerator SendRequests()
    {
        for (int i = 0; i < numberOfRequests; i++)
        {
            StartCoroutine(SendRequest(i));
            yield return new WaitForSeconds(intervalBetweenRequests);
        }
    }

    IEnumerator SendRequest(int id)
    {
        UnityWebRequest request = UnityWebRequest.Get(serverUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Solicitud #{id} falló: {request.error}");
        }
        else
        {
            Debug.Log($"Solicitud #{id} completada: {request.downloadHandler.text}");
        }
    }
}