using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Unity.Networking.Transport;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.UIElements;

[System.Serializable]
public class NetworkMan : MonoBehaviour
{
    [SerializeField]
    private GameObject playerModel;
    public GameObject localPlayer;

    private List<GameObject> players = new List<GameObject>();
    private List<string> spawnIDs = new List<string>();
    private List<string> cullIDs = new List<string>();

    public UdpClient udp;
    public float updatePerSecond = 30;

    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();
        
        udp.Connect("3.22.224.12", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1/updatePerSecond);
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
        public Player[] player;
    }
    
    [Serializable]
    public class Player{
        [Serializable]
        public struct Position{
            public float X;
            public float Y;
            public float Z;
        }
        public int hearbeat;
        public string id;
        public Position position;        
    }

    [Serializable]
    public class Heartbeat
    {        
        public struct Position
        {
            public float X;
            public float Y;
            public float Z;
        }
        public int heartbeat = 1;
        public Position position;
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
                    foreach (Player player in latestMessage.player)
                    {
                        Debug.Log("Add ID: " + player.id);
                        spawnIDs.Add(player.id);
                    }
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.LOST_CLIENT:
                    Debug.Log(latestMessage.player.ToString());
                    foreach (Player player in latestMessage.player)
                    {
                        Debug.Log("Cull ID: " + player.id);
                        cullIDs.Add(player.id);
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
                GameObject player = Instantiate(playerModel, spawnPoint, new Quaternion());
                player.GetComponent<Renderer>().material.SetColor("_Color", new Color(x/10, y/10, z/10));
                player.GetComponent<IDScript>().address = id;
                players.Add(player);
                player.GetComponent<PlayerController>().networkMan = this;
                localPlayer = player;
                Debug.Log("Added New Player with ID: " + id);
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
                    playID.position.x = p2.position.X;
                    playID.position.y = p2.position.Y;
                    playID.position.z = p2.position.Z;

                    p.transform.position = playID.position;
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
                        Debug.Log("Deleted Old Player with ID: " + id);
                        break;
                    }
                }
            }
        }
        cullIDs.Clear();
    }

    void HeartBeat() {
        Heartbeat message = new Heartbeat();
        message.position.X = localPlayer.transform.position.x;
        message.position.Y = localPlayer.transform.position.y;
        message.position.Z = localPlayer.transform.position.z;
        string m = JsonUtility.ToJson(message);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(m);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
