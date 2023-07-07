using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField]
    float speed = 30.0f;
    public float SetSpeed { set { speed = value; } }

    //Rigidbody rigidbody;
    //Camera camera;
    Vector3 dest = new Vector3();
    Vector3 judge = new Vector3();

    // TODO
    bool arriveTest = false;
    public float surviveTime = 0.0f;
    public float endTime;
    public float startTime;
    public float fullLength;
    public float halfLength;
    public float judgeEnterTime;
    public float judgeExitTime;
    public float guessThroughTime;
    public float realThroughTime;

    private void Awake()
    {
        //rigidbody = GetComponent<Rigidbody>();
        //camera = Camera.main;
        dest.y = 0.0f;
        //dest.z = Camera.main.transform.position.z - 100.0f;
        dest.z = Camera.main.transform.position.z;

    }

    private void OnEnable()
    {
        
        dest.x = transform.position.x;
        surviveTime = 0.0f;

    }


    

    // Update is called once per frame
    void Update()
    {
        surviveTime += Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, dest, speed * Time.deltaTime);
        
    }
}
