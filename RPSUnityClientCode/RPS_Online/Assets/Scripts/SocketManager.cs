using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class SocketManager : MonoBehaviour
{
    // ----------------------- VARIABLES ----------------------------------
    public UdpClient udp; //new udp (send/recieve messages from aws) 
    public const string IP_ADRESS = "54.205.115.9"; //the ip the server is connected to..
    public const int PORT = 12345; //port we are using
   private GameplayManager gmScript; //Reference to GameplayManager script
    public ServerMessage latestServerMessage; //last message recieved from the server
    public String lobbyKeyInput = "null"; //user input when trying to connect to a lobby
    [SerializeField] InputField inputTextBox; //reference to lobbykeyinput text input field
    const string characters= "abcdefghijkmnpqrstuvwxyz23456789"; //chars for random lobby id
    private string lobbyString = "null"; //what is the current lobby ID
    private bool heartbeating = false; //has the heartbeat started?


    // ----------------------- FUNCTIONS ----------------------------------
    void Start() 
    {
        gmScript = this.gameObject.GetComponent<GameplayManager>(); //find gamemanager script so you can access it.
    }
    void OnDestroy(){
        //When you close the client destroy UDP that if running
        if(udp != null){
            udp.Dispose();
        }
    }
    public void DestroyUDP(){ //Destroy UDP when you get disconnected and sent back to the menu
        if(udp != null){
            udp.Dispose();
            heartbeating = false; //stop heartbeat
        }
    }
    public void StartButton(){ //Called when you click the start button in game.
        
        udp = new UdpClient(); //create new udp client
        try{
            udp.Connect(IP_ADRESS, PORT); //try to connect to server
        }catch{
            Debug.LogError("Didnt find IP address");
        }
        SendConnectMessage(); //send a connect message to server (also add player to a connect list).
        udp.BeginReceive(new AsyncCallback(OnRecieved), udp);        //wait for server messages...

        gmScript.OutPutMessage("Start Button clicked wait for server!"); //Tell client it was unable to connect (this will be changed almost immediately if it was able to)

        gmScript.ChangeGameState("CONNECTHOST"); //Change game state && show next screen.
        if(!heartbeating){ //If the heart isnt beating start it
            heartbeating = true; //make sure it doesnt start more then once
            InvokeRepeating("HeartBeatMessageToServer", 1, 1);  //send a repeating message to server every second to tell server that client is still connected.
        }
        
    }
    public void BackButtonClick(){ //Called by the back button when it is clicked. Disconnects client and sets lobby string to null
        lobbyString = "null";
        SendConnectMessage();
        gmScript.PlayerDisconnected("disconnectbtn"); //tell client its just the disconnect button and to tell other client to dc
        gmScript.OutPutMessage("Client successfully connected to server! Pick an option."); //set output message

    }
     void OnRecieved(IAsyncResult result){ //Waiting for a message from the server..
        //convert recived async to socket
        UdpClient socket = result.AsyncState as UdpClient;

        //new source obj
        IPEndPoint source = new IPEndPoint(IPAddress.Any, 0 );
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
        //Debug.Log ("sending connect message");
        var payload = new ConnectClientMessage{ //payload is what you are sending to server.
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
    public void SendRestartToServer(){ //send restart message to the server.
        var payload = new RestartServerMessage{
            header = socketMessagetype.RESTART
        };
        var data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
        udp.Send(data, data.Length);
    }
    public void SendDCToServer(){ //send disconnect message to the server.
        var payload = new BaseSocketMessage{
            header = socketMessagetype.DISCONNECT
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
            case socketMessagetype.UPDATELOOP:
                latestServerMessage= JsonUtility.FromJson<ServerMessage>(data); //convert data from base class to result class
                ClientRecievedMessage(); //tell client it is connected to the server/got update message
                break;
            case socketMessagetype.NEWCLIENT:
                ServerMessage newClientPayload= JsonUtility.FromJson<ServerMessage>(data); //convert data from base class to result class
                gmScript.FillClientDetails(newClientPayload.players[0].id, newClientPayload.players[0].lobbyID);//tell client what its own ID is and what lobby it is in
                break;
            case socketMessagetype.DISCONNECT:
                var disconnectPayload = JsonUtility.FromJson<DisconnectPayload>(data); //convert data from base class to result class
                //tell client what type of disconnect message it is
                /*if(disconnectPayload.droppedID != "null"){
                    Debug.Log("the other player disconnected");
                }if(disconnectPayload.droppedID == "null"){
                    Debug.Log("you disconnected");
                }*/
                //tell client to drop id from list
                gmScript.PlayerDisconnected(disconnectPayload.droppedID);
                
                break;
        }

    }
    void ClientRecievedMessage(){ //client got update message
        gmScript.ClientConnectPing(); //update heartbeat / still connected
    }
    void HeartBeatMessageToServer(){ //tell server client is connected every second
        if(!heartbeating){ //stop heart if it is dced from losing connection on client side
            CancelInvoke();
            return;
        }
        
        var payload = new HeartBeatMessage{
            header = socketMessagetype.HEARTBEAT,
        };
        var data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
        udp.Send(data, data.Length);
        gmScript.HeartbeatfromClient();
    }
    public void HostNewLobby(){ //create new random lobby string that will be used to host a new lobby.
        bool m_uniqueLobbyKey = true;
        for (int i = 0; i < latestServerMessage.players.Length; i++)
        {
            if(lobbyString == latestServerMessage.players[i].lobbyID){
                m_uniqueLobbyKey = false;
            }
        }
        if(m_uniqueLobbyKey == false){
            lobbyString = "null";
        }
        if(lobbyString == "null"){
            CreateLobbyKey();
        }else{ 
            ConnectButton(true);
            return;
        }
        HostNewLobby();
    }
    void CreateLobbyKey(){ //Create a random string
        lobbyString = "";
        for(int i=0; i<5; i++)
        {        
             lobbyString += characters[Random.Range(0, characters.Length)];
        }
        Debug.Log(lobbyString);
    }
    public bool IsLobbyReady(string _lobbyID){ //check that lobby is existing and has 2 players
        int temp = 0;
        for (int i = 0; i < latestServerMessage.players.Length; i++)
        {
            if(_lobbyID == latestServerMessage.players[i].lobbyID && latestServerMessage.players[i].hand == "null"){
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
    public bool IsLobbyExistent(string _lobbyID){//check that lobby is existing and has space for another player
        int temp = 0;
        for (int i = 0; i < latestServerMessage.players.Length; i++)
        {
            if(_lobbyID == latestServerMessage.players[i].lobbyID){
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
     public int IsGameOver(string _lobbyID, string _myID){ //check if game is over
        int temp = 0;
        int _opID = 0;
        int _idNum = 0;
        

        //cycle all adresses to find what players havein their hands
        for (int i = 0; i < latestServerMessage.players.Length; i++)
        {
            if(_lobbyID == latestServerMessage.players[i].lobbyID){
                if(latestServerMessage.players[i].hand != "null"){
                    if(latestServerMessage.players[i].id != _myID){
                        _opID = i;
                    }
                    if(latestServerMessage.players[i].id == _myID){
                        _idNum = i;
                        
                    }
                    temp ++;
                }
            }
        }
        Debug.Log("the hands are: " + latestServerMessage.players[_idNum].hand + " " + latestServerMessage.players[_opID].hand);
        if(temp == 0){ //none existent host
            return 0;
        }else if(temp < 2){
            if(latestServerMessage.players[_idNum].hand != "null"){
                gmScript.ShowObjects(latestServerMessage.players[_idNum].hand, "null");
            }
            return 0;
        }else{
            gmScript.ShowObjects(latestServerMessage.players[_idNum].hand, latestServerMessage.players[_opID].hand);
            switch(latestServerMessage.players[_idNum].hand){
            case "rock":
                if(latestServerMessage.players[_opID].hand == "rock"){
                    return 3;
                }else if(latestServerMessage.players[_opID].hand == "paper"){
                    return 2;
                }else if(latestServerMessage.players[_opID].hand == "scissors"){
                    return 1;
                }
                break;
            case "paper":
                if(latestServerMessage.players[_opID].hand == "rock"){
                    return 1;
                }else if(latestServerMessage.players[_opID].hand == "paper"){
                    return 3;
                }else if(latestServerMessage.players[_opID].hand == "scissors"){
                    return 2;
                }
                break;
            case "scissors":
                if(latestServerMessage.players[_opID].hand == "rock"){
                    return 2;
                }else if(latestServerMessage.players[_opID].hand == "paper"){
                    return 1;
                }else if(latestServerMessage.players[_opID].hand == "scissors"){
                    return 3;
                }
                break;

        }
            return 0;
        }
    }

    public void ConnectButton(bool _Host){ //connect to server on button click
    
        if(_Host){
            SendConnectMessage();
            gmScript.SetInLobby(lobbyString);
        }else if(!_Host){
            lobbyKeyInput = inputTextBox.text;
            int temp = 0;
            for (int i = 0; i < latestServerMessage.players.Length; i++)
            {
                if(lobbyKeyInput == latestServerMessage.players[i].lobbyID){
                    temp ++;
                }
            }
            if(temp == 0){ //none existent host
                Debug.Log("Host non-existent");
                gmScript.KeyErrorMessage("Key is not existent");
                gmScript.OutPutMessage("The key ' "+ lobbyKeyInput + " ' does not exist! Please enter a different key and try again.");
                return;
            }else if(temp < 2){
                Debug.Log("room with host .. going to connect!");
                lobbyString = lobbyKeyInput;
                gmScript.SetInLobby(lobbyString);
                SendConnectMessage();
            }else if(temp >= 2){
                Debug.Log("Server is full");
                gmScript.KeyErrorMessage("Server is full");
                gmScript.OutPutMessage("The server you tried to join is full!");
                return;
            }
            
        }

    }
    public void ResetInputField(){ //reset input text box when you leave or dc from a game.
        inputTextBox.text = "";
    }

}

// ----------------------- ENUMS AND CLASSES ----------------------------------

public enum socketMessagetype{
    NONE = 0, //nothing
    CONNECT = 1, //SENT FROM CLIENT TO SERVER
    HAND = 2, //SENT FROM CLIENT TO SERVER 
    CLIENTCONNECTED = 3, //SENT FROM SERVER TO CLIENT
    RESTART = 6, //sent from client to server
    HEARTBEAT = 12, 
    UPDATELOOP = 13,
    NEWCLIENT = 14,
    DISCONNECT = 15,

}

[System.Serializable] class BaseSocketMessage{
    public socketMessagetype header; //enum header. of what its doing
}
[System.Serializable] class ConnectClientMessage: BaseSocketMessage
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

[System.Serializable] public class Player{
    public string id;   
    public string lobbyID; 
    public string hand; 
}


[System.Serializable] public class ServerMessage{
    public socketMessagetype header; //enum header. of what its doing
    public Player[] players;
}
