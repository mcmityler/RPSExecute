using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidScript : MonoBehaviour
{

    [SerializeField] Vector3 moveDir;
    [SerializeField] int moveUp = 0;
    [SerializeField] int moveRight = 0;

    // Start is called before the first frame update
    void Start()
    {
        RanomPosition();
    }

    // Update is called once per frame
    void Update()
    {
        if(gameObject.transform.position.x > Screen.width + 250){
            RanomPosition();
        }if(gameObject.transform.position.x < -250){
            RanomPosition();
        }if(gameObject.transform.position.y > Screen.height + 250){
            RanomPosition();
        }if(gameObject.transform.position.y < -250){
            RanomPosition();
        }
        transform.position += moveDir * Time.deltaTime;
    }

    void RanomPosition(){
        int randx = 0;
        int randy = 0;
        if(moveRight == 1){
            randx = Random.Range(-50, -200);
        }else if (moveRight == -1){
            randx = Random.Range(Screen.width +50, Screen.width + 200);
        }
        if(moveUp == 1){
            randy = Random.Range(-50, -200);
        }else if (moveUp == -1){
            randy = Random.Range(Screen.height +50, Screen.height + 200);
        }
        if(randx == 0){
            transform.position = new Vector3(transform.position.x, randy, 0);
        }
        else if(randy == 0){
            transform.position = new Vector3(randx, transform.position.y, 0);
        }else{
            transform.position = new Vector3(randx, randy, 0);
        }
    }
}
