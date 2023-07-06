using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgeLine : MonoBehaviour
{
    float totalTime;
    float prevStartTime;
    float curStartTime;
    float prevConflictTime;
    float curConflicTime;
    AudioSource audioSource;
    private void Start()
    {
        audioSource = GameObject.Find("RealTimeNoteMaker").GetComponent<AudioSource>();
    }
    private void Update()
    {
        //if (audioSource.isPlaying)
        {
            totalTime += Time.deltaTime;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        Note note = other.gameObject.GetComponent<Note>();
        Debug.Assert(note != null, "JudgeLine : note is null");

    
        if (note != null)
        {
            prevStartTime = curStartTime;
            curStartTime = note.startTime;
            prevConflictTime = curConflicTime;
            curConflicTime = totalTime;

            //Debug.Log($"NoteStartTime : {note.startTime}, ConflictTime : {totalTime}, AudioPlayTime : {audioSource.time}");
            //Debug.Log($"Note SurviveTime : {note.surviveTime}, Abs(endTime, startTime : {note.endTime - note.startTime}");
            Debug.Log($"NoteTerm : {curStartTime - prevStartTime}, ConflictTerm : {curConflicTime - prevConflictTime}");
        }
    }
}
