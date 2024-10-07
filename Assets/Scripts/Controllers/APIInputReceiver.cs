using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Platformer.Mechanics;
using System.Collections;

public class APIInputReceiver : MonoBehaviour
{
    public int connectionPort = 25001;  // Puerto del servidor
    public PlayerController playerController; // Referencia al PlayerController
    public PlayerFenceController playerFenceController; // Referencia al PlayerFenceController
    public float updateInterval = 1f; // Intervalo de tiempo para realizar la acci�n
    private Thread thread;
    private TcpListener server; // Servidor TCP
    private TcpClient client;   // Cliente TCP
    private bool running;       // Para controlar si el servidor est� activo
    private string receivedInput; // Para almacenar el input recibido

    void Start()
    {
        // Iniciar el servidor en un hilo separado
        thread = new Thread(StartSocketServer);
        thread.IsBackground = true;
        thread.Start();

        // Iniciar el Coroutine para verificar el input
        StartCoroutine(HandleReceivedInput());
    }

    // M�todo para iniciar el servidor de sockets
    void StartSocketServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, connectionPort);
            server.Start();
            Debug.Log("Servidor TCP iniciado en el puerto: " + connectionPort);

            client = server.AcceptTcpClient();
            running = true;

            while (running)
            {
                HandleConnection();
            }
        }
        catch (SocketException ex)
        {
            Debug.LogError("Error de socket: " + ex.Message);
        }
        finally
        {
            server?.Stop();
            Debug.Log("Servidor detenido.");
        }
    }

    // M�todo para manejar la conexi�n y recibir datos del cliente
    void HandleConnection()
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                receivedInput = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("Datos recibidos: " + receivedInput);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error durante la conexi�n: " + ex.Message);
        }
    }

    // Coroutine que revisa si hay input recibido y lo procesa
    IEnumerator HandleReceivedInput()
    {
        while (true)
        {
            if (!string.IsNullOrEmpty(receivedInput))
            {
                Debug.Log("Procesando input: " + receivedInput);
                HandleInput(receivedInput);
                receivedInput = null; // Reiniciar input para la pr�xima iteraci�n
            }

            // Esperar antes de volver a consultar
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // M�todo para manejar el input recibido y aplicar acciones en Unity
    void HandleInput(string input)
    {
        // Si el input recibido es "1", realiza la acci�n correspondiente
        if (input == "1")
        {
            playerController.IncrementSpacebarPressCount();
            playerFenceController.Jump();
        }
        // Si el input recibido es "0", podr�as agregar otras acciones
        else if (input == "0")
        {
            Debug.Log("Recibido input '0', deteniendo acci�n.");
            // Aqu� podr�as definir otra acci�n
        }
        else
        {
            Debug.LogWarning("Input desconocido: " + input);
        }
    }

    // Cuando la aplicaci�n se cierra
    private void OnApplicationQuit()
    {
        running = false;
        thread?.Join();
        client?.Close();
        server?.Stop();
        Debug.Log("Servidor finalizado.");
    }
}