using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum MoveDir
{
    Left,
    Right,
    None,
}

enum PlayerState
{
    Idle,
    Moving,
    Fall,
    Dead,
}

public class PlayerMove : MonoBehaviour
{
    // TODO TEMP, 추후 게임 실행시 자동으로 gridNum 정보 가져오도록 하기
    [SerializeField]
    int gameGridNumber = 5;

    [SerializeField]
    float speed = 20.0f;

    int curPlayerGrid;
    Vector3 playerPos;

    PlayerState State;
    MoveDir Dir;
    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateController();
    }

    void Init()
    {
        GameObject judgeLine = GameObject.Find("JudgeLine");
        curPlayerGrid = gameGridNumber / 2;
        playerPos = new Vector3();
        playerPos.x = curPlayerGrid;
        playerPos.y = 1.0f;
        playerPos.z = judgeLine.transform.position.z - transform.localScale.z;
        transform.position = playerPos;
    }

    void UpdateController()
    {
        switch (State)
        {
            case PlayerState.Idle:
                UpdateIdle();
                break;
            case PlayerState.Moving:
                UpdateMoving();
                break;
            case PlayerState.Fall:
                break;
            case PlayerState.Dead:
                break;
        }
    }

    void UpdateIdle()
    {
        UpdateDirInput();

        if(Dir != MoveDir.None)
        {
            State = PlayerState.Moving;
            return;
        }
    }

    void UpdateDirInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Dir = MoveDir.Left;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            Dir = MoveDir.Right;
        }
        else
        {
            Dir = MoveDir.None;
        }
    }

    void UpdateMoving()
    {
        int destGrid = NextPos();
        float dist = (float)destGrid - transform.position.x;
        Debug.Log($"Dist : {dist}");
        // 도착 
        if(Mathf.Abs(dist) < speed * Time.deltaTime)
        {
            playerPos.x = destGrid;
            transform.position = playerPos;
            curPlayerGrid = destGrid;

            State = PlayerState.Idle;
        }
        else
        {
            playerPos.x = transform.position.x + (dist * speed * Time.deltaTime);
            transform.position = playerPos;
            State = PlayerState.Moving;
        }
    }

    int NextPos()
    {
        int nextPlayerGrid;

        if(Dir == MoveDir.Left)
        {
            nextPlayerGrid = Mathf.Max(0, curPlayerGrid - 1);
        }
        else
        {
            nextPlayerGrid = Mathf.Min(gameGridNumber - 1, curPlayerGrid + 1);
        }
        Debug.Log($"Next Grid : {nextPlayerGrid}");

        return nextPlayerGrid;
    }
}
