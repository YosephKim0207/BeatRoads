using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;
using System.Threading;
using DSPLib;

// 230701 TODO
// 일정 시간 혹은 프레임 동안의 각 음역대별 평균 값을 구해 저장한다
// 갑자기 튀는 음역대가 있다면 해당 분을 비트로 간주한다
// 일단 비트에 대한 사전적 정의를 보고 audiacity로 비트 구분을 전체 볼륨으로 하는, 특정 음역대가 튀는걸로 하는지 확인해보기
// 각 음역대별 평균이랑 스펙트럼당 평균 다 구해보고 비교 / 연구해보기


public class PreNoteMaker : MonoBehaviour
{
    public GameObject notePrefab;
    // 비트 판단을 위한 변수들
    public int beatCheckWindowSize = 50;    // 해당 스펙트럼 기준 앞뒤로 비교할 스펙트럼의 개수
    public float beatCheckRatio = 2.0f;
    // TODO
    // bpm에 따라 minNoteSwitchTime이 변하도록 하기
    public float minNoteSwitchTime = 0.3f;  // 노트가 변경되는 최소 시간. 이것보다는 노트 변경 주기가 커야 한다
    public int noteDecisionWindowSize = 3;

    public int notePositionGridNumber = 3;


    AudioSource audioSource;

    int spectrumSize = 1024;
    float[] curSpectrumChunk;
    float[] prevSpectrumChunk;
    List<SpectralFluxInfo> totalSpectrumChunk = new List<SpectralFluxInfo>();
    public List<SpectralFluxInfo> totalBeat = new List<SpectralFluxInfo>();
    public List<NoteInfo> totalNote = new List<NoteInfo>();

    Camera camera;
    int noteIdx = 0;
    float EPSILON = 0.001f;

    //TODO
    float time = 0.0f;
    bool convertFinish = false;

    // 구조체로 만들면 스택오버플로우 날까?
    public class SpectralFluxInfo
    {
        public float time;
        public float spectralFlux;
        public float beatStandard;
        public float prunedSpectralFlux; // 생략가능
        public bool isPeak;
    }

    public class NoteInfo
    {
        public float startTime;
        public float endTime;
        public int noteGridPosition;
    }

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        camera = GameObject.Find("Main Camera").GetComponent<Camera>();

