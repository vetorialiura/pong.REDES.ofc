using UnityEngine;
using TMPro;

public class Bola : MonoBehaviour
{
    private Rigidbody2D rb;
    private UdpClientTwoClients udpClient;
    private bool bolaLancada = false;

    public int PontoA = 0;
    public int PontoB = 0;
    public TextMeshProUGUI textoPontoA;
    public TextMeshProUGUI textoPontoB;
    public TextMeshProUGUI VitoriaLocal;
    public TextMeshProUGUI VitoriaRemote;

    public float velocidade = 5f;   // Velocidade base da bola
    public float fatorDesvio = 2f;  // Quanto influencia o ponto de contato no ângulo

    void Start()
    {
        
        rb = GetComponent<Rigidbody2D>();
        udpClient = FindObjectOfType<UdpClientTwoClients>();

        if (udpClient != null && udpClient.myId == 2)
        {
            Invoke("LancarBola", 1f);
        }
    }

    void LancarBola()
    {
        float dirX = Random.Range(0, 2) == 0 ? -1 : 1;
        float dirY = Random.Range(-0.5f, 0.5f); // inicia com pequeno ângulo
        rb.linearVelocity = new Vector2(dirX, dirY).normalized * velocidade;
    }

    void Update()
    {
        if (udpClient == null) return;

        if (!bolaLancada && udpClient.myId == 2)
        {
            bolaLancada = true;
            Invoke("LancarBola", 1f);
        }

        if (udpClient.myId == 2)
        {
            string msg = "BALL:" +
                         transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";" +
                         transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture);

            udpClient.SendUdpMessage(msg);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (udpClient == null) return;

        if (col.gameObject.CompareTag("Raquete"))
        {
            // Pega o ponto de contato
            float posYbola = transform.position.y;
            float posYraquete = col.transform.position.y;
            float alturaRaquete = col.collider.bounds.size.y;

            // Calcula diferença (normalizado entre -1 e 1)
            float diferenca = (posYbola - posYraquete) / (alturaRaquete / 2f);

            // Direção X mantém, Y é baseado na diferença
            Vector2 direcao = new Vector2(Mathf.Sign(rb.linearVelocity.x), diferenca * fatorDesvio);
            rb.linearVelocity = direcao.normalized * velocidade;
        }
        else if (col.gameObject.CompareTag("Gol1"))
        {
            PontoB++;
            textoPontoB.text = "Pontos:" + PontoB;
            ResetBola();
        }
        else if (col.gameObject.CompareTag("Gol2"))
        {
            PontoA++;
            textoPontoA.text = "Pontos:" + PontoA;
            ResetBola();
        }
    }

    void ResetBola()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        
        if (PontoA > 10 || PontoB > 10)
        {
            GameOver();
        }
        
        else if (udpClient != null && udpClient.myId == 2)
        {
            Invoke("LancarBola", 1f);

            string msg = "SCORE:" + PontoA + ";" + PontoB;
            udpClient.SendUdpMessage(msg);
        }
        
        

    }

    void GameOver()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        if (PontoA > 10 && udpClient.myId == 1)
        {
            VitoriaLocal.gameObject.SetActive(true);
        }
        else if (PontoA > 10 && udpClient.myId == 2)
        {
            VitoriaRemote.gameObject.SetActive(true);
        }
        else if (PontoB > 10 && udpClient.myId == 1)
        {
            VitoriaRemote.gameObject.SetActive(true);
        }
        else if (PontoB > 10 && udpClient.myId == 2)
        {
            VitoriaLocal.gameObject.SetActive(true);
        }
        
    }
}
