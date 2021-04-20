using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class SocketManager : MonoBehaviour
{

    public UdpClient udp; //new udp
    public const string IP_ADRESS = "3.224.107.80"; //the ip the server is connected to..
    public const int PORT = 12345; //port we are using

    GamePlayScript gamePlayScript;

    public GameState lastestGameState;

    public String enteredString = "cats";
    [SerializeField] InputField inputTextBox;

    const string letters= "abcdefghijkmnpqrstuvwxyz23456789";
    private string lobbyString = "null";

    private bool heartbeating = false;

    public void HostNewLobby(){ //create new random lobby string that will be used to host a new lobby.
        lobbyString = "";
        for(int i=0; i<5; i++)
        {        
             lobbyString += letters[Random.Range(0, letters.Length)];
        }
        ConnectButton(true);
    }
    void Start()
    {
        gamePlayScript = this.gameObject.GetComponent<GamePlayScript>();
    }
    void OnDestroy(){
        if(udp != null){
            udp.Dispose();
        }
    }
    public bool IsLobbyReady(string _lobbyID){
        int temp = 0;
        for (int i = 0; i < lastestGameState.players.Length; i++)
        {
            if(_lobbyID == lastestGameState.players[i].lobbyID && lastestGameState.players[i].hand == "null"){
                temp ++;
            }
        }
        if(temp == 0){ //none existent host
            return false;
        }else if(temp < 2){
           return false;
        }else if(temp == 2){
            return true;
        }else{
            return false;
        }
    }
     public int IsGameOver(string _lobbyID, string _myID){
        int temp = 0;
        int _opID = 0;
        int _idNum = 0;
        
        for (int i = 0; i < lastestGameState.players.Length; i++)
        {
            if(_lobbyID == lastestGameState.players[i].lobbyID){
                if(lastestGameState.players[i].hand != "null"){
                    if(lastestGameState.players[i].id != _myID){
                        _opID = i;
                    }
                    if(lastestGameState.players[i].id == _myID){
                        _idNum = i;
                        
                    }
                    temp ++;
                }
            }
        }
        Debug.Log("the hands are: " + lastestGameState.players[_idNum].hand + " " + lastestGameState.players[_opID].hand);
        if(temp == 0){ //none existent host
            return 0;
        }else if(temp < 2){
            return 0;
        }else{
            gamePlayScript.ShowObjects(lastestGameState.players[_idNum].hand, lastestGameState.players[_opID].hand);
            switch(lastestGameState.players[_idNum].hand){
            case "rock":
                if(lastestGameState.players[_opID].hand == "rock"){
                    return 3;
                }else if(lastestGameState.players[_opID].hand == "paper"){
                    return 2;
                }else if(lastestGameState.players[_opID].hand == "scissors"){
                    return 1;
                }
                break;
            case "paper":
                if(lastestGameState.players[_opID].hand == "rock"){
                    return 1;
                }else if(lastestGameState.players[_opID].hand == "paper"){
                    return 3;
                }else if(lastestGameState.players[_opID].hand == "scissors"){
                    return 2;
                }
                break;
            case "scissors":
                if(lastestGameState.players[_opID].hand == "rock"){
                    return 2;
                }else if(lastestGameState.players[_opID].hand == "paper"){
                    return 1;
                }else if(lastestGameState.players[_opID].hand == "scissors"){
                    return 3;
                }
                break;

        }
            return 0;
        }
    }

    public void ConnectButton(bool _Host){ //connect to server on button click
    
        if(_Host){
            SendConnectLobbyMessage();
            gamePlayScript.SetInLobby(lobbyString);
        }else if(!_Host){
            enteredString = inputTextBox.text;
            int temp = 0;
            for (int i = 0; i < lastestGameState.players.Length; i++)
            {
                if(enteredString == lastestGameState.players[i].lobbyID){
                    temp ++;
                }
            }
            if(temp == 0){ //none existent host
                Debug.Log("Host non-existent");
                gamePlayScript.KeyMessage("Key is not existent");
                return;
            }else if(temp < 2){
                Debug.Log("room with host .. going to connect!");
                lobbyString = enteredString;
                gamePlayScript.SetInLobby(lobbyString);
                SendConnectLobbyMessage();
            }else if(temp >= 2){
                Debug.Log("Server is full");
                gamePlayScript.KeyMessage("Server is full");
                return;
            }
            
        }

    }
    public void StartButton(){
        udp = new UdpClient(); //create new client
        try{
            udp.Connect(IP_ADRESS, PORT); //try to connect to server
        }catch(Exception e){
            Debug.LogError(e);
        }
        SendConnectMessage(); //send a connect message to server (also add player to a connect list).
        udp.BeginReceive(new AsyncCallback(OnRecieved), udp);        //wait for server messages...
        gamePlayScript.FillMessage("Connected to server", "either click host or connect");
        gamePlayScript.StartPanel();
        if(!heartbeating){
            heartbeating = true;
            InvokeRepeating("HeartBeatMessageToServer", 1, 1);  
        }
    }

    void OnRecieved(IAsyncResult result){ //Waiting for a message from the server..
        //convert recived async to socket
        UdpClient socket = result.AsyncState as UdpClient;

        //new source obj
        IPEndPoint source = new IPEndPoint(0, 0 );
        //get data that was passed // source passed by memory not value
        byte[] message = socket.EndReceive(result, ref source);

        //turn data recieve to string
        string returnData = Encoding.ASCII.GetString(message);

        //start looking for another message
        socket.BeginReceive(new AsyncCallback(OnRecieved), socket);
        
        //handle message you recieved.
        HandleMessagePayload(returnData);
    } 

    
    
    void SendConnectMessage(){ //tell server you have connected.. send connect message
        var payload = new ConnectClientMassage{ //payload is what you are sending to server.
            header = socketMessagetype.CONNECT, //header tells server what type of message it is.
            lobbyID = lobbyString
        };
        var data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload)); //convert payload to transmittable data.(json file)
        udp.Send(data, data.Length); //send data to server you connected to in start func. 
    }
     void SendConnectLobbyMessage(){ //tell server you have connected.. send connect message
        var payload = new ConnectClientMassage{ //payload is what you are sending to server.
            header = socketMessagetype.CONNECT, //header tells server what type of message it is.
            lobbyID = lobbyString
        };
        var data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload)); //convert payload to transmittable data.(json file)
        udp.Send(data, data.Length); //send data to server you connected to in start func. 
    }
    public void SendHandToServer(string _hand){ //send players hand to the server.
        var payload = new HandClientMessage{
            header = socketMessagetype.HAND,
            hand =  _hand
        };
        var data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
        udp.Send(data, data.Length);
    }
    public void SendRestartToServer(){ //send players hand to the server.
        var payload = new RestartServerMessage{
            header = socketMessagetype.RESTART,
        };
        var data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
        udp.Send(data, data.Length);
    }
    public void SendDCToServer(){ //send players hand to the server.
        var payload = new BaseSocketMessage{
            header = socketMessagetype.DISCONNECT,
        };
        var data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
        udp.Send(data, data.Length);
    }
    
    
    void HandleMessagePayload(string data){ //recieved message from server now process it.
        Debug.Log("Got Message: " + data);

        var payload = JsonUtility.FromJson<BaseSocketMessage>(data); //convert data string to base socket class.
        Debug.Log("Got Message: " + payload.header); //got the header message
         //check what type of header it is then convert and do what that payload needs to do.
        switch(payload.header)
        {
            case socketMessagetype.GAMERULES:
                var gameRulesPayload = JsonUtility.FromJson<GameRulesServerMessage>(data); //get game rules from server
                Debug.Log("Received if first turn");
                break;
            case socketMessagetype.UPDATELOOP:
                lastestGameState= JsonUtility.FromJson<GameState>(data); //convert data from base class to result class
                //Debug.Log("client update loop: " + lastestGameState.players[0].id);
                break;
            case socketMessagetype.NEWCLIENT:
                GameState newClientPayload= JsonUtility.FromJson<GameState>(data); //convert data from base class to result class
                //Debug.Log("connect new client " +  newClientPayload.players[0].id);
                gamePlayScript.FillClientDetails(newClientPayload.players[0].id, newClientPayload.players[0].lobbyID);
                
                break;
            case socketMessagetype.DISCONNECT:
                var disconnectPayload = JsonUtility.FromJson<DisconnectPayload>(data); //convert data from base class to result class
                if(disconnectPayload.droppedID != "null"){
                    Debug.Log("the other player disconnected");
                }if(disconnectPayload.droppedID == "null"){
                    Debug.Log("you disconnected");
                }
                gamePlayScript.PlayerDisconnected(disconnectPayload.droppedID);
                //Debug.Log("connect new client " +  newClientPayload.players[0].id);
                //gamePlayScript.FillClientDetails(newClientPayload.players[0].id, newClientPayload.players[0].lobbyID);
                
                break;
        }

    }
    void HeartBeatMessageToServer(){
        var payload = new HeartBeatMessage{
            header = socketMessagetype.HEARTBEAT,
        };
        var data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
        udp.Send(data, data.Length);
    }
    
}

