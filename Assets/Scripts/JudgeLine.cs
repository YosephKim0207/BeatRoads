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

    float noteStartTimeConflictDifferSum;
    float deltaTime;
    int conflictNoteCount;
    private void Start()
    {
        audioSource = GameObject.Find("RealTimeNoteMaker").GetComponent<AudioSource>();
    }
    private void Update()
    {
        //if (audioSource.isPlaying)
        {
            totalTime += Time.deltaTime;
            deltaTime = Time.deltaTime;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        Note note = other.gameObject.GetComponent<Note>();
        Debug.Assert(note != null, "JudgeLine : note is null");

    
        if (note != null)
        {
            ++conflictNoteCount;

            note.judgeEnterTime = totalTime;
            prevStartTime = curStartTime;
            curStartTime = note.startTime;
            prevConflictTime = curConflicTime;
            curConflicTime = audioSource.time;


            //Debug.Log($"{note.name}_ startTime : {note.startTime}, endTime : {note.endTime}, NoteTerm : {curStartTime - prevStartTime}, ConflictTerm : {curConflicTime - prevConflictTime}, SurviveTime : {note.surviveTime}");
            //Debug.Log($"{note.name}_ AudioPlayTime : {audioSource.time}, ConflictTime : {totalTime - 3.0f}");
            float absdd = Mathf.Abs(note.startTime - curConflicTime);
            Debug.Log($"{note.name}'s startTime : {note.startTime}, conflicTime : {curConflicTime}, Abs : {absdd}, DeltaTime : {deltaTime}");
            noteStartTimeConflictDifferSum += absdd;
            //Debug.Log($"Abs_ConflictTime, AudioTime : {Mathf.Abs(totalTime - 3.0f - audioSource.time)}, Abs_noteStartTime,ConflicTime {absdd}");
            Debug.Log($"Avr noteStart_Conflict Differ : { noteStartTimeConflictDifferSum / (float)conflictNoteCount}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Note note = other.gameObject.GetComponent<Note>();
        Debug.Assert(note != null, "JudgeLine : note is null");


        if (note != null)
        {
            note.judgeExitTime = totalTime - 3.0f;

            //Debug.Log($"NoteStartTime : {note.startTime}, ConflictTime : {totalTime}, AudioPlayTime : {audioSource.time}");
            //Debug.Log($"Note SurviveTime : {note.surviveTime}, Abs(endTime, startTime : {note.endTime - note.startTime}");
            note.realThroughTime = note.judgeExitTime - note.judgeEnterTime;
            //Debug.Log($"{note.name}_ RealthroughTime : {note.realThroughTime}");
        }
    }
}
