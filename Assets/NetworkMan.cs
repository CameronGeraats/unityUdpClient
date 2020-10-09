using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

/// <summary>
/// A class that takes care of talking to our server
/// </summary>
public class NetworkMan : MonoBehaviour
{
    public UdpClient udp; // an instance of the UDP client
    public GameObject playerGO; // our player object

    public string myAddress; // my address = (IP, PORT)
    public Dictionary<string,GameObject> currentPlayers; // A list of currently connected players
    public List<string> newPlayers, droppedPlayers; // a list of new players, and a list of dropped players
    public GameState lastestGameState; // the last game state received from server
    public ListOfPlayers initialSetofPlayers; // initial set of players to spawn
    
    public MessageType latestMessage; // the last message received from the server


    // Start is called before the first frame update
    void Start()
    {
        // Initialize variables
        newPlayers = new List<string>();
        droppedPlayers = new List<string>();
        currentPlayers = new Dictionary<string, GameObject>();
        initialSetofPlayers = new ListOfPlayers();
        // Connect to the client.
        // All this is explained in Week 1-4 slides
        udp = new UdpClient();
        Debug.Log("Connecting...");
        udp.Connect("54.147.237.83", 12345);
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("SendPosition", 1, 0.033f);
    }

    void OnDestroy(){
        udp.Dispose();
    }

    /// <summary>
    /// A structure that replicates our server color dictionary
    /// </summary>
    [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }

    [Serializable]
    public struct playerPositionOrientation
    {
        public float x;
        public float y;
        public float z;
        public float rx;
        public float ry;
        public float rz;
        public float rw;
    }

    /// <summary>
    /// A structure that replicates our player dictionary on server
    /// </summary>
    [Serializable]
    public class Player{
        public string id;
        public receivedColor color;
        public playerPositionOrientation posOri;
    }


    [Serializable]
    public class ListOfPlayers{
        public Player[] players;

        public ListOfPlayers(){
            players = new Player[0];
        }
    }
    [Serializable]
    public class ListOfDroppedPlayers{
        public string[] droppedPlayers;
    }

    /// <summary>
    /// A structure that replicates our game state dictionary on server
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int pktID;
        public Player[] players;
    }

    /// <summary>
    /// A structure that replicates the mesage dictionary on our server
    /// </summary>
    [Serializable]
    public class MessageType{
        public commands cmd;
    }

    /// <summary>
    /// Ordererd enums for our cmd values
    /// </summary>
    public enum commands{
        PLAYER_CONNECTED,       //0
        GAME_UPDATE,            // 1
        PLAYER_DISCONNECTED,    // 2
        CONNECTION_APPROVED,    // 3
        LIST_OF_PLAYERS,        // 4
    };
    
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        // Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<MessageType>(returnData);
        
        Debug.Log(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.PLAYER_CONNECTED:
                    ListOfPlayers latestPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in latestPlayer.players){
                        newPlayers.Add(player.id);
                    }
                    break;
                case commands.GAME_UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.PLAYER_DISCONNECTED:
                    ListOfDroppedPlayers latestDroppedPlayer = JsonUtility.FromJson<ListOfDroppedPlayers>(returnData);
                    foreach (string player in latestDroppedPlayer.droppedPlayers){
                        droppedPlayers.Add(player);
                    }
                    break;
                case commands.CONNECTION_APPROVED:
                    ListOfPlayers myPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in myPlayer.players){
                        newPlayers.Add(player.id);
                        myAddress = player.id;
                    }
                    break;
                case commands.LIST_OF_PLAYERS:
                    initialSetofPlayers = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    break; 
                default:
                    Debug.Log("Error: " + returnData);
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
        if (newPlayers.Count > 0){
            foreach (string playerID in newPlayers){
                currentPlayers.Add(playerID,Instantiate(playerGO, new Vector3(0,0,0),Quaternion.identity));
                currentPlayers[playerID].name = playerID;
            }
            newPlayers.Clear();
        }
        if (initialSetofPlayers.players.Length > 0){
            Debug.Log(initialSetofPlayers);
            foreach (Player player in initialSetofPlayers.players){
                if (player.id == myAddress)
                    continue;
                currentPlayers.Add(player.id, Instantiate(playerGO, new Vector3(0,0,0), Quaternion.identity));
                currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R, player.color.G, player.color.B);
                currentPlayers[player.id].name = player.id;
            }
            initialSetofPlayers.players = new Player[0];
        }
    }

    void UpdatePlayers(){
        if (lastestGameState.players.Length >0){
            foreach (NetworkMan.Player player in lastestGameState.players){
                string playerID = player.id;
                currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R,player.color.G,player.color.B);
                if(player.id != myAddress)
                currentPlayers[player.id].GetComponent<Transform>().position = new Vector3(player.posOri.x, player.posOri.y, player.posOri.z);
                currentPlayers[player.id].GetComponent<Transform>().rotation = new Quaternion(player.posOri.rx, player.posOri.ry, player.posOri.rz, player.posOri.rw);
            }
            lastestGameState.players = new Player[0];
        }
    }

    void DestroyPlayers(){
        if (droppedPlayers.Count > 0){
            foreach (string playerID in droppedPlayers){
                Debug.Log(playerID);
                Debug.Log(currentPlayers[playerID]);
                Destroy(currentPlayers[playerID].gameObject);
                currentPlayers.Remove(playerID);
            }
            droppedPlayers.Clear();
        }
    }
    
    void SendPosition()
    {
        Transform trans = currentPlayers[myAddress].GetComponent<Transform>();
        playerPositionOrientation pPO = new playerPositionOrientation();
        pPO.x = trans.position.x;
        pPO.y = trans.position.y;
        pPO.z = trans.position.z;
        pPO.rx = trans.rotation.x;
        pPO.ry = trans.rotation.y;
        pPO.rz = trans.rotation.z;
        pPO.rw = trans.rotation.w;

        string json = JsonUtility.ToJson(pPO);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(json);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}