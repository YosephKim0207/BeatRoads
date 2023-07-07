using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;
using System.Threading;
using DSPLib;
using System.IO;

// 230701 TODO
// 일정 시간 혹은 프레임 동안의 각 음역대별 평균 값을 구해 저장한다
// 갑자기 튀는 음역대가 있다면 해당 분을 비트로 간주한다
// 일단 비트에 대한 사전적 정의를 보고 audiacity로 비트 구분을 전체 볼륨으로 하는, 특정 음역대가 튀는걸로 하는지 확인해보기
// 각 음역대별 평균이랑 스펙트럼당 평균 다 구해보고 비교 / 연구해보기

#region Test
[System.Serializable]
public class ToJson
{
    public List<SpectralFluxInfo> noteInfos;
}
#endregion

[System.Serializable]
public class NoteToJson
{
    public List<NoteInfo> noteInfos;
}

[System.Serializable]
public class NoteInfo
{
    public float startTime;
    public float endTime;
    public int noteGridPosition;
}

public class PreNoteMaker
{
    #region Test
    SpectralFluxAnalyzer preProcessedSpectralFluxAnalyzer = new SpectralFluxAnalyzer();
    #endregion

    // 비트 판단을 위한 변수들
    float beatCheckRatio = 2.0f;
    public float SetBeatCheckRatio { set { beatCheckRatio = value; } }
    float minNoteSwitchTime = 0.3f;  // 노트가 변경되는 최소 시간. 이것보다는 노트 변경 주기가 커야 한다
    public float SetMinNoteSwitchTime { set { minNoteSwitchTime = value; } }
    int noteDecisionWindowSize = 3;
    int notePositionGridNumber = 5;
    public int SetNotePositionGridNumber { set { notePositionGridNumber = value; } }

    AudioClip audioClip;
    public AudioClip SetAudioSourceClip { set { audioClip = value; } }

    
    List<SpectralFluxInfo> totalSpectrumChunk;

    [SerializeField]
    List<SpectralFluxInfo> totalBeat;

    [SerializeField]
    List<NoteInfo> totalNote;

    public class SpectralFluxInfo
    {
        public float time;
        public float spectralFlux;
        public float beatStandard;
        public float prunedSpectralFlux;
        public bool isPeak;
    }

