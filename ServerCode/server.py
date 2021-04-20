import socket
import _thread
import threading
import time
from enum import IntEnum
import json
from datetime import datetime


clients_lock = threading.Lock()
clients = {}


class SocketMessageType(IntEnum):
    NONE = 0 #nothing
    CONNECT = 1 #SENT FROM CLIENT TO SERVER
    HAND = 2 #SENT FROM CLIENT TO SERVER 
    GAMERULES = 3 #SENT FROM SERVER TO CLIENT
    RESULT = 4 #SENT FROM SERVER TO CLIENT
    GAMEFULL = 5 #SENT FROM SERVER TO CLIENT
    RESTART = 6 #SENT FROM CLIENT TO SERVER
    SECONDHAND = 7 #Sent from server to client
    PROCESSHANDS = 8
    FINALTURN = 9 
    GETRESULTS = 10
    RESTARTCLIENT = 11
    HEARTBEAT = 12
    UPDATELOOP = 13
    NEWCLIENT = 14
    DISCONNECT = 15

def cleanClients(sock): #check if there are any clients to disconnect
   while True:
      for c in list(clients.keys()):
         if (datetime.now() - clients[c]['lastBeat']).total_seconds() > 5:
            for q in list(clients.keys()):
                if (clients[c]['lobbyID'] == clients[q]['lobbyID'] and c != q):
                    SendDisconnectMessage(sock, c, q)
                    
            print('Dropped Client: ', c)
            clients_lock.acquire()
            del clients[c]
            clients_lock.release()
      time.sleep(1)

def gameLoop(sock):
   while True:
      GameState = {"header": 13, "players": []}
      clients_lock.acquire()
     # print (clients)
      for c in clients:
         player = {}
         player['id'] = str(c)
         player['lobbyID'] = clients[c]['lobbyID']
         player['hand'] = clients[c]['hand']
         GameState['players'].append(player)
      s=json.dumps(GameState)
      #print(s)
      for c in clients:
         sock.sendto(bytes(s,'utf8'), (c[0],c[1]))
      clients_lock.release()
      time.sleep(1)

#listen for messages from server..
def handle_messages(sock: socket.socket):
    print("listening to messages on new thread")
    while True:
        
        data, addr = sock.recvfrom(1024)
        data = str(data.decode("utf-8"))
        data = json.loads(data)

        #print(f'Recieved message from {addr}: {data}')

        #payload = "guess recieved"
        #payload = bytes(payload.encode("utf-8"))
        clients_lock.acquire()
        if(addr in clients): #check if address is already in client list, if so then do something with that client
                if (data['header'] == SocketMessageType.HEARTBEAT): #if header is heartbeat then update heartbeat time.
                    #print(clients[addr]['lastBeat'])
                    clients[addr]['lastBeat'] = datetime.now() #update heartbeat
                    #print(clients[addr]['lastBeat'])
                elif(data['header'] == SocketMessageType.CONNECT):
                    print(clients[addr]['lobbyID'])
                    clients[addr]['lobbyID'] = data['lobbyID']
                elif(data['header'] == SocketMessageType.RESTART):
                    print("this player wants to restart " + str(addr))
                    clients[addr]['hand'] = "null"
                elif(data['header'] == SocketMessageType.HAND):
                    print("got hand from " + str(addr) + " player " + data['hand'])
                    clients[addr]['hand'] = data['hand']
                elif(data['header'] == SocketMessageType.DISCONNECT):
                    print("deleting player lobby/hand:  " + str(addr))
                    clients[addr]['hand'] = "null"
                    clients[addr]['lobbyID'] = "null"

                
        else: # if you arent part of the contact list then do the connection / badheartbeat... 
            if(data['header'] == SocketMessageType.CONNECT): # if they are connecting..
                
                clients[addr] = {} #create new obj
                clients[addr]['lastBeat'] = datetime.now()
                clients[addr]['lobbyID'] = data['lobbyID']
                clients[addr]['hand'] = "null"
                message = {"header": 14,"players":[{"id":str(addr),"lobbyID":str(data['lobbyID']), "hand":str(clients[addr]['hand'])},]} # tell each client connected that a new player joined.
                m = json.dumps(message)
                sock.sendto(bytes(m,'utf8'), addr)
                print(clients[addr]['lobbyID'])
            if (data['header'] == SocketMessageType.HEARTBEAT): #if header is heartbeat then update heartbeat time.
                print(str(addr) + " has already been disconnected")
                SendDisconnectMessage(sock, "null", addr)

        clients_lock.release()
        
        
                


def sendGameRulesToClient(sock: socket.socket, addr):
    #send a json to client.
    payload = {} #dictionary
    payload['header'] = SocketMessageType.GAMERULES #fill in header
    payload = json.dumps(payload).encode('utf-8') #convert obj to json formatted string.
    sock.sendto(bytes(payload), (addr[0], addr[1]))

def sendGameFullMessage(sock: socket.socket, addr):
    #send a json to client.
    payload = {} #dictionary
    payload['header'] = SocketMessageType.GAMEFULL #fill in header
    #payload['guessOptions'] = to_guess_options #add gameoptions.

    payload = json.dumps(payload).encode('utf-8') #convert obj to json formatted string.
    sock.sendto(bytes(payload), (addr[0], addr[1]))

def SendDisconnectMessage(sock: socket.socket, dropped ,addr):
    #send a json to client.
    if(dropped == "null"):
        print('I need to DC: ')
    else: 
        print('Told other client to quit: ')
    payload = {} #dictionary
    payload['header'] = SocketMessageType.DISCONNECT #fill in header
    payload['droppedID'] = dropped
    #payload['guessOptions'] = to_guess_options #add gameoptions.

    payload = json.dumps(payload).encode('utf-8') #convert obj to json formatted string.
    sock.sendto(bytes(payload), (addr[0], addr[1]))

def main():
    PORT = 12345
    print("Starting server.. on PORT: " + str(PORT))
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    s.bind(('',PORT))

    #start new thread for listening to messages
    _thread.start_new_thread(gameLoop, (s,))
    _thread.start_new_thread(handle_messages, (s,))
    #_thread.start_new_thread(cleanClients,(s, ))

    while True:
        time.sleep(1)


if __name__ == '__main__':
    main()


