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
    float noteScaleRatio = 15.0f;

    AudioSource audioSource;
    NoteInfos noteInfos;
    NoteInfo noteInfo;
    Vector3 noteObjectPosition = new Vector3();
    Vector3 noteScale = new Vector3();
    float noteMakerZPos;
    int prevNoteInfoIdx;
    int newNoteInfoIdx;
    float prevNoteStartTime;
    float newNoteStartTime;
    float curTime = 0.0f;
    float noteStartTime = 0.0f;
    float noteMoveTime = 2.0f;
    float prevNoteThroghTime = 2.0f;
    float prevNoteMakeTime;

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

        // 먼저 노트가 생성된다
        // 노트가 플레이어 위치까지 오는 시점이 비트가 치는 시간이어야 한다
        // JudgeLine 에 충돌판정 만들어서 충돌한 오브젝트 생성 시간이랑 충돌시간 구해서 이동시간이랑 속도 구하기

        curTime += Time.deltaTime;

        //Debug.Log($"curTime{}")
        if (!audioSource.isPlaying && curTime > 2.0f)
        {
            Debug.Log("Audio Play");
            audioSource.Play();
            curTime = 0.0f;
        }

        if (audioSource.time - prevNoteMakeTime > (prevNoteThroghTime))
        {
            if (newNoteInfoIdx >= noteInfos.noteInfos.Count)
            {
                return;
            }
            prevNoteStartTime = newNoteStartTime;
            noteInfo = noteInfos.noteInfos[newNoteInfoIdx];
            newNoteStartTime = noteInfo.startTime;

            // scale된 노트들이 RealTimeNoteMaker을 넘지 않는 동일선상에 위치하도록 생성 position를 scale 기준으로 보정
            noteObjectPosition.x = noteInfo.noteGridPosition;
            Vector3 noteObjInitPos = new Vector3(noteInfo.noteGridPosition, 0.0f, noteMakerZPos);
            float judgeDist = Vector3.Distance(judge, noteObjInitPos);

            float noteSpeed = (judgeDist / noteMoveTime);

            //float noteLength = (noteInfo.endTime - noteInfo.startTime) * noteScaleRatio;
            //float noteLength = (noteInfo.endTime - noteInfo.startTime) / noteSpeed;
            float noteLength = (newNoteStartTime - prevNoteStartTime) * noteSpeed;
            //Debug.Log($"{i}'s NoteLength : {noteLength}");
            noteObjectPosition.z = noteMakerZPos + (noteLength / 4.0f);
            //Debug.Log($"{i}'s z Pos : {noteMakerZPos + (noteLength / 4.0f)}");
            GameObject noteObject = Object.Instantiate(notePrefab, noteObjectPosition, Quaternion.identity);
            prevNoteMakeTime = audioSource.time;

            if (noteObject)
            {

                judge.x = noteObjectPosition.x;
                judge.y = 0.0f;
                judge.z = judgeObject.transform.position.z;
                //float judgeDist = Vector3.Distance(judge, noteObjectPosition);
                //float noteArriveTime = noteInfo.endTime - noteInfo.startTime;
                Note note = noteObject.GetComponent<Note>();
                note.halfLength = noteLength;
                note.SetSpeed = noteSpeed;
                note.endTime = noteInfo.endTime;
                note.startTime = noteInfo.startTime;

                noteScale.z = noteLength / 2.0f;
                //Debug.Log($"{i}'s scale : {noteLength / 2.0f}");
                noteObject.transform.localScale = noteScale;
                prevNoteThroghTime = noteLength / noteSpeed;
                curTime = 0.0f;
                // 노트 오브젝트 풀 사용시 용도
                //noteObject.SetActive(true);

                ++newNoteInfoIdx;
            }

        }

    }
}
