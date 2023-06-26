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
