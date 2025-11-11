using System.Collections.Concurrent;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using TMPro; // Para TextMeshPro

public class UdpClientPongFour : MonoBehaviour
{
   UdpClient client;
   Thread receiveThread;
   IPEndPoint serverEP;
  
   public GameObject localPaddle;
   public GameObject paddle1;
   public GameObject paddle2;
   public GameObject paddle3;
   public GameObject paddle4;
   public GameObject ball;
  
   // UI do Placar
   public TextMeshProUGUI scoreText; // Arraste um Text (TMP) do Canvas aqui
   public TextMeshProUGUI playerInfoText; // Arraste outro Text (TMP) aqui
  
   int myId = -1;
  
   // Placar
   private int scoreLeft = 0;
   private int scoreRight = 0;
  
   // Dicionário para armazenar posições dos outros jogadores
   private Dictionary<int, ConcurrentQueue<Vector3>> playerQueues = new Dictionary<int, ConcurrentQueue<Vector3>>();
   private ConcurrentQueue<Vector3> ballPositionsQueue = new ConcurrentQueue<Vector3>();
  
   // Última posição conhecida da bola (para interpolação suave)
   private Vector3 lastBallPosition;
   private Vector3 targetBallPosition;
   private float ballLerpSpeed = 20f; // Velocidade de interpolação da bola
  
   void Start()
   {
       // Inicializa filas para os 4 jogadores
       for (int i = 1; i <= 4; i++)
       {
           playerQueues[i] = new ConcurrentQueue<Vector3>();
       }
      
       client = new UdpClient();
       serverEP = new IPEndPoint(IPAddress.Parse("10.57.1.75"), 5001); // ALTERE PARA O IP DO SERVIDOR
       client.Connect(serverEP);
      
       receiveThread = new Thread(ReceiveData);
       receiveThread.IsBackground = true;
       receiveThread.Start();
      
       byte[] hello = Encoding.UTF8.GetBytes("HELLO");
       client.Send(hello, hello.Length);
      
       // Inicializa posição da bola
       if (ball != null)
       {
           lastBallPosition = ball.transform.position;
           targetBallPosition = ball.transform.position;
       }
      
       UpdateScoreUI();
   }
  
   void Update()
   {
       // Controla APENAS o paddle local
       if (myId != -1 && localPaddle != null)
       {
           float v = Input.GetAxis("Vertical");
           localPaddle.transform.Translate(new Vector3(0, v, 0) * Time.deltaTime * 5);
          
           // Envia posição para o servidor
           string msg = "POS:" + localPaddle.transform.position.x.ToString("F2", CultureInfo.InvariantCulture) + ";" +
                        localPaddle.transform.position.y.ToString("F2", CultureInfo.InvariantCulture);
           byte[] data = Encoding.UTF8.GetBytes(msg);
           client.Send(data, data.Length);
       }
      
       // Atualiza posições dos paddles dos outros jogadores
       UpdateRemotePaddle(1, paddle1);
       UpdateRemotePaddle(2, paddle2);
       UpdateRemotePaddle(3, paddle3);
       UpdateRemotePaddle(4, paddle4);
      
       // Atualiza posição da bola com interpolação mais rápida e precisa
       if (ballPositionsQueue.TryDequeue(out Vector3 ballPos))
       {
           targetBallPosition = ballPos;
       }
      
       if (ball != null)
       {
           // Interpolação mais agressiva para seguir o servidor
           ball.transform.position = Vector3.Lerp(ball.transform.position, targetBallPosition, Time.deltaTime * ballLerpSpeed);
          
           // Se a distância for muito grande, teleporta (evita atrasos grandes)
           float distance = Vector3.Distance(ball.transform.position, targetBallPosition);
           if (distance > 2f)
           {
               ball.transform.position = targetBallPosition;
           }
       }
   }
  
   void UpdateScoreUI()
   {
       if (scoreText != null)
       {
           scoreText.text = $"{scoreLeft}  -  {scoreRight}";
       }
      
       if (playerInfoText != null && myId != -1)
       {
           string team = (myId <= 2) ? "Time Verde (Esquerda)" : "Time Rosa (Direita)";
           string color = (myId <= 2) ? "#00FF00" : "#FF69B4";
           playerInfoText.text = $"<color={color}>Você é o Jogador {myId}\n{team}</color>";
       }
   }
  
   void UpdateRemotePaddle(int id, GameObject paddle)
   {
       if (id == myId || paddle == null) return;
      
       if (playerQueues[id].TryDequeue(out Vector3 pos))
       {
           paddle.transform.position = Vector3.Lerp(paddle.transform.position, pos, Time.deltaTime * 10f);
       }
   }
  
   void ReceiveData()
   {
       IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
       while (true)
       {
           try
           {
               byte[] data = client.Receive(ref remoteEP);
               string msg = Encoding.UTF8.GetString(data);
              
               if (msg.StartsWith("ASSIGN:"))
               {
                   myId = int.Parse(msg.Substring(7));
                   Debug.Log("[Cliente] Recebi ID = " + myId);
                   AssignLocalPaddle();
                   UpdateScoreUI();
               }
               else if (msg.StartsWith("PLAYER:"))
               {
                   string[] parts = msg.Split(':');
                   int id = int.Parse(parts[1]);
                  
                   string[] coords = parts[2].Split(';');
                   float x = float.Parse(coords[0], CultureInfo.InvariantCulture);
                   float y = float.Parse(coords[1], CultureInfo.InvariantCulture);
                  
                   playerQueues[id].Enqueue(new Vector3(x, y, 0));
               }
               else if (msg.StartsWith("BALL:"))
               {
                   string[] coords = msg.Substring(5).Split(';');
                   float x = float.Parse(coords[0], CultureInfo.InvariantCulture);
                   float y = float.Parse(coords[1], CultureInfo.InvariantCulture);
                   ballPositionsQueue.Enqueue(new Vector3(x, y, 0));
               }
               else if (msg.StartsWith("SCORE:"))
               {
                   string[] scores = msg.Substring(6).Split(';');
                   scoreLeft = int.Parse(scores[0]);
                   scoreRight = int.Parse(scores[1]);
                   Debug.Log($"[Cliente] Placar atualizado: {scoreLeft} x {scoreRight}");
                   UpdateScoreUI();
               }
           }
           catch (SocketException ex)
           {
               Debug.LogWarning("Socket encerrado ou erro: " + ex.Message);
               break;
           }
           catch (System.Exception ex)
           {
               Debug.LogError("Erro no cliente: " + ex.Message);
           }
       }
   }
  
   void AssignLocalPaddle()
   {
       switch (myId)
       {
           case 1:
               localPaddle = paddle1;
               break;
           case 2:
               localPaddle = paddle2;
               break;
           case 3:
               localPaddle = paddle3;
               break;
           case 4:
               localPaddle = paddle4;
               break;
       }
      
       Debug.Log($"[Cliente] Controlando Paddle {myId}");
   }
  
   void OnApplicationQuit()
   {
       receiveThread?.Abort();
       client?.Close();
   }
}