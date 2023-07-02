using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealTimeNoteMaker : MonoBehaviour
{

    public float beatCheck = 1.0f;

    AudioSource audioSource;
    float[] curSpectrums;
    float[] prevSpectrums;
    int sampleRate;

    Vector3 originVector;

    public List<float> spectralDifferList;
    public int beatCheckWindow = 50;
    public int windowMidIdx;
    public float beatCheckRatio = 1.6f;

    // TODO TEST
    public GameObject notePrefab;
    public int standardIdx;
    List<float[]> spectrumsList;
    List<float> scaledSpectrumSum = new List<float>();
    float prevTime = 0.0f;
    public float curTime = 0.0f;
    public float minNoteSwitchTime = 0.5f;  // 노트가 변경되는 최소 시간. 이것보다는 노트 변경 주기가 커야 한다
    List<float> totalBeatTime = new List<float>();
    public float length = 100.0f;
    int NotePos = 0;
    public bool playMusic = false;
    Camera camera;


    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if(audioSource == null)
        {
            Debug.LogWarning($"{name} : audio is NULL!");
        }

        curSpectrums = new float[1024];
        prevSpectrums = new float[1024];
        sampleRate = AudioSettings.outputSampleRate;
        originVector = this.transform.localScale;

        spectralDifferList = new List<float>();
        windowMidIdx = beatCheckWindow / 2;


        spectrumsList = new List<float[]>();

        // TODO Test
        camera = GameObject.Find("Main Camera").GetComponent<Camera>();

    }

    // Update is called once per frame
    void Update()
    {
        if (audioSource)
        {   
            curSpectrums.CopyTo(prevSpectrums, 0);
            audioSource.GetSpectrumData(curSpectrums, 0, FFTWindow.BlackmanHarris);

            // TODO Test
            spectrumsList.Add(curSpectrums);
            float sum = 0;
            for(int i = 0; i < curSpectrums.Length; ++i)
            {
                sum += curSpectrums[i];
            }
            scaledSpectrumSum.Add(sum);
            //if (spectrumsList.Count >= beatCheckWindow)
            //{
            //    if (!playMusic)
            //    {
            //        audioSource.Play();
            //        playMusic = true;
            //    }
                
            //    float avrSpectrum = CheckAroundSpectrum(standardIdx, 1024);

            //    if (avrSpectrum * beatCheckRatio < scaledSpectrumSum[standardIdx])
            //    {
                    
            //        if (curTime - prevTime > minNoteSwitchTime)
            //        {
            //            totalBeatTime.Add(curTime);
                        
            //            GameObject go = Instantiate(notePrefab, new UnityEngine.Vector3(standardIdx % 3, 0, curTime * length), UnityEngine.Quaternion.identity);
                        
            //            //Debug.Log(curTime);
            //        }

            //    }

            //    standardIdx++;
            //}

            //transform.localScale = originVector * curSpectrums[4] * 10;

            float increase = SpectrumIncrease();
            spectralDifferList.Add(increase);
            if(spectralDifferList.Count >= beatCheckWindow)
            {
                float check = CheckBeat(windowMidIdx);
                //Debug.Log($"avr Value : {check}");
                //Debug.Log($"cur Value : {increase}");

                if (check < increase)
                {
                    prevTime = curTime;
                    curTime = audioSource.time;
                    //if(curTime - prevTime > minNoteSwitchTime)
                    //{
                        Vector3 prevCamPos = camera.transform.position;
                        Vector3 newCampos = new Vector3(1.0f, 10.0f, curTime * 1.5f);
                        Debug.Log($"{audioSource.time} : BEAT!");
                        GameObject go = Instantiate(notePrefab, new UnityEngine.Vector3(windowMidIdx % 3, 0, curTime * length), UnityEngine.Quaternion.identity);
                        camera.transform.position = Vector3.Lerp(prevCamPos, newCampos, 0.2f);
                        camera.transform.LookAt(new Vector3(1.0f, go.transform.position.y, go.transform.position.z));
                    //}
                }

                windowMidIdx++;
            }

            
        }
    }

    // TODO Test
    float CheckAroundSpectrum(int standardIdx, int spectrumSize)
    {
        int fftSpectrumSize = spectrumSize / 2;
        int windowStartIdx = Mathf.Max(0, standardIdx - beatCheckWindow / 2);
        int windowEndIdx = Mathf.Min(spectralDifferList.Count - 1, standardIdx + beatCheckWindow / 2);

        float sum = 0.0f;
        for (int i = windowStartIdx; i < windowEndIdx; i++)
        {
            sum += scaledSpectrumSum[i];
        }


        return (sum / (windowEndIdx - windowStartIdx));
    }
    void MakeNote()
    {
        float str = 0.0f;
        float ed = 0.0f;
        float length = 0.0f;
        for (int i = 0; i < totalBeatTime.Count - 1; i++)
        {
            str = totalBeatTime[i];
            ed = totalBeatTime[i + 1];
            length += (ed - str) * 3;

            GameObject go = Instantiate(notePrefab, new UnityEngine.Vector3(length, 0, i % 3), UnityEngine.Quaternion.identity);
            //go.transform.localScale = new UnityEngine.Vector3(Mathf.Max(1.0f, (ed - str) * 5), 1, 1);
            go.transform.localScale = new UnityEngine.Vector3(1, 1, 1);
        }
    }

    float SpectrumIncrease()
    {
        //Debug.Log($"CurSpectrum : {curSpectrums[4]}");
        //Debug.Log($"PrevSpectrum : {prevSpectrums[4]}");
        //Debug.Log($"Differ : {curSpectrums[4] - prevSpectrums[4]}");

        float sum = 0.0f;
        for(int i = 0; i < 1024; i++)
        {   
            sum += Mathf.Max(0.0f, (curSpectrums[i] - prevSpectrums[i]));
        }

        //if(sum > 0.0f)
        //{
        //    Debug.Log($"Spectrum Increase : {sum}");
        //}
        

        return sum;
    }

    float CheckBeat(int midIdx)
    {
        int windowStartIdx = Mathf.Max(0, midIdx - beatCheckWindow / 2);
        int windowEndIdx = Mathf.Min(spectralDifferList.Count - 1, midIdx + beatCheckWindow / 2);

        float sum = 0.0f;
        for(int i = windowStartIdx; i < windowEndIdx; i++)
        {
            sum += spectralDifferList[i];
        }

        return (sum / (windowEndIdx - windowStartIdx)) * beatCheckRatio;
    }
}
