using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    [SerializeField]
    private GameObject cube;

    private List<GameObject> players = new List<GameObject>();
    private List<string> spawnIDs = new List<string>();
    private List<string> cullIDs = new List<string>();

    public UdpClient udp;
    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();
        
        udp.Connect("localhost",12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        LOST_CLIENT
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
        public string[] player;
    }
    
    [Serializable]
    public class Player{
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        public string id;
        public receivedColor color;        
    }

    [Serializable]
    public class NewPlayer{
        
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:
                    // Debug.Log(latestMessage.player[0]);   Add new players from list of ids
                    foreach (string id in latestMessage.player)
                    {
                        Debug.Log("Add ID: " + id);
                        spawnIDs.Add(id);
                    }
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.LOST_CLIENT:
                    Debug.Log(latestMessage.player.ToString());
                    foreach (string id in latestMessage.player)
                    {
                        Debug.Log("Cull ID: " + id);
                        cullIDs.Add(id);
                    }
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers(){
        foreach (string id in spawnIDs)
        {
            if (id != null)
            {
                float x = UnityEngine.Random.Range(-10.0f, 10.0f);
                float y = UnityEngine.Random.Range(-10.0f, 10.0f);
                float z = UnityEngine.Random.Range(-10.0f, 10.0f);
                Vector3 spawnPoint = new Vector3(x, y, z);
                GameObject player = Instantiate(cube, spawnPoint, new Quaternion());
                player.GetComponent<IDScript>().address = id;
                players.Add(player);
            }
        }
        spawnIDs.Clear();
    }

    void UpdatePlayers(){
        foreach (GameObject p in players)
        {
            IDScript playID = p.GetComponent<IDScript>();
            foreach (Player p2 in lastestGameState.players)
            {
                if (playID.address == p2.id)
                {
                    playID.color.r = p2.color.R;
                    playID.color.g = p2.color.G;
                    playID.color.b = p2.color.B;

                    p.GetComponent<Renderer>().material.SetColor("_Color", playID.color);
                    break;
                }
            }
        }
    }

    void DestroyPlayers()
    {
        foreach (string id in cullIDs)
        {
            if (id != null)
            {
                foreach (GameObject p in players)
                {
                    if (id == p.GetComponent<IDScript>().address)
                    {
                        players.Remove(p);
                        Destroy(p);
                        break;
                    }
                }
            }
        }
        cullIDs.Clear();
    }

    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