    public bool RunPreNoteMaker(ref string savePath)
    {
        if(audioClip)
        {   
            int clipChannels = audioClip.channels;
            int sampleCount = audioClip.samples;
            int sampleRate = audioClip.frequency;

            float[] audioSamplesByChannel = new float[sampleCount * clipChannels];            
            audioClip.GetData(audioSamplesByChannel, 0);

            // audioSamplesByChannel에는 각 채널에 대한 샘플이 모두 저장 
            // 따라서 스테레오 채널인 경우 샘플을 하나로 합치는 작업이 필요하다
            float[] audioSamples = new float[sampleCount];
            ConvertAudioSampesByChannel(ref audioSamples, ref audioSamplesByChannel, clipChannels);

            // 실시간 처리에는 AudioSource.GetSpectrumData를 통해 퓨리에 변환을 진행, 음역대를 추출할 수 있다
            // 하지만 유니티에는 전체 샘플에 대해 퓨리에 변환을 수행할 수 있는 함수가 없다
            // 따라서 외부 함수 - DSPLib - 를 사용한다
            //Thread thread = new Thread(() => DoFFT(ref audioSamples, ref sampleChunk, sampleRate, spectrumSize));
            //thread.Start();
            DoFFT(ref audioSamples, sampleRate);
            int beatCheckWindowSize = 50;   // 해당 스펙트럼 기준 앞뒤로 비교할 스펙트럼의 개수
            ExtractBeat(beatCheckWindowSize);
            MakeNote();
            bool saveSuccess = SaveNote(ref savePath);

            return saveSuccess;
        }
        else
        {
            Debug.Log("RunPreNoteMaker False : audioClip is null");

            return false;
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

    void DoFFT(ref float[] audioSamples, int sampleRate)
    {
        totalSpectrumChunk = new List<SpectralFluxInfo>();
        int spectrumSize = 1024;
        double[] sampleChunk = new double[spectrumSize];
        float[] curSpectrumChunk = new float[spectrumSize];
        float[] prevSpectrumChunk = new float[spectrumSize];
        int totalSampleCount = audioSamples.Length / spectrumSize;
        FFT fft = new FFT();
        fft.Initialize((uint)spectrumSize);

        // 아래의 출처로부터 참고
        // https://medium.com/giant-scam/algorithmic-beat-mapping-in-unity-preprocessed-audio-analysis-d41c339c135a
        Debug.Log(string.Format("Processing {0} time domain samples for FFT", totalSampleCount));
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

            totalSpectrumChunk.Add(spectralFluxInfo);
        }

        Debug.Log("Spectrum Analysis done");
        Debug.Log($"Total SpectrumChunk count : {totalSpectrumChunk.Count}");
    }

    void ExtractBeat(int beatCheckWindowSize)
    {
        totalBeat = new List<SpectralFluxInfo>();
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

    void MakeNote()
    {
        int prevBeatIdx = 0;

        float curBeatTime = 0.0f;
        float prevBeatTime = 0.0f;

        int curNoteIdx = 0;
        Debug.Log($"Make Note Position Grid : {notePositionGridNumber}");
        int curNotePos = notePositionGridNumber / 2;
        int prevNotePos = notePositionGridNumber / 2;

        Debug.Log($"Cur note pos : {curNotePos}");

        totalNote = new List<NoteInfo>();
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

        totalNote[totalNote.Count - 1].endTime = audioClip.length;
        Debug.Log("Make Note done");
        Debug.Log($"Total Note count : {totalNote.Count}");
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
        int windowEndIdx = Mathf.Min(totalBeat.Count - 1, noteIdx + noteDecisionWindowSize / 2);
        
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


    bool SaveNote(ref string savePath)
    {
        bool saveSuccess = false;

        NoteToJson noteToJson = new NoteToJson();
        noteToJson.noteInfos = new List<NoteInfo>(totalNote);
        string json = JsonUtility.ToJson(noteToJson);
        Debug.Assert(json != null, "Make json fail");
        if(json == null)
        {
            return saveSuccess;
        }

        File.WriteAllText(savePath, json);
        FileInfo fileInfo = new FileInfo(savePath);
        if (fileInfo.Exists)
        {
            saveSuccess = true;
            Debug.Log($"Save to {savePath + audioClip.name}");
        }

        Debug.Assert(saveSuccess, "Make note file fail");

        return saveSuccess;
    }


    #region 비교용 코드
    //public void getFullSpectrumThreaded()
    //{
    //    int numTotalSamples = audioClip.samples;
    //    float[] multiChannelSamples = new float[audioClip.samples * audioClip.channels];
    //    int numChannels = audioClip.channels;
    //    float clipLength = audioClip.length;
    //    try
    //    {
    //        // 1.
    //        // We only need to retain the samples for combined channels over the time domain
    //        float[] preProcessedSamples = new float[numTotalSamples];

    //        int numProcessed = 0;
    //        float combinedChannelAverage = 0f;
    //        for (int i = 0; i < multiChannelSamples.Length; i++)
    //        {
    //            combinedChannelAverage += multiChannelSamples[i];

    //            // Each time we have processed all channels samples for a point in time, we will store the average of the channels combined
    //            if ((i + 1) % numChannels == 0)
    //            {
    //                preProcessedSamples[numProcessed] = combinedChannelAverage / numChannels;
    //                numProcessed++;
    //                combinedChannelAverage = 0f;
    //            }
    //        }

    //        Debug.Log("Combine Channels done");
    //        Debug.Log(preProcessedSamples.Length);

    //        // 2.
    //        // Once we have our audio sample data prepared, we can execute an FFT to return the spectrum data over the time domain
    //        int spectrumSampleSize = 1024;
    //        int iterations = preProcessedSamples.Length / spectrumSampleSize;

    //        FFT fft = new FFT();
    //        fft.Initialize((UInt32)spectrumSampleSize);

    //        Debug.Log(string.Format("Processing {0} time domain samples for FFT", iterations));
    //        double[] sampleChunk = new double[spectrumSampleSize];
    //        for (int i = 0; i < iterations; i++)
    //        {
    //            // 3.
    //            // Grab the current 1024 chunk of audio sample data
    //            Array.Copy(preProcessedSamples, i * spectrumSampleSize, sampleChunk, 0, spectrumSampleSize);

    //            // 4.
    //            // Apply our chosen FFT Window
    //            double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)spectrumSampleSize);
    //            double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
    //            double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

    //            // Perform the FFT and convert output (complex numbers) to Magnitude
    //            Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
    //            double[] scaledFFTSpectrum = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
    //            scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

    //            // These 1024 magnitude values correspond (roughly) to a single point in the audio timeline
    //            float curSongTime = getTimeFromIndex(i) * spectrumSampleSize;

    //            // 9.
    //            // Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks

    //            preProcessedSpectralFluxAnalyzer.analyzeSpectrum(Array.ConvertAll(scaledFFTSpectrum, x => (float)x), curSongTime);
    //        }


    //        Debug.Log("Spectrum Analysis done");
    //        Debug.Log("Background Thread Completed");

    //        for (int i = 1; i < preProcessedSpectralFluxAnalyzer.peakList.Count; ++i)
    //        {
    //            preProcessedSpectralFluxAnalyzer.peakList[i - 1].endTime = preProcessedSpectralFluxAnalyzer.peakList[i].startTime;
    //        }

    //        preProcessedSpectralFluxAnalyzer.peakList[preProcessedSpectralFluxAnalyzer.peakList.Count - 1].endTime = audioClip.length;

    //        Debug.Log("Make Note using onlineCode done");
    //        Debug.Log($"Total Note count : {preProcessedSpectralFluxAnalyzer.peakList.Count}");
    //    }
    //    catch (Exception e)
    //    {
    //        // Catch exceptions here since the background thread won't always surface the exception to the main thread
    //        Debug.Log(e.ToString());
    //    }
    //}

    //bool SaveNote(ref string savePath)
    //{
    //    bool saveSuccess = false;

    //    ToJson noteToJson = new ToJson();
    //    noteToJson.noteInfos = new List<SpectralFluxInfo>(preProcessedSpectralFluxAnalyzer.peakList);
    //    string json = JsonUtility.ToJson(noteToJson);
    //    Debug.Assert(json != null, "Make json fail");
    //    if (json == null)
    //    {
    //        return saveSuccess;
    //    }

    //    File.WriteAllText(savePath, json);
    //    FileInfo fileInfo = new FileInfo(savePath);
    //    if (fileInfo.Exists)
    //    {
    //        saveSuccess = true;
    //        Debug.Log($"Save to {savePath + audioClip.name}");
    //    }

    //    Debug.Assert(saveSuccess, "Make note file fail");

    //    return saveSuccess;
    //}

    //public int getIndexFromTime(float curTime)
    //{
    //    float lengthPerSample = audioClip.length / (float)audioClip.samples;

    //    return Mathf.FloorToInt(curTime / lengthPerSample);
    //}

    //public float getTimeFromIndex(int index)
    //{
    //    return ((1f / (float)audioClip.frequency) * index);
    //}

    #endregion
}
