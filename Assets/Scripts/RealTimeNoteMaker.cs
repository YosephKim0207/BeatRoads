using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


public class NoteInfos
{
    public List<NoteInfo> noteInfos;
}

public class RealTimeNoteMaker : MonoBehaviour
{
    [SerializeField]
    TextAsset jsonAsset;

    [SerializeField]
    GameObject notePrefab;

    [SerializeField]
    const float noteSwitchJudgeArea = 1.0f;

    [SerializeField]
    const float audioStartTime = 3.0f;

    // TODO TEMP, 추후 게임 실행시 자동으로 gridNum 정보 가져오도록 하기
    const int gridNum = 7;

    // TODO 카메라 높이 조절해가며 적절한 높이 맞춰보기
    const float cameraHeight = 4.0f;

    AudioSource audioSource;
    NoteInfos noteInfos;
    NoteInfo noteInfo;
    GameObject judgeObject;
    float noteMakerToJudgeDist;
    float noteSpeed;
    Vector3 noteObjectPosition = new Vector3();
    Vector3 noteScale = new Vector3();
    float noteMakerZPos;
    int newNoteInfoIdx;
    float newNoteStartTime;
    float curTime;
    const float noteMoveTime = 2.0f;

    //Vector3 judge = new Vector3();


    private void Awake()
    {
        noteInfos = JsonUtility.FromJson<NoteInfos>(jsonAsset.text);
        noteObjectPosition = Vector3.zero;
        noteScale = Vector3.one;
        Camera.main.transform.position = new Vector3(gridNum / 2, cameraHeight, 1.3f);
        Camera.main.transform.LookAt(new Vector3(gridNum/ 2, transform.position.y, transform.position.z));
        noteMakerZPos = this.transform.position.z;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        Init();
    }


    // Update is called once per frame
    void Update()
    {
        curTime += Time.deltaTime;

        // UpdateMakingNote();
    }

    void Init()
    {
        judgeObject = GameObject.Find("JudgeLine");
        noteMakerToJudgeDist = Vector3.Distance(judgeObject.transform.position, this.transform.position);
        noteSpeed = noteMakerToJudgeDist / noteMoveTime;

        MakeNote();
    }

    void UpdateMakingNote()
    {
        // note가 judge까지 이동하는데 걸리는 시간 == noteMoveTime
        // audio가 start 되기 noteMoveTime 시간 전에 노트가 생성되어야 note.startTime에 노트가 judge에 도착한다
        if (curTime + noteMoveTime > audioStartTime)
        {
            if (newNoteInfoIdx >= noteInfos.noteInfos.Count)
            {
                return;
            }

            if (audioSource.time < noteMoveTime)
            {
                if (newNoteStartTime - noteMoveTime < curTime - audioStartTime)
                {
                    //Debug.Log($"{newNoteInfoIdx}'s startTime : {newNoteStartTime}, birthTime : {newNoteStartTime - noteMoveTime}");
                    MakeNote(newNoteInfoIdx);
                    ++newNoteInfoIdx;
                }
            }
            else if (newNoteStartTime - noteMoveTime < audioSource.time)
            {
                //Debug.Log($"{newNoteInfoIdx}'s startTime : {newNoteStartTime}, birthTime : {newNoteStartTime - noteMoveTime}");
                MakeNote(newNoteInfoIdx);
                ++newNoteInfoIdx;
            }
        }
    }

    void MakeNote(int newNoteInfoIdx = -1)
    {
        // noteInfo 설정
        if(newNoteInfoIdx == -1)
        {
            noteInfo = new NoteInfo();
            noteInfo.noteGridPosition = gridNum / 2;

            // TODO 생각해보면 지금 이 endTime과 StartTime 기준으로 노트를 생성하면 RealTimeMaker의 Line에서 출발해서 3.03초 동안 이동하고 JudgeLine에 도착한다는 뜻 아닌가?
            // 그것도 아님 ; 뭐지 ...
            noteInfo.endTime = audioStartTime + noteInfos.noteInfos[newNoteInfoIdx + 1].startTime;
            noteInfo.startTime = 0.0f;
        }
        else
        {
            noteInfo = noteInfos.noteInfos[newNoteInfoIdx];
        }


        // 노트의 길이 설정
        float noteLength;
        if (newNoteInfoIdx == -1)
        {
            noteLength = (noteInfo.endTime - noteInfo.startTime) * noteSpeed;
            // TODO 1.0f == Player PrefabSize, 추후 변수로 변경할 것 
            //noteLength = (noteInfo.endTime - noteInfo.startTime + 1.0f) * noteSpeed;
        }
        else
        {
            noteLength = (noteInfo.endTime - noteInfo.startTime) * noteSpeed;
        }

        // startTime, endTime에 비례해 scale된 노트들이 RealTimeNoteMaker을 넘지 않는 동일선상에 위치하도록 생성 position를 length의 절반만큼 이동
        noteObjectPosition.x = noteInfo.noteGridPosition;
        if(newNoteInfoIdx == -1)
        {
            //noteObjectPosition.z = judgeObject.transform.position.z + (noteLength / 2.0f);

            // TODO 위의 생각해보면 지금 이 endTime과 StartTime 기준으로 노트를 생성하면 RealTimeMaker의 Line에서 출발해서 3.03초 동안 이동하고 JudgeLine에 도착한다는 뜻 아닌가?
            // Test 용도 
            noteObjectPosition.z = transform.position.z + (noteLength / 2.0f);
        }
        else
        {
            noteObjectPosition.z = transform.position.z + (noteLength / 2.0f);
        }
        

        // 모든 노트의 시작점이 RealTimeNoteMaker 좌표와 동일선이 되도록 한다
        //Debug.Log($"{newNoteInfoIdx}'s birth time : {audioSource.time}");
        //Debug.Log($"{newNoteInfoIdx}'s starTime : {noteInfo.startTime}");
        GameObject noteObject = Object.Instantiate(notePrefab, noteObjectPosition, Quaternion.identity);

        if(newNoteInfoIdx == -1)
        {
            newNoteStartTime = noteInfos.noteInfos[0].startTime;
        }
        else
        {
            newNoteStartTime = noteInfo.endTime;
        }
        
        if (noteObject)
        {
            noteObject.name = newNoteInfoIdx.ToString();

            Note note = noteObject.GetComponent<Note>();

            #region 디버그용 코드
            note.fullLength = noteLength;
            note.SetSpeed = noteSpeed;
            note.endTime = noteInfo.endTime;
            note.startTime = noteInfo.startTime;
            note.guessThroughTime = noteLength / noteSpeed;

            // audioSource의 timeSamples를 이용한 '현재 시간 계산'과 audioSource.time과 차이는 없었음
            //Debug.Log($"Use timeSaples : {((float)audioSource.timeSamples / (float)audioSource.clip.frequency)}, AudioTime : {audioSource.time}");
            #endregion

            if(newNoteInfoIdx == -1)
            {
                noteScale.z = noteLength;
                    //+ noteMakerToJudgeDist + noteSwitchJudgeArea;
            }
            else
            {
                noteScale.z = noteLength;
                //+ noteSwitchJudgeArea;
            }

            noteObject.transform.localScale = noteScale;

            // TODO 노트 오브젝트 풀 사용시 용도
            //noteObject.SetActive(true);
        }
    }
}
