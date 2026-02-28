# ZoneRouter

Windows 화면 분할 + Zone(페이지) 기반 창 라우팅 유틸리티

## 개요

보조 모니터 없이도 작업 효율을 올리기 위해,
화면을 점/선으로 분할해 Zone(칸)을 만들고,
각 Zone에 특정 앱/창을 자동으로 배치·관리·전환할 수 있는 개인용 유틸리티.

## 기술 스택

- **언어**: C# (.NET 8)
- **UI 프레임워크**: WPF
- **Win32 API**: P/Invoke (창 이동/리사이즈/전면화)
- **설정 저장**: JSON

## 프로젝트 구조

```
ZoneRouter/
├── README.md
├── PROGRESS.md
├── ZoneRouter.sln
└── ZoneRouter/
    ├── App.xaml / App.xaml.cs
    ├── Core/
    │   ├── ZoneEngine.cs       ← 점 → 4개 Rect(Zone) 계산
    │   ├── WindowManager.cs    ← Win32 창 이동/리사이즈/전면화
    │   ├── WindowMonitor.cs    ← 폴링으로 새 창 감지
    │   ├── Router.cs           ← 규칙 기반 Zone 자동 배치
    │   └── ConfigStore.cs      ← JSON 설정 저장/로드
    └── UI/
        ├── OverlayWindow.xaml       ← 투명 오버레이 + 점 드래그
        ├── ZonePickerWindow.xaml    ← Zone 선택 팝업
        └── ZoneBarControl.xaml     ← Zone 상단 창 제목 리스트
```

## MVP 단계

### MVP1 (현재)
- [x] 점 1개 기반 4분할 + 드래그로 점 이동
- [x] 현재 활성 창을 선택한 Zone으로 전송
- [x] processName → zoneId 규칙 저장/로드
- [x] 프로그램 실행 시 규칙 있으면 자동 배치
- [x] Zone 상단 창 제목 리스트 → 클릭 시 전면화

### MVP2 (예정)
- [ ] 새 창 감지 시 자동 ZonePicker 팝업
- [ ] WinEventHook으로 폴링 대체 (성능 개선)

### MVP3 (선택/확장)
- [ ] DWM Thumbnail 썸네일 스위처
- [ ] 다중 선/점 분할 (그리드)
- [ ] 멀티 모니터 지원

## 사용 방법

1. 앱 실행 → 투명 오버레이 표시
2. 중앙 점을 드래그해서 원하는 분할 위치 설정
3. "분할 확정" 클릭 → 오버레이 숨김
4. 앱 실행 후 창이 뜨면 → Zone 선택 → 자동 배치 + 규칙 저장
5. 이후 동일 앱은 자동으로 해당 Zone으로 이동

## 주의사항

- UWP 앱은 이동/리사이즈 제한 있을 수 있음 (실패 시 무시 처리)
- SetForegroundWindow는 사용자 입력(클릭/핫키) 시에만 안정적으로 동작
- DPI 배율 환경에서는 좌표 변환 로직 적용됨
