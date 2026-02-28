# 진행 상황 (PROGRESS)

## 현재 단계: MVP1 완성 ✅

---

## 완료된 작업

### 프로젝트 기반
- [x] 프로젝트 폴더 구조 생성
- [x] README.md 작성
- [x] PROGRESS.md 작성
- [x] .sln / .csproj / app.manifest (DPI PerMonitorV2)
- [x] App.xaml / App.xaml.cs

### Core 레이어
- [x] ZoneEngine.cs — 점(x,y) → Zone 4개 Rect 계산
- [x] ConfigStore.cs — JSON 저장/로드 (분할 정보 + 라우팅 규칙)
- [x] WindowManager.cs — Win32 API 창 이동/리사이즈/전면화
- [x] Router.cs — processName → zoneId 규칙 적용 + Zone 창 목록 관리
- [x] WindowMonitor.cs — 폴링(400ms) 창 목록 감지 + 자동 라우팅

### UI 레이어
- [x] OverlayWindow.xaml — 투명 오버레이 + 점 드래그 + Zone 시각화
- [x] ZonePickerWindow.xaml — 어느 Zone으로 보낼지 선택 팝업
- [x] ZoneBarControl.xaml — Zone별 창 제목 리스트 (전면화 버튼)

---

## MVP2 예정

- [ ] 새 창 감지 시 자동 ZonePicker 팝업
- [ ] WinEventHook으로 폴링 대체 (성능 개선)
- [ ] 시스템 트레이 아이콘

---

## 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-02-28 | 프로젝트 초기 구조 생성, README/PROGRESS 작성 |
| 2026-02-28 | MVP1 전체 구현 완료 (Core + UI), 빌드 성공 (오류 0, 경고 0) |
