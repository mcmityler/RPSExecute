using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameplayManager : MonoBehaviour
{
    // ---------------------- VARIABLES ---------------------------
    public enum GameState
    { //Enum to represent game state of client
        START,
        CONNECTHOST,
        LOBBY,
        PICKPHASE,
        VERSEPHASE,
        WINPHASE,
        RESETPHASE
    }
    [SerializeField] private GameState _CurrentGamestate = GameState.START; //what is the current game state of the client.
    private string _myHand = "null"; //my current hand
    private string _opHand = "null"; //opponents current hand.
    [SerializeField] private GameObject _optionPanels; //Rock paper scissors options.
    [SerializeField] private GameObject _winScreen; // Win text and Restart button
    [SerializeField] private GameObject _hostConnectScreen; //connect and host button screen
    [SerializeField] private GameObject _startScreen; //start button screen
    [SerializeField] private GameObject _backQuitObj; //back and quit buttons in the top left
    [SerializeField] private GameObject _lobbyKeyScreenObj; //lobby key and waiting text
    [SerializeField] private GameObject _verseScreenObj; //screen that shows the output of the round
    [SerializeField] private GameObject _ScoreObj; //score panel at bottom
    [SerializeField] private Button _backButtonObj; //back button reference to disable and enable interativeness
    [SerializeField] private GameObject _ConnectHeartbeatObj; //object that shows user if they are connected / when their last heartbeat was
    [SerializeField] private GameObject _connectedText; //reference to text in connect heartbeat obj that says if you are connected
    [SerializeField] private Text _heartbeatText;//reference to text in connect heartbeat obj that says if your last heartbeat time.
    [SerializeField] private GameObject _helpScreenObj; //object that shows user help screen
    SocketManager _socketManager; //reference to socketmanager script
    float _dottimer = 0; //timer for waiting text dot in lobby screen
    int _dotctr = -1; //counter for how many dots on waiting text in lobby screen
    [SerializeField] private Text _winnerText;  //reference to who wins textbox
    [SerializeField] private Text _keyErrorText; //reference to error text above key input textbox
    [SerializeField] private Text _waitingText; //reference to waiting text textbox in the lobby
    [SerializeField] private Text _outputText; //reference to the output textbox
    [SerializeField] private Text _lobbyIDString; //reference to lobby key textbox
    [SerializeField] private Text _scoreText;//reference to score textbox
    [SerializeField] private Sprite[] _rpsImages; //reference to the images that appear on the versus screen
    [SerializeField] private Image _playerImage; //reference to what the image the player has
    [SerializeField] private Image _opImage; //reference to what image the opponent has
    private String _lobbyString = "null"; //what the Current lobby string is 
    private bool _startScreenVisable, _HostConnectScreenVisable, _lobbyScreenVisable, _helpScreenVisable, _optionsPanelVisable, _verseScreenVisable, _winScreenVisable = false; //should you be able to see any of these screens
    private bool _startGame, _startedGame, _isGameComplete, _restartGame = false; //Gameloop variables
    private int _winner = 0; //who won the game or was it a tie
    private string _MyUserID = "null"; //what is my users 'id' or ip address
    private int _myScore, _enemyScore = 0; //how many wins you or your enemy has
    private int _connectedState = 3; //0 = disconnected , 1 = connected, 2 = pending connection * this is for disconnection message
    private bool _connectMessage, _disconnectMessage = false; //disconnect/connect message showing
    private float _heartbeatTimer = 0; //how long since last heart beat / update loop was recieved
    private bool _connectHostStarted = false; //have you tried to start game once already
    bool _showSuccessfullyConnectedText = true; //show successfully connected text in lobby screen



    // ---------------------- FUNCTIONS ---------------------------  
    void Start()
    {
        _socketManager = this.GetComponent<SocketManager>(); //set reference to socketmanagerscript
        _CurrentGamestate = GameState.START; //set current game state to start state
    }
    public void ChangeGameState(string m_gamestate) //Change gamestate of game && Change which screen is currently visable
    {
        Debug.Log("change game state to: " + m_gamestate);
        //Set all screens visability to false
        _startScreenVisable = false;
        _HostConnectScreenVisable = false;
        _lobbyScreenVisable = false;
        _optionsPanelVisable = false;
        _verseScreenVisable = false;
        _winScreenVisable = false;
        switch (m_gamestate)
        {
            case "START":
                _startScreenVisable = true; //make start screen visibility true;
                _CurrentGamestate = GameState.START;//change gamestate
                break;
            case "CONNECTHOST":
                _HostConnectScreenVisable = true; //make hostconnect screen visibility true;
                _CurrentGamestate = GameState.CONNECTHOST; //change gamestate
                KeyErrorMessage(""); //Reset error key text
                _socketManager.ResetInputField(); //Reset input text box 
                break;
            case "LOBBY":
                //Make Lobby screen appear  
                _lobbyScreenVisable = true;
                _CurrentGamestate = GameState.LOBBY;//change gamestate
                break;
            case "PICKPHASE":
                //Make Option screen appear  
                _optionsPanelVisable = true;
                _CurrentGamestate = GameState.PICKPHASE;//change gamestate
                break;
            case "VERSEPHASE":
                //Make Versus screen appear  
                _verseScreenVisable = true;
                _CurrentGamestate = GameState.VERSEPHASE;//change gamestate
                break;
            case "WINPHASE":
                //Make win screen appear  
                _winScreenVisable = true;
                _CurrentGamestate = GameState.WINPHASE;//change gamestate
                break;
            default:
                Debug.Log("GAME STATE NON EXISTENT");
                break;


        }
        _startScreen.SetActive(_startScreenVisable); //disable or enable start screen visability
        _backQuitObj.SetActive(!_startScreenVisable); //show back/quit button if on any screen but start screen
        _backButtonObj.interactable = !_HostConnectScreenVisable; //disable back button if on host/connect screen
        _hostConnectScreen.SetActive(_HostConnectScreenVisable); //disable or enable hostconnect screen visability
        _lobbyKeyScreenObj.SetActive(_lobbyScreenVisable);//disable or enable lobby screen visability
        _optionPanels.SetActive(_optionsPanelVisable); //disable or enable options panel visability
        _ScoreObj.SetActive(_optionsPanelVisable || _winScreenVisable || _verseScreenVisable);//disable or enable score panel visability
        _verseScreenObj.SetActive(_verseScreenVisable || _winScreenVisable); //disable or enable versus screen visability
        _winScreen.SetActive(_winScreenVisable); //disable or enable win screen visability

    }
    public void HelpButton() //triggered from help button on start screen
    {
        //toggle screen visability
        _helpScreenVisable = !_helpScreenVisable;
        _helpScreenObj.SetActive(_helpScreenVisable);
    }
    public void RestartClick()//triggered from restart button on win screen
    {
        _socketManager.SendRestartToServer(); //Send a message to the server telling it you want to restart
        //reset any variables that affect gameloop
        _winner = 0;
        _myHand = "null";
        _opHand = "null";
        _isGameComplete = false;
        _startedGame = false;
        _startGame = false;
        _restartGame = true;
        //change the lobby text from the lobby key to a restart string
        _lobbyIDString.text = "Restarting the game";

    }
    public void QuitGame()//triggered from Quit button on win screen
    {
        Application.Quit(); //close exe application
    }
    public void ClientConnectPing() //tell client it had a connect ping
    {
        _connectMessage = true;
        _disconnectMessage = false;
    }
    public void ClientDisconnectPing() //tell client it had a disconnect ping
    {
        Debug.Log("CLIENT DISCONNECT PING");
        if (_disconnectMessage == false)
        {
            _heartbeatTimer = 15.5f; //set heartbeat 15.5 seconds (how high it would be if it timed out)
        }
        _disconnectMessage = true;
    }
    public void HeartbeatfromClient() //called from socket manager heartbeattoserver. 
    {
        //Tell connect message what state of connection it has... (connected, pending, disconnected)
        if (_connectMessage)
        {
            _connectedState = 1;
            _heartbeatTimer = 0;
            _connectMessage = false;
        }
        if (_heartbeatTimer >= 2 && _heartbeatTimer < 14)
        {
            _connectedState = 2;
        }
        if (_heartbeatTimer >= 15.5 || _disconnectMessage)
        {
            _connectedState = 0;
            ChangeGameState("START");
            _socketManager.DestroyUDP();
            _disconnectMessage = true;
        }
    }
    public void ConnectPing(string m_connected) //change the connection text depending on status
    {
        if (m_connected == "connect")
        {
            _connectedText.GetComponent<Text>().text = "Connected";
            _connectedText.GetComponent<Text>().color = Color.green;

        }
        else if (m_connected == "dc")
        {
            _connectedText.GetComponent<Text>().text = "Disconnected";
            _connectedText.GetComponent<Text>().color = Color.red;
        }
        else if (m_connected == "pending")
        {
            _connectedText.GetComponent<Text>().text = "Pending...";
            _connectedText.GetComponent<Text>().color = Color.yellow;
        }

    }
    public void SetInLobby(string m_lobbyID) //tell gamemanager what the lobby id is
    {
        //change lobby id
        _lobbyString = m_lobbyID;
        _lobbyIDString.text = "The lobby ID is: " + _lobbyString;
        ChangeGameState("LOBBY"); //change to lobby screen
        _dotctr = -1; //reset dot counter on waiting text

    }
    public void FillClientDetails(string m_id, string m_lobbyID)//get what player id u are and what lobby you are in..
    {
        _MyUserID = m_id;
        _lobbyString = m_lobbyID;
        Debug.Log("this client is: " + _MyUserID);
    }
    public void SelectHand(string m_type) //triggered when player chooses rock paper or scissor buttons.
    {
        Debug.Log("chose" + m_type);
        _myHand = m_type; //set hand
        ShowObjects(m_type, "null"); //show your hand and players as null
        OutPutMessage("You selected " + m_type + ". Please wait for your opponent to pick their hand");
        ChangeGameState("VERSEPHASE"); //change game state
        _socketManager.SendHandToServer(m_type); //send your hand to the server

    }
    public void OutPutMessage(string m_text) //set output message in game
    {
        _connectHostStarted = true; //game has started if this has been called
        _outputText.text = m_text;
    }
    public void OutPutMessageAdd(string m_text)//add to output message .. used by waiting lobby
    {
        _outputText.text += m_text;
    }
    public void KeyErrorMessage(string m_text) //set error message in game
    {
        _keyErrorText.text = m_text;
    }
    public void PlayerDisconnected(string m_droppedID) //tell client or server game dced
    {

        if (m_droppedID != "null") //tell server that you dced (through back button)
        {
            _socketManager.SendDCToServer();
            ChangeGameState("CONNECTHOST");
        }
        else //you dced through other means, timeout or opponent left
        {
            //make start button reappear
            ClientDisconnectPing();
        }
        //reset game variables
        _myScore = 0;
        _enemyScore = 0;
        _winner = 0;
        _myHand = "null";
        _opHand = "null";
        _isGameComplete = false;
        _startedGame = false;
        _startGame = false;
        _keyErrorText.text = "";
        _restartGame = false;
        _connectMessage = false;

    }
    public void ShowObjects(string m_myhand, string m_opHand) //show images for versus screen
    {

        switch (m_myhand)
        {
            case "rock":
                _playerImage.sprite = _rpsImages[0];
                break;
            case "paper":
                _playerImage.sprite = _rpsImages[1];
                break;
            case "scissors":
                _playerImage.sprite = _rpsImages[2];
                break;
        }

        switch (m_opHand)
        {
            case "rock":
                _opImage.sprite = _rpsImages[0];
                _opImage.color = Color.white;
                break;
            case "paper":
                _opImage.sprite = _rpsImages[1];
                _opImage.color = Color.white;
                break;
            case "scissors":
                _opImage.sprite = _rpsImages[2];
                _opImage.color = Color.white;
                break;
            case "null":
                //Randomize object!
                _opImage.sprite = _rpsImages[3];
                break;
        }
    }
    void Update()
    {
        if (_CurrentGamestate != GameState.START || _connectHostStarted) //are you on any screen but mains creen or have you started the game before
        {
            //Display connection status
            if (_connectedState == 1)
            {
                ConnectPing("connect");
                if (_CurrentGamestate == GameState.CONNECTHOST && _showSuccessfullyConnectedText)
                {
                    OutPutMessage("Client successfully connected to server! Pick an option."); //set output message
                    _showSuccessfullyConnectedText = false;
                }
            }
            else if (_connectedState == 0)
            {
                ConnectPing("dc");
                OutPutMessage("You disconnected from the server. Press Start to try again");
                _showSuccessfullyConnectedText = true;
            }
            else if (_connectedState == 2)
            {
                ConnectPing("pending");
                _showSuccessfullyConnectedText = true;
            }
            //Add to heartbeat
            _heartbeatTimer += Time.deltaTime;
            _heartbeatText.text = _heartbeatTimer.ToString();
        }
        if (_CurrentGamestate == GameState.LOBBY && !_startGame)//are you in the lobby waiting for setup.
        {
            if (_dotctr == -1)
            { //base waiting message
                OutPutMessage("Waiting for other player");
                _waitingText.text = "Waiting";
                if (_restartGame)
                {
                    OutPutMessage("Restarting, waiting for other player");
                    _waitingText.text = "Restarting";
                }
            }
            _dottimer += Time.deltaTime; //increase timer increment
            if (_dottimer >= 1.5f)
            { //check if you should add a new dot to text
                OutPutMessageAdd(".");
                _waitingText.text += ".";
                _dottimer = 0;
                _dotctr++;
            }
            if (_dotctr >= 3)
            { //check if you should restart dots to 0.
                _dotctr = -1;
            }
            _startGame = _socketManager.IsLobbyReady(_lobbyString); //check if lobby is ready.. if ready (enough players) set true which starts game.
        }
        //----------------------------GAME LOOP-------------------------------
        if (_startGame) //Lobby is ready and has enough players
        {
            if (_startedGame == false) //Has the game been started (one time event)
            {
                _restartGame = false;
                OutPutMessage("Please pick rock, paper, or scissors.");
                _startedGame = true;
                ChangeGameState("PICKPHASE");
            }
            else if (_startedGame && !_isGameComplete)
            {
                if (_winner == 0)
                {
                    _winner = _socketManager.IsGameOver(_lobbyString, _MyUserID);
                    if (_winner == 1)
                    {
                        //win
                        Debug.Log("_winner");
                        _winnerText.text = "You Win!";
                        OutPutMessage("You won the game! Do you want to play again?");
                        _myScore++;
                        _isGameComplete = true;
                    }
                    else if (_winner == 2)
                    {
                        //lose
                        _winnerText.text = "You lost.";
                        OutPutMessage("You lost the game. Do you want to play again?");
                        Debug.Log("You lost.");
                        _enemyScore++;
                        _isGameComplete = true;
                    }
                    else if (_winner == 3)
                    {
                        //tie
                        Debug.Log("tie");
                        _winnerText.text = "It's a tie.";
                        OutPutMessage("You tied the game. Do you want to play again?");
                        _isGameComplete = true;
                    }
                }
            }
            else if (_isGameComplete)
            {
                ChangeGameState("WINPHASE");
            }
        }

        if (_startGame || _restartGame)
        {
            if (_socketManager.IsLobbyExistent(_lobbyString) == false)
            {
                PlayerDisconnected("null");
            }
            _scoreText.text = "You |  " + _myScore.ToString() + " - " + _enemyScore.ToString() + "  | Opponent";
            if (_restartGame)
            { //if you choose to restart send back to pick phase..
                ChangeGameState("LOBBY");
            }
        }

        //END OF UPDATE LOOP
    }







    //END OF SCRIPT
}
