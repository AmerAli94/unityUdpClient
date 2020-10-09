using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;


public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameObject playerPrefab;  
    private List<GameObject> listOfPlayers;

    //assign new ID to the new player from the list of players 
    GameObject playerObject(string ID)
    {
        foreach(GameObject player in listOfPlayers)
        {
            if (player.GetComponent<PlayerID>().ID == ID)
            {
                return player;
            }
        }
        return null;
    }

    public class JsonMessage
    {
        public string message;
        public Vector3 playerPosition;
    }


    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();

        listOfPlayers = new List<GameObject>();

        udp.Connect("3.12.76.48 ", 12345);

        JsonMessage connect = new JsonMessage();
        connect.message = "connect";

        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(connect));
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 0.03f);
        
    }


    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        DISC_CLIENT,
        ADD_ID
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }

    [Serializable]
    public class UDPClient
    {
        public string ID;
    }
    
    [Serializable]
    public class Player{
        public string id;
        [Serializable] 
        public struct receivedColor{
            public float R, G, B;
        }
        public receivedColor color;
        public Vector3 position;
    }

    [Serializable]
    public class NewPlayer{
        public string id;
        public Vector3 vSpawnPoint;
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    // Class for current Players to keep of connecting/disconnecting players
    [Serializable]
    public class CurrentPlayers
    {
        public NewPlayer[] newPlayers;
    }

    public Message latestMessage;
    public UDPClient info;
    public GameState lastestGameState;
    public CurrentPlayers connectingPlayers;
    public CurrentPlayers disconnectingPlayers;

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
                    connectingPlayers = JsonUtility.FromJson<CurrentPlayers>(returnData);
                    Debug.Log("New Players: " + connectingPlayers.newPlayers);
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.DISC_CLIENT:
                    disconnectingPlayers = JsonUtility.FromJson<CurrentPlayers>(returnData);
                    Debug.Log("Players Left: " + disconnectingPlayers.newPlayers);
                    break;
                case commands.ADD_ID:
                    info = JsonUtility.FromJson<UDPClient>(returnData);
                    Debug.Log("Gain client ID: " + info.ID);
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

        if (connectingPlayers.newPlayers != null)
        {
            foreach (NewPlayer player in connectingPlayers.newPlayers)
            {
                //instantiate Player
                GameObject newPlayer = Instantiate(playerPrefab, player.vSpawnPoint, Quaternion.identity);
                //assign ID to the new player and check if its the client and add movement component to it and add to list
                newPlayer.GetComponent<PlayerID>().ID = player.id;
                //Debug.Log("playerList size: " + listOfPlayers.Count);
                if (newPlayer.GetComponent<PlayerID>().ID == info.ID) {
                 
                    newPlayer.AddComponent<PlayerCube>();
                }
                listOfPlayers.Add(newPlayer);

            }
            // if not set the list to null
            connectingPlayers.newPlayers = null;
        }
    }

    void UpdatePlayers(){
        foreach (Player player in lastestGameState.players) {
            GameObject playerCube = playerObject(player.id);

            if (playerCube != null)  {
                // set the color and position as per server 
                playerCube.GetComponent<MeshRenderer>().material.color = new Color(player.color.R, player.color.G, player.color.B);
                playerCube.transform.position = player.position;
            }
        }
    }

    void DestroyPlayers(){
        if (disconnectingPlayers.newPlayers != null)
        {
            foreach (NewPlayer player in disconnectingPlayers.newPlayers)
            {
                // Get the player cube in the game
                GameObject playerCube = playerObject(player.id);

                if (playerCube != null)
                {
                    // Remove object from the list
                    listOfPlayers.Remove(playerCube);

                    // Destory the actor
                    Destroy(playerCube);

                }
            }

            disconnectingPlayers.newPlayers = null;
        }
    }
  
    void HeartBeat(){
        JsonMessage heartbeat = new JsonMessage();
        heartbeat.message = "heartbeat";
        //check for cube in scene
        GameObject playerCube = playerObject(info.ID);
        //copy the cube position if theres any
        if (playerCube != null) {
            heartbeat.playerPosition = playerCube.transform.position;
        }
        // send to server
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(heartbeat));
        udp.Send(sendBytes, sendBytes.Length);
    }
   
    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}