        if (audioSource)
        {
            int clipChannels = audioSource.clip.channels;
            int sampleCount = audioSource.clip.samples;
            int sampleRate = audioSource.clip.frequency;

            float[] audioSamplesByChannel = new float[sampleCount * clipChannels];
            curSpectrumChunk = new float[spectrumSize];
            prevSpectrumChunk = new float[spectrumSize];

            audioSource.clip.GetData(audioSamplesByChannel, 0);

            // audioSamplesByChannel에는 각 채널에 대한 샘플이 모두 저장 
            // 따라서 스테레오 채널인 경우 샘플을 하나로 합치는 작업이 필요하다
            float[] audioSamples = new float[sampleCount];
            ConvertAudioSampesByChannel(ref audioSamples, ref audioSamplesByChannel, clipChannels);

            // 실시간 처리에는 AudioSource.GetSpectrumData를 통해 퓨리에 변환을 진행, 음역대를 추출할 수 있다
            // 하지만 유니티에는 전체 샘플에 대해 퓨리에 변환을 수행할 수 있는 함수가 없다
            // 따라서 외부 함수 - DSPLib - 를 사용한다
            double[] sampleChunk = new double[spectrumSize];
            //Thread thread = new Thread(() => DoFFT(ref audioSamples, ref sampleChunk, sampleRate, spectrumSize));
            //thread.Start();
            DoFFT(ref audioSamples, ref sampleChunk, sampleRate);
            int beatCheckWindowSize = 50;
            ExtractBeat(beatCheckWindowSize);
            MakeNote();
        }
    }

    void Update()
    {
        if (convertFinish)
        {
            float playTime = audioSource.time;
            if (totalNote.Count > noteIdx)
            {
                NoteInfo noteInfo = totalNote[noteIdx];
                //Debug.Log($"playTime : {playTime}, noteTime : {noteInfo.startTime}");
                //Debug.Log($"Abs : {Mathf.Abs(noteInfo.startTime - playTime)}");
                

                if (noteInfo.startTime < playTime)
                {
                    MakeNoteObj(playTime, ref noteInfo);
                    ++noteIdx;
                }
                //time += Time.deltaTime;
                //if (time > 0.5f)
                {
                    
                    //time = 0.0f;
                }
            }
        }
    }

    void ConvertAudioSampesByChannel(ref float[] audioSamples, ref float[] audioSamplesByChannel, int clipChannels)
    {
        if (clipChannels > 1)
        {
            float steroToMono = 0.0f;
            int j = 0;
            for (int i = 0; i < audioSamplesByChannel.Length; i++)
            {
                steroToMono += audioSamplesByChannel[i];

                if (i % clipChannels == 1)
                {
                    audioSamples[j] = steroToMono;
                    j++;
                    steroToMono = 0.0f;
                }
            }
        }
        else
        {
            audioSamplesByChannel.CopyTo(audioSamples, 0);
        }
    }

    float GetTimeFromIndex(int index, int sampleRate, int spectrumSize)
    {
        return ((1f / (float)sampleRate) * index) * spectrumSize;
    }

    void DoFFT(ref float[] audioSamples, ref double[] sampleChunk, int sampleRate)
    {
           
        int totalSampleCount = audioSamples.Length / spectrumSize;
        FFT fft = new FFT();
        fft.Initialize((uint)spectrumSize);

        // 아래의 출처로부터 참고
        // https://medium.com/giant-scam/algorithmic-beat-mapping-in-unity-preprocessed-audio-analysis-d41c339c135a
        Debug.Log(string.Format("Processing {0} time domain samples for FFT", totalSampleCount));
        sampleChunk = new double[spectrumSize];
        for (int i = 0; i < totalSampleCount; ++i)
        {
            // 3.
            // Grab the current 1024 chunk of audio sample data
            Array.Copy(audioSamples, i * spectrumSize, sampleChunk, 0, spectrumSize);

            // 4.
            // Apply our chosen FFT Window
            double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)spectrumSize);
            double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
            double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

            // Perform the FFT and convert output (complex numbers) to Magnitude
            Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
            double[] scaledFFTSpectrumDoubleType = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
            scaledFFTSpectrumDoubleType = DSP.Math.Multiply(scaledFFTSpectrumDoubleType, scaleFactor);
            //Debug.Log($"scaledFFTSpectrum Size : {scaledFFTSpectrumDoubleType.Length}");

            

            curSpectrumChunk.CopyTo(prevSpectrumChunk, 0);
            Array.ConvertAll(scaledFFTSpectrumDoubleType, x => (float)x).CopyTo(curSpectrumChunk, 0);

            // 스펙트럼 flux에 대한 정보 설정
            SpectralFluxInfo spectralFluxInfo = new SpectralFluxInfo();

            // These 1024 magnitude values correspond (roughly) to a single point in the audio timeline
            float curSongTime = GetTimeFromIndex(i, sampleRate, spectrumSize);
            spectralFluxInfo.time = curSongTime;

            // 이전 스펙트럼과 현재 스펙트럼 간의 각 음역대별 증가분 합 저장
            float spectralFlux = 0.0f;
            for (int hzBin = 0; hzBin < curSpectrumChunk.Length; ++hzBin)
            {
                spectralFlux += Mathf.Max(0.0f, prevSpectrumChunk[hzBin] - curSpectrumChunk[hzBin]);
            }
            spectralFluxInfo.spectralFlux = spectralFlux;
            // 9.
            // Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks

            //preProcessedSpectralFluxAnalyzer.analyzeSpectrum(Array.ConvertAll(scaledFFTSpectrum, x => (float)x), curSongTime);

            totalSpectrumChunk.Add(spectralFluxInfo);
        }

        Debug.Log("Spectrum Analysis done");
        Debug.Log($"Total SpectrumChunk count : {totalSpectrumChunk.Count}");
    }

    void ExtractBeat(int beatCheckWindowSize)
    {
        //Debug.Log($"TotlaSpectrumChunk Size : {totalSpectrumChunk.Count}");
        for (int i = 0; i < totalSpectrumChunk.Count; i++)
        {
            float avrSpectrum = CheckAroundSpectrum(i, beatCheckWindowSize);
            totalSpectrumChunk[i].beatStandard = avrSpectrum * beatCheckRatio;

            totalSpectrumChunk[i].prunedSpectralFlux =
                Mathf.Max(0.0f, totalSpectrumChunk[i].spectralFlux - totalSpectrumChunk[i].beatStandard);

            if(i == 0 || i == totalSpectrumChunk.Count - 1)
            {
                totalSpectrumChunk[i].isPeak = false;
                totalBeat.Add(totalSpectrumChunk[i]);
                continue;
            }

            totalSpectrumChunk[i - 1].isPeak = CheckPeak(i - 1);
            if(totalSpectrumChunk[i - 1].isPeak)
            {
                totalBeat.Add(totalSpectrumChunk[i]);
            }
        }

        Debug.Log("Extract Beat done");
        Debug.Log($"Total Beat count : {totalBeat.Count}");
    }

    float CheckAroundSpectrum(int standardIdx, int beatCheckWindowSize)
    {
        int windowStartIdx = Mathf.Max(0, standardIdx - beatCheckWindowSize / 2);
        int windowEndIdx = Mathf.Min(totalSpectrumChunk.Count - 1, standardIdx + beatCheckWindowSize / 2);

        float sum = 0.0f;
        for (int i = windowStartIdx; i < windowEndIdx; i++)
        {
            sum += totalSpectrumChunk[i].spectralFlux;
        }

        return (sum / (windowEndIdx - windowStartIdx));
    }

    bool CheckPeak(int index)
    {
        bool isPeak = false;

        if(index == 0 || index + 1 == totalSpectrumChunk.Count - 1)
        {
            return isPeak;
        }

        // index 기준 좌우 스펙트럼보다 prunedSpectralFlux가 크다면 peak로 판정
        if(totalSpectrumChunk[index].prunedSpectralFlux > totalSpectrumChunk[index - 1].prunedSpectralFlux
            && totalSpectrumChunk[index].prunedSpectralFlux > totalSpectrumChunk[index + 1].prunedSpectralFlux)
        {
            isPeak = true;
        }

        return isPeak;
    }

    // TODO
    // 이전 노트를 기준으로 standardTime 이후의 비트 A를 추출
    // A비트를 기준으로 좌우 window탐색
    // minNoteSwitchTime 이후면서 && 시간차이가 이전의 노트보다 현재의 비트A와 가깝고 && pruneSpectralFlux가 가장 높은 비트 확인
    // 해당 비트를 노트로 생성 
    void MakeNote()
    {
        //int curBeatIdx = 0;
        int prevBeatIdx = 0;

        float curBeatTime = 0.0f;
        float prevBeatTime = 0.0f;

        int curNoteIdx = 0;
        int curNotePos = notePositionGridNumber / 2;
        int prevNotePos = notePositionGridNumber / 2;

        for (int curBeatIdx = 0; curBeatIdx < totalBeat.Count; ++curBeatIdx)
        //while(curBeatIdx < totalBeat.Count)
        {
            prevBeatIdx = curBeatIdx;
            prevBeatTime = totalBeat[prevBeatIdx].time;
            prevNotePos = curNotePos;

            curBeatIdx = CompareToNoteDecision(curBeatIdx, prevBeatIdx);
            curBeatTime = totalBeat[curBeatIdx].time;

            if (curNoteIdx > 0)
            {
                totalNote[curNoteIdx - 1].endTime = curBeatTime;
            }

            NoteInfo noteInfo = new NoteInfo();
            noteInfo.startTime = curBeatTime;
            curNotePos = DecisionNotePosition(prevNotePos);
            noteInfo.noteGridPosition = curNotePos;

            totalNote.Add(noteInfo);
            ++curNoteIdx;
        }

        totalNote[totalNote.Count - 1].endTime = audioSource.clip.length;
        Debug.Log("Make Note done");
        Debug.Log($"Total Note count : {totalNote.Count}");
        convertFinish = true;
        audioSource.Play();
    }

    // minNoteSwitchTime 이후를 기준으로 좌우 탐색 && 시간차이가 이전의 노트보다 현재의 비트A와 가깝고 && pruneSpectralFlux가 가장 높은 비트 확인
    int CompareToNoteDecision(int standardIdx, int prevNoteIdx)
    {
        while (totalBeat[standardIdx].time - totalBeat[prevNoteIdx].time < minNoteSwitchTime)
        {
            Debug.Assert(standardIdx < totalBeat.Count, $"noteIdx : {standardIdx}");
            
            ++standardIdx;
            if(standardIdx == totalBeat.Count)
            {
                --standardIdx;
                break;
            }
            
        }
        int noteIdx = standardIdx;
        int windowStartIdx = Mathf.Max(0, noteIdx - noteDecisionWindowSize / 2);
        int windowEndIdx = Mathf.Min(totalBeat.Count - 0, noteIdx + noteDecisionWindowSize / 2);
        
        for (int i = windowStartIdx; i < windowEndIdx; ++i)
        {
            float compareBeatTime = totalBeat[i].time;
            float farFromPrevNote = Mathf.Max(0.0f, (compareBeatTime - totalBeat[prevNoteIdx].time));   // prevNodte 이후의 비트 && prevNote와의 시간 차이 확인
            float farFromstandardBeat = Mathf.Max(0.0f, totalBeat[noteIdx].time - compareBeatTime);

            if (farFromPrevNote < farFromstandardBeat)
            {
                continue;
            }

            if (totalBeat[noteIdx].prunedSpectralFlux < totalBeat[i].prunedSpectralFlux)
            {
                noteIdx = i;
            }
        }

        return noteIdx;

    }

    int DecisionNotePosition(int prevNotePos)
    {
        int newNotePos = prevNotePos;

        if(prevNotePos == notePositionGridNumber - 1)
        {
            newNotePos = prevNotePos - 1;
        }
        else if(prevNotePos == 0)
        {
            newNotePos = prevNotePos + 1;
        }
        else
        {
            int posKey = UnityEngine.Random.Range(0, 2);
            if (posKey == 1)
            {
                newNotePos = prevNotePos + 1;
            }
            else
            {
                newNotePos = prevNotePos - 1;
            }
        }

        return newNotePos;
    }

    void MakeNoteObj(float playTime, ref NoteInfo noteInfo)
    {
        UnityEngine.Vector3 prevCamPos = camera.transform.position;
        float curZPos = (noteInfo.startTime + ((noteInfo.endTime - noteInfo.startTime) / 2)) * 5.0f;
        GameObject go = Instantiate(notePrefab,
            new UnityEngine.Vector3(noteInfo.noteGridPosition, 0.0f, curZPos), UnityEngine.Quaternion.identity);
        go.transform.localScale = new UnityEngine.Vector3(1.0f, 1.0f, (noteInfo.endTime - noteInfo.startTime) * 10.0f);
        UnityEngine.Vector3 newCampos = new UnityEngine.Vector3(1.0f, 10.0f, go.transform.position.z - 5.0f);
        camera.transform.position = UnityEngine.Vector3.Lerp(prevCamPos, newCampos, 0.2f);
        camera.transform.LookAt(new UnityEngine.Vector3(1.0f, go.transform.position.y, go.transform.position.z));
        Debug.Log($"Note StartTime : {noteInfo.startTime}");
        Debug.Log($"Note EndTIme : {noteInfo.endTime}");
        Debug.Log($"Note Length : {(noteInfo.endTime - noteInfo.startTime) * 50.0f}");
        Debug.Log($"NoteTime : {playTime} - {noteIdx} / {totalNote.Count}");

    }
}
