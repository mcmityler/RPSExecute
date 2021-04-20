using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GamePlayScript : MonoBehaviour
{   


    private string myHand = "null"; //my current hand
    private string opHand = "null"; //opponents current hand.
    private bool firstPlayer = false;
    [SerializeField] Text gameText;
    [SerializeField] Text output;
    [SerializeField] GameObject gamePanel;
    [SerializeField] GameObject restartBtn;

    [SerializeField] GameObject hostConnectPanel;
    [SerializeField] GameObject startPanel;
    [SerializeField] GameObject dcText;

    SocketManager socketManager;

    private bool displayWinner = false;

    private string tempString, tempOutput = "";
    private string newString, newOutput = "";

    [SerializeField] Text keyText;

    private bool setPanelActive = false;
    private bool isItActive = false;
    private String lobbyString = "null";

    private bool inLobby, startGame, startedGame, isGameComplete, restartGame = false;
    private int winner = 0;
    private string MyUserID = "null";

    private int myScore = 0;
    private int enemyScore = 0;

    [SerializeField] Text scoreText;
    void Start(){
        socketManager = this.GetComponent<SocketManager>();
    }

    public void SetInLobby(string _lobbyID){
        inLobby = true;
        lobbyString = _lobbyID;
        newString = "The lobby ID is: " + lobbyString;
        HostPanel();
    }

    public void FillClientDetails(string id, string _lobbyID){ //get what player id u are and what lobby you are in..
        MyUserID = id;
        lobbyString = _lobbyID;
        Debug.Log("this client is: " + MyUserID);
    }

    void FixedUpdate(){
        if(inLobby && !startGame){
            Debug.Log("Waiting for lobby to be ready " + MyUserID);
            newOutput = "Waiting for lobby to be ready";
            startGame = socketManager.IsLobbyReady(lobbyString);

            Debug.Log("Waiting for lobby to be ready " + startGame);
        }
        if(startGame){
            
            if(startedGame == false){
                restartGame = false;
                Debug.Log("Make panel visable");
                newString = "Rock, Paper, or Scissors?";
                newOutput = "";
                startedGame = true;
                setPanelActive = true;
            }
            else if(startedGame && !isGameComplete){
                if(winner == 0){
                    winner = socketManager.IsGameOver(lobbyString, MyUserID);
                    if(winner == 1){
                        //win
                        Debug.Log("winner");
                        newString = "You Win!";
                        myScore ++;
                        isGameComplete = true;
                    }else if(winner == 2){
                        //lose
                        newString = "You lost.";
                        Debug.Log("You lost.");
                        enemyScore ++;
                        isGameComplete = true;
                    }else if(winner == 3){
                        //tie
                        Debug.Log("tie");
                        newString = "Its a Tie";
                        isGameComplete = true;
                    }
                }
            }else if(isGameComplete){
                
                displayWinner = true;
            }
        }
        if(displayWinner){
            restartBtn.SetActive(true);
        }else{
            restartBtn.SetActive(false);
        }
        if(tempString != newString){
            tempString = newString;
            gameText.text = tempString;
        }
        if(tempOutput != newOutput){
            tempOutput = newOutput;
            output.text = tempOutput;
        }
        if(isItActive != setPanelActive){
            isItActive = setPanelActive;
            gamePanel.SetActive(setPanelActive);
        }
        if(isHostControlPanelVisable != HostControlPanelVisable){
            isHostControlPanelVisable = HostControlPanelVisable;
            hostConnectPanel.SetActive(isHostControlPanelVisable);
        }
        if(isStartPanelVisable != startPanelVisable){
            isStartPanelVisable = startPanelVisable;
            startPanel.SetActive(isStartPanelVisable);
        }
        if(startGame || restartGame){
            if(socketManager.IsLobbyExistent(lobbyString) == false){
                PlayerDisconnected("null");
            }
            scoreText.text = "You |  "+myScore.ToString()+" - "+enemyScore.ToString()+"  | Opponent";
        }else{
            scoreText.text = "";
        }
        if(showingImage != changeImage){
            showingImage = changeImage;
            playerImage.enabled = showingImage;
            opImage.enabled = showingImage;
            vsText.SetActive(showingImage);
        }
        
    }
    public void SelectHand(string _type){
        myHand = _type;
        newString = "Waiting..!";
        setPanelActive = false;
        socketManager.SendHandToServer(_type);
        
    }
    public void QuitGame(){
        Application.Quit();
    }
    public void FillMessage(string _bigText, string _output){
        newString = _bigText;
        newOutput = _output;
    }
    public void FullMessage(){
        newString = "Game is full";
         newOutput = "Game is full";
    }
    public void KeyMessage(string _text){
        keyText.text = _text;
    }
    public void RestartClick(){
        socketManager.SendRestartToServer();
        displayWinner = false;
        winner = 0;
        myHand = "null";
        opHand = "null";
        isGameComplete = false;
        startedGame = false;
        startGame = false;
        newString = "Restarting..";
        changeImage = false;
        restartGame = true;
        
    }
    bool HostControlPanelVisable = true;
    bool isHostControlPanelVisable = true;
     bool startPanelVisable = true;
    bool isStartPanelVisable = true;
    public void HostPanel(){
        HostControlPanelVisable = !HostControlPanelVisable;
    }public void StartPanel(){
        startPanelVisable = !startPanelVisable;
    }

    public void PlayerDisconnected(string _droppedID){
        if(_droppedID != "null"){
            socketManager.SendDCToServer();
            dcText.SetActive(true);
            
            newString = "Player dced..";
        }else{
            //make start button reappear
            Debug.Log("make start button appear");
            
            newString = "You dced..";
            


        }
        if(_droppedID != "disconnectbtn"){
            startPanelVisable = true;
        }
        myScore = 0;
        enemyScore = 0;
        HostControlPanelVisable = true;
        displayWinner = false;
        setPanelActive = false;
        winner = 0;
        myHand = "null";
        opHand = "null";
        isGameComplete = false;
        startedGame = false;
        startGame = false;
        inLobby = false;
        keyText.text = "";
        changeImage = false;
        restartGame = false;
    }
    [SerializeField] Sprite[] rpsImages;
    [SerializeField] Image playerImage;
    [SerializeField] Image opImage;
     [SerializeField] GameObject vsText;
    bool changeImage = false;
    bool showingImage = false;
    public void ShowObjects(string _myhand, string _opHand){
        switch(_myhand){
            case "rock":
                playerImage.sprite = rpsImages[0];
                break;
            case "paper":
                playerImage.sprite = rpsImages[1];
                break;
            case "scissors":
                playerImage.sprite = rpsImages[2];
                break;
        }
        switch(_opHand){
            case "rock":
                opImage.sprite = rpsImages[0];
                break;
            case "paper":
                opImage.sprite = rpsImages[1];
                break;
            case "scissors":
                opImage.sprite = rpsImages[2];
                break;
        }
        changeImage = true;
    }
    
}
