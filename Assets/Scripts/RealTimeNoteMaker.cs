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
    const float noteScaleRatio = 15.0f;

    [SerializeField]
    const float audioStartTime = 3.0f;

    AudioSource audioSource;
    NoteInfos noteInfos;
    NoteInfo noteInfo;
    Vector3 noteObjectPosition = new Vector3();
    Vector3 noteScale = new Vector3();
    float noteMakerZPos;
    int newNoteInfoIdx;
    float prevNoteStartTime;
    float newNOteStartTime;
    float curTime;
    float noteStartTime;
    const float noteMoveTime = 2.0f;
    float prevNoteThroghTime;

    GameObject judgeObject;
    Vector3 judge = new Vector3();


    private void Awake()
    {
        noteInfos = JsonUtility.FromJson<NoteInfos>(jsonAsset.text);
        noteObjectPosition = Vector3.zero;
        noteScale = Vector3.one;
        Camera.main.transform.position = new Vector3(noteInfos.noteInfos[0].noteGridPosition, 5.0f, 0.0f);
        Camera.main.transform.LookAt(this.transform);
        noteMakerZPos = this.transform.position.z;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        noteStartTime = noteInfos.noteInfos[0].startTime;
        Debug.Log(noteStartTime);
    }

    private void Start()
    {
        judgeObject = GameObject.Find("JudgeLine");
    }


    // Update is called once per frame
    void Update()
    {
        curTime += Time.deltaTime;

        if (!audioSource.isPlaying && curTime - audioStartTime > 0 && newNoteInfoIdx < noteInfos.noteInfos.Count)
        {
            Debug.Log("Audio Play");
            audioSource.Play();
            prevNoteStartTime = noteInfos.noteInfos[newNoteInfoIdx].startTime;
        }

        // note가 judge까지 이동하는데 걸리는 시간 == noteMoveTime
        // audio가 start 되기 noteMoveTime 시간 전에 노트가 생성되어야 note.startTime에 노트가 judge에 도착한다
        if (curTime + noteMoveTime > audioStartTime)
        {
            if (newNoteInfoIdx >= noteInfos.noteInfos.Count)
            {
                return;
            }

            if (prevNoteStartTime + prevNoteThroghTime < audioSource.time + noteMoveTime)
            {
                // scale된 노트들이 RealTimeNoteMaker을 넘지 않는 동일선상에 위치하도록 생성 position를 scale 기준으로 보정
                noteInfo = noteInfos.noteInfos[newNoteInfoIdx];
                noteObjectPosition.x = noteInfo.noteGridPosition;
                Vector3 noteObjInitPos = new Vector3(noteInfo.noteGridPosition, 0.0f, noteMakerZPos);

                // TODO GridPos를 input으로 받아서 내부에서 MakerToNoteDist를 구하고 속도를 반환하는 함수 만들 수 있지 않을까?
                // note의 이동 속도 설정
                judge.x = noteObjectPosition.x;
                judge.y = 0.0f;
                judge.z = judgeObject.transform.position.z;
                float NoteMakerToJudgeDist = Vector3.Distance(judge, noteObjInitPos);
                float noteSpeed = (NoteMakerToJudgeDist / noteMoveTime);

                // 노트의 길이 설정
                float noteLength = (noteInfo.endTime - noteInfo.startTime) * noteSpeed;

                // 모든 노트의 시작점이 RealTimeNoteMaker 좌표와 동일선이 되도록 한다 
                noteObjectPosition.z = noteMakerZPos + (noteLength / 2.0f);
                GameObject noteObject = Object.Instantiate(notePrefab, noteObjectPosition, Quaternion.identity);
                //prevNoteStartTime = curTime;
                prevNoteStartTime = noteInfo.startTime + noteMoveTime;
                if (noteObject)
                {
                    noteObject.name = newNoteInfoIdx.ToString();

                    Note note = noteObject.GetComponent<Note>();

                    #region 디버그용 코드
                    note.fullLength = noteLength;
                    note.halfLength = noteLength;
                    note.SetSpeed = noteSpeed;
                    note.endTime = noteInfo.endTime;
                    note.startTime = noteInfo.startTime;
                    note.guessThroughTime = noteLength / noteSpeed;

                    // audioSource의 timeSamples를 이용한 '현재 시간 계산'과 audioSource.time과 차이는 없었음
                    //Debug.Log($"Use timeSaples : {((float)audioSource.timeSamples / (float)audioSource.clip.frequency)}, AudioTime : {audioSource.time}");
                    #endregion

                    noteScale.z = noteLength;
                    noteObject.transform.localScale = noteScale;

                    prevNoteThroghTime = noteLength / noteSpeed;

                    // 노트 오브젝트 풀 사용시 용도
                    //noteObject.SetActive(true);
                    
                    ++newNoteInfoIdx;
                }
            }

        }




        //if (audioSource.time - prevNoteMakeTime > (prevNoteThroghTime))
        //{

        //    prevNoteStartTime = newNoteStartTime;
        //    noteInfo = noteInfos.noteInfos[newNoteInfoIdx];
        //    newNoteStartTime = noteInfo.startTime;

        //    // scale된 노트들이 RealTimeNoteMaker을 넘지 않는 동일선상에 위치하도록 생성 position를 scale 기준으로 보정
        //    noteObjectPosition.x = noteInfo.noteGridPosition;
        //    Vector3 noteObjInitPos = new Vector3(noteInfo.noteGridPosition, 0.0f, noteMakerZPos);
        //    float judgeDist = Vector3.Distance(judge, noteObjInitPos);

        //    float noteSpeed = (judgeDist / noteMoveTime);

        //    //float noteLength = (noteInfo.endTime - noteInfo.startTime) * noteScaleRatio;
        //    //float noteLength = (noteInfo.endTime - noteInfo.startTime) / noteSpeed;
        //    float noteLength = (newNoteStartTime - prevNoteStartTime) * noteSpeed;
        //    //Debug.Log($"{i}'s NoteLength : {noteLength}");
        //    noteObjectPosition.z = noteMakerZPos + (noteLength / 4.0f);
        //    //Debug.Log($"{i}'s z Pos : {noteMakerZPos + (noteLength / 4.0f)}");
        //    GameObject noteObject = Object.Instantiate(notePrefab, noteObjectPosition, Quaternion.identity);
        //    prevNoteMakeTime = audioSource.time;

        //    if (noteObject)
        //    {

        //        judge.x = noteObjectPosition.x;
        //        judge.y = 0.0f;
        //        judge.z = judgeObject.transform.position.z;
        //        //float judgeDist = Vector3.Distance(judge, noteObjectPosition);
        //        //float noteArriveTime = noteInfo.endTime - noteInfo.startTime;
        //        Note note = noteObject.GetComponent<Note>();
        //        note.halfLength = noteLength;
        //        note.SetSpeed = noteSpeed;
        //        note.endTime = noteInfo.endTime;
        //        note.startTime = noteInfo.startTime;

        //        noteScale.z = noteLength / 2.0f;
        //        //Debug.Log($"{i}'s scale : {noteLength / 2.0f}");
        //        noteObject.transform.localScale = noteScale;
        //        prevNoteThroghTime = noteLength / noteSpeed;
        //        curTime = 0.0f;
        //        // 노트 오브젝트 풀 사용시 용도
        //        //noteObject.SetActive(true);

        //        ++newNoteInfoIdx;
        //    }

        //}

    }
}
