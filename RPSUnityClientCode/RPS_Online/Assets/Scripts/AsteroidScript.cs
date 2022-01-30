using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidScript : MonoBehaviour
{
    [SerializeField] private GameObject[] _AsteroidObjects; //reference to all asteroid objects
    private List<int> _AsteroidXSpeed = new List<int>(); //speed asteroids are going in X direction
    private List<int> _AsteroidYSpeed = new List<int>(); //speed asteroids are going in Y direction
    private List<int> _AsteroidRotationSpeed = new List<int>(); //speed astoids are rotating
    private bool _asteroidsMoving = false; //are the asteroids moving
    private List<bool> _AsteroidLarge = new List<bool>(); //are the asteroids still large
    public void AsteroidStart(){ //activated on the click of the start button
        if(!_asteroidsMoving){ //makes sure it doesnt do it again if you dc.
            for (int i = 0; i < _AsteroidObjects.Length; i++)
            {   
                randomizeAsteroids(i); //randomize asteroids before moving is true to randomize their starting movement and rotation
            }
            _asteroidsMoving = true; //make the asteroids start moving
        }
    }
    void Update(){
        if(_asteroidsMoving){ //move the positions of the asteroids if they are supposed to be moving
            for (int i = 0; i < _AsteroidObjects.Length; i++)
            { 
                if(_AsteroidLarge[i]){ //move large asteroids 9 times faster than the regular asteroids
                    _AsteroidObjects[i].transform.position += new Vector3(_AsteroidXSpeed[i],_AsteroidYSpeed[i] ,0) * Time.deltaTime * 9;
                    _AsteroidObjects[i].transform.eulerAngles += Vector3.forward * _AsteroidRotationSpeed[i] * Time.deltaTime * 9;
                }  
                else{
                    _AsteroidObjects[i].transform.position += new Vector3(_AsteroidXSpeed[i],_AsteroidYSpeed[i] ,0) * Time.deltaTime;
                    _AsteroidObjects[i].transform.eulerAngles += Vector3.forward * _AsteroidRotationSpeed[i] * Time.deltaTime;
                }

                
            }
        }
    }
    void FixedUpdate(){
        //check if the asteroids are out of bounds and if they are re randomize them back into the game
        if(_asteroidsMoving){ 
            for (int i = 0; i < _AsteroidObjects.Length; i++)
            { 
                if(_AsteroidObjects[i].transform.position.x > Screen.width + 250){
                    randomizeAsteroids(i);
                }if(_AsteroidObjects[i].transform.position.x < -250){
                     randomizeAsteroids(i);
                }if(_AsteroidObjects[i].transform.position.y > Screen.height + 250){
                    randomizeAsteroids(i);
                }if(_AsteroidObjects[i].transform.position.y < -250){
                    randomizeAsteroids(i);
                }

                
            }
        }
    }
    void randomizeAsteroids(int m_asteroidNum){
        if(!_asteroidsMoving){
            //Add asteroid movement, rotation to a list.
            _AsteroidXSpeed.Add(0);
            _AsteroidYSpeed.Add(0);
            _AsteroidRotationSpeed.Add(0);
            _AsteroidLarge.Add(true);
        }
        //make asteroids smaller if they are large and off the screen
        if(_asteroidsMoving && _AsteroidLarge[m_asteroidNum]){ 
            _AsteroidLarge[m_asteroidNum] = false;
            _AsteroidObjects[m_asteroidNum].GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
        }
        //randomize asteroids movement speed
        do{    
            _AsteroidXSpeed[m_asteroidNum] = Random.Range(-100, 100);
        }while(_AsteroidXSpeed[m_asteroidNum] < 3 && _AsteroidXSpeed[m_asteroidNum] > -3);
        do{    
            _AsteroidYSpeed[m_asteroidNum] = Random.Range(-40, 40);
        }while(_AsteroidYSpeed[m_asteroidNum] < 3 && _AsteroidYSpeed[m_asteroidNum] > -3);
        //randomize asteroids rotation speed
        do{    
            _AsteroidRotationSpeed[m_asteroidNum] = Random.Range(-100, 100);
        }while(_AsteroidRotationSpeed[m_asteroidNum] <3 && _AsteroidRotationSpeed[m_asteroidNum] > -3);
        int randx = 0;
        int randy = 0;
        if(_asteroidsMoving){ 
            //randomize asteroids position to optimize it appearing on screen (if moving up and right try and spawn on bottom left area)
            randx = 0;
            randy = 0;
            if(_AsteroidXSpeed[m_asteroidNum] > 1){ //moving right
                randx = Random.Range(-80, Screen.width/2);
            }else if (_AsteroidXSpeed[m_asteroidNum] < -1){ //moving left
                randx = Random.Range(Screen.width +80, Screen.width/2);
            }
            if(_AsteroidYSpeed[m_asteroidNum] > 1){
                if((_AsteroidXSpeed[m_asteroidNum] > 1) && randx < 0){ //moving up, right and spawned left of screen
                    randy = Random.Range(0, Screen.height/2);
                }else if((_AsteroidXSpeed[m_asteroidNum] < -1) && randx > Screen.width){//moving up, left and spawned right of screen
                    randy = Random.Range(0, Screen.height/2);
                }else{
                    randy = Random.Range(-50, -200);
                }
            }else if (_AsteroidYSpeed[m_asteroidNum] < -1){
                if((_AsteroidXSpeed[m_asteroidNum] > 1) && randx < 0){
                    randy = Random.Range(Screen.height , Screen.height - Screen.height/2);//moving down, right and spawned left of screen
                }else if((_AsteroidXSpeed[m_asteroidNum] < -1) && randx > Screen.width){
                    randy = Random.Range(Screen.height , Screen.height - Screen.height/2);//moving down, left and spawned right of screen
                }else{
                    randy = Random.Range(Screen.height +50, Screen.height + 200);
                }
                
            }
            _AsteroidObjects[m_asteroidNum].transform.position = new Vector3(randx, randy, 0); //set to new position
            
        }
    }

}
