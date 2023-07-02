# BeatRoads

---

## Introduce

- 아이디어 : SkyRoads + Crypt of the NecroDancer

<img src="https://github.com/YosephKim0207/BeatRoads/assets/46564046/27f6bddb-a156-48ba-8721-908eeb488191" width="480" height="320"/>

<img src="https://github.com/YosephKim0207/BeatRoads/assets/46564046/34b7a469-9353-46d8-8753-62c6eac20858" width="480" height="320"/>

<img src="https://github.com/YosephKim0207/BeatRoads/assets/46564046/63129347-4ad6-4283-8381-0c17c37c76ed" width="480" height="320"/>

<img src="https://github.com/YosephKim0207/BeatRoads/assets/46564046/ade3e0e0-643a-4996-9faf-74c9b5028bd7" width="480" height="320"/>


- 특징 :
  1. 플레이어는 3분할 / 5분할 그리드 방식 영역 이동
  2. 한 곡에 한 스테이지
  3. 유니티 엔진에서 전처리된 노트(플레이어 이동 가능 영역)를 이용한 스테이지
  4. Crypt of th NecroDancer처럼 박자에 맞추어 노트를 이동하도록 유도한다
  5. 박자가 정확도에 따라 밟은 노트의 색이 달라진다
  6. 적절한 타이밍에 노트를 이동하지 못하는 경우 에너지를 소비한다

- 구현해야하는 요소 :
  1. Audio Source로부터 노트를 생성하는 유니티 Tool 제작
  2. 특정 주파수 - 킥 or 스네어 - 에 노트가 생성되도록 FFT를 이용해 특정 주파수 시간에 switch할 노트가 생성되도록 한다
  3. Tool에는 플레이어의 이동속도에 따라 노트의 길이, switch시 노트에서 떨어지지 않을만큼의 겹쳐지는 구간의 폭이 설정되도록 한다
 
## Work In Progress

- 구현 중
  1. 오디오 스펙트럼 분석해서 비트 만들기
     a. 적절한 비트 만들기 문제
      i. window를 통해 앞뒤 50개의 spectrum의 평균을 구하고 적절한 beat 판별 ratio를 곱함
      >> i.1 현재 스펙트럼의 값이 i의 결과보다 크다면 beat로 간주
      >> 
      >> i.2 단 이전 비트 생성 시간보다 일정 시간 이후부터 생성 가능
      >> 
      >> i.3 추가할 사항으로 노래의 bpm에 따른 판별식도 고려

      ii. [기존의 코드](https://medium.com/giant-scam/algorithmic-beat-mapping-in-unity-real-time-audio-analysis-using-the-unity-api-6e9595823ce4) 활용시 해당 샘플의 비트는 잘 뽑아내지만 비트가 너무 많이 생성된다
      - 기존의 방식 :
      >> ii.1 직전 프레임과 현재 프레임의 스펙트럼 음역대별 비교로 스펙트럼 증가분(flux) 합 계산
      >> 
      >> ii.2 현재 스펙트럼 기준 앞뒤로 window/2개 만큼에 대해
      >> 
      >> ii.3 window 전체의 flux 평균에 비트 계산 상수를 곱하여 현 스펙트럼의 비트 기준값 결정
      >> 
      >> ii.4 해당 비트 기준값을 기준으로 현 프레임의 스펙트럼이 얼마나 큰지 계산 - prune
      >> 
      >> ii.5 이때 크지 않다면 0으로 저장
      >> 
      >> ii.6 이후 index - 1의 prune값을 기준으로 Index -2, Index를 비교
      >> 
      >> ii.7 index -1의 prune 값이 가장 크다면 피크값으로 판정 

      - 내 보정
      >> 곡의 실제 bpm과 비교해보면 거의 10배 가까이 더 생긴다(10배 이상)
      >> 
      >> sol1. 비트 상수의 크기를 기운다
      >> 
      >> 테스트 결과 비트 상수의 크기가 2일 때 실제 비트와 유사한 결과를 보여준다
      >> 
      >> sol2. 비트가 과추출되는 것을 방지하기 위해 && 현실적인 플레이를 위해 비트 갱신 최소시간을 둔다
      >> 
      >> sol3.  비트 갱신 최소 시간을 넘기는 비트를 기준으로 주변의 비트들 중 일정 시간 내의 비트들 중 가장 스펙트럼의 값이 큰 것을 비트로 결정한다

  2. 비트의 시작시간, 노트 만들 위치를 직렬화-역직렬화
  
