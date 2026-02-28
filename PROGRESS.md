# 진행 상황 (PROGRESS)

## 현재 단계: MVP1 구현 중

---

## 완료된 작업

- [x] 프로젝트 폴더 구조 생성
- [x] README.md 작성
- [x] PROGRESS.md 작성

---

## 진행 중

- [ ] 프로젝트 파일 생성 (.sln, .csproj)
- [ ] Core 레이어 구현
- [ ] UI 레이어 구현

---

## 구현 예정 (MVP1)

### Core
- [ ] ZoneEngine.cs — 점(x,y) → Zone 4개 Rect 계산
- [ ] ConfigStore.cs — JSON 저장/로드 (분할 정보 + 라우팅 규칙)
- [ ] WindowManager.cs — Win32 API 창 이동/리사이즈/전면화
- [ ] Router.cs — processName → zoneId 규칙 적용
- [ ] WindowMonitor.cs — 폴링(300ms) 창 목록 감지

### UI
- [ ] OverlayWindow.xaml — 투명 오버레이 + 점 드래그 + Zone 시각화
- [ ] ZonePickerWindow.xaml — 어느 Zone으로 보낼지 선택 팝업
- [ ] ZoneBarControl.xaml — Zone별 창 제목 리스트 (전면화 버튼)

---

## 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-02-28 | 프로젝트 초기 구조 생성, README/PROGRESS 작성 |
