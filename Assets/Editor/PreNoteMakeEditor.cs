using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PreNoteMakeEditor : EditorWindow
{
    AudioClip audioClip;
    string jsonFilePath = "Assets/Resources/Data/NoteJSONData/";
    string extention = ".json";
    int gridNum = 5;
    float beatCheckRatio = 2.0f;
    float minNoteSwitchTime = 0.3f;

    [MenuItem("CustomTools/PreNoteMaker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(PreNoteMakeEditor));
    }

    private void OnGUI()
    {
        GUILayout.Label("Make Note Resources", EditorStyles.boldLabel);
        audioClip = (AudioClip)EditorGUILayout.ObjectField("AudioClip", audioClip, typeof(AudioClip), false);
        gridNum = EditorGUILayout.IntField("Grid Number", gridNum);
        beatCheckRatio = EditorGUILayout.FloatField("Beat Check Ratio", beatCheckRatio);
        minNoteSwitchTime = EditorGUILayout.FloatField("Min Note Switch Time", minNoteSwitchTime);



        if (GUILayout.Button("Make Note", GUILayout.Width(128)))
        {
            Debug.Assert(audioClip != null, "오디오 클립을 넣어주세요");
            if (audioClip == null)
            {
                return;
            }

            MakeNote();


        }
    }

    private void MakeNote()
    {
        Debug.Assert(audioClip != null, "오디오 클립을 넣어주세요");
        string audioClipJsonName = audioClip.name;
        PreNoteMaker preNoteMaker = new PreNoteMaker();
        Debug.Assert(preNoteMaker != null, "NoteMaker 인스턴스 생성 오류");
        //PreNoteMaker preNoteMaker = new PreNoteMaker();
        //Debug.Assert(preNoteMaker != null, "preNoteMaker is null");
        if (preNoteMaker == null)
        {
            return;
        }

        preNoteMaker.SetAudioSourceClip = audioClip;
        preNoteMaker.SetNotePositionGridNumber = gridNum;
        preNoteMaker.SetBeatCheckRatio = beatCheckRatio;
        preNoteMaker.SetMinNoteSwitchTime = minNoteSwitchTime;

        string grid = gridNum.ToString();
        string ratio = beatCheckRatio.ToString();
        string switchTime = minNoteSwitchTime.ToString();
        string path = jsonFilePath + audioClipJsonName + grid + "_" + ratio + "_" + switchTime + extention;
        Debug.Log(path);

        bool RunPreNoteMakerSuccess = preNoteMaker.RunPreNoteMaker(ref path);
        if (RunPreNoteMakerSuccess)
        {
            Debug.Log($"JSON file save to Assets/Resources/Data/NoteJSONData/{audioClipJsonName}");
        }

        Debug.Assert(RunPreNoteMakerSuccess, "RunPreNoteMaker 실패");

        System.GC.Collect();

    }
}