public enum socketMessagetype{
    NONE = 0, //nothing
    CONNECT = 1, //SENT FROM CLIENT TO SERVER
    HAND = 2, //SENT FROM CLIENT TO SERVER 
    GAMERULES = 3, //SENT FROM SERVER TO CLIENT
    RESULT = 4, //SENT FROM SERVER TO CLIENT
    GAMEFULL = 5, //sent from server to client
    RESTART = 6, //sent from client to server
    SECONDHAND = 7, //Sent from server to client
    PROCESSHANDS = 8, //sent from client to server
    FINALTURN = 9, //sent from server to client
    GETRESULTS = 10, //sent from client to server
    RESTARTCLIENT = 11,
    HEARTBEAT = 12, 
    UPDATELOOP = 13,
    NEWCLIENT = 14,
    DISCONNECT = 15,

}
[System.Serializable] class BaseSocketMessage{
    public socketMessagetype header; //enum header. of what its doing
}
[System.Serializable] class GameRulesServerMessage: BaseSocketMessage
{
    public bool firstPlayer; //are you the first player
}
[System.Serializable] class SecondHandMessage: BaseSocketMessage
{
    public string hand; //secondplayers hand
}
[System.Serializable] class ResultServerMessage: BaseSocketMessage
{

}
[System.Serializable] class RestartClientMessage: BaseSocketMessage
{

}
[System.Serializable] class FullServerMessage: BaseSocketMessage
{
   
}
[System.Serializable] class ConnectClientMassage: BaseSocketMessage
{
   public string lobbyID; 
}

[System.Serializable] class RestartServerMessage: BaseSocketMessage
{
   
}
[System.Serializable] class DisconnectPayload: BaseSocketMessage
{
   public string droppedID;
}


[System.Serializable] class HeartBeatMessage: BaseSocketMessage
{
   
}

[System.Serializable] class HandClientMessage: BaseSocketMessage
{
    public string hand; 
}
[System.Serializable] class ProcessClientMessage: BaseSocketMessage
{
    public string hand; 
}
[System.Serializable] class FinalClientMessage: BaseSocketMessage
{
    public string hand; 
}

[System.Serializable]
public class Player{
    public string id;   
    public string lobbyID; 
    public string hand; 
}

[System.Serializable]    
public class NewPlayer{
        
}

[System.Serializable]
public class GameState{
    public socketMessagetype header; //enum header. of what its doing
    public Player[] players;
}