# 다나와 자동차 v2 — 구현 진행 상황 (인수인계)

작성 시점: 2026-09-23. 7단계(관리자)까지 다른 PC에서 완료 → 같은 날 이 PC(Windows)에서 환경을 맞추고 **8단계(UI 개선) 완료**.
다른 PC의 Claude Code에서 이어가려면 이 문서를 먼저 읽는다.

---

## 0. 이 문서만으로는 부족하다 — 반드시 같이 가져갈 것

`dnwa_v2/`는 프로젝트 폴더의 일부일 뿐이다. 아래는 **`dnwa_v2`의 상위 폴더(현재는 `Downloads\danawa_v2`)에 형제 폴더/파일로** 존재하고, 전부 있어야 이어갈 수 있다.

```
danawa_v2/                       ← 이 폴더 전체를 옮기는 게 가장 안전하다
├── CLAUDE.md                    ← 프로젝트 규칙서. 최우선. 없으면 아무것도 하지 마라
├── docs/
│   ├── 01_요구사항명세.md        (57건)
│   ├── 02_테이블명세.md          (DDL 전문 포함)
│   ├── 03_API명세.md            (29개 인터페이스 상세정의. IF-CAR-002는 5단계에서 brand_id를 선택으로 바꾸고 origin_type을 추가함)
│   ├── 04_화면명세.md            (12화면, MVVM 구현 규칙)
│   ├── 05_구현지시서.md          (단계별 지시·검증기준, 8단계 체크리스트 완료 표시)
│   └── 06_요구사항대조표.md      (8단계 검증: 57건 판정·근거. 9단계 V9-5가 이어서 쓴다)
├── sql/
│   ├── ddl_v2.sql
│   ├── seed.sql
│   ├── queries.sql
│   └── test_constraints.sh       (mysql 바이너리를 직접 부른다 — mariadb 아님)
└── dnwa_v2/                     ← 실제 코드 (이 문서가 있는 곳)
```

이 저장소는 **git이 아니다**(폴더 복사만 가능). 다른 PC로 옮길 때는 위 폴더 통째로 옮기거나, 최소한 `CLAUDE.md`, `docs/`, `sql/`, `dnwa_v2/`를 같은 상대 구조로 옮긴다.

---

## 1. 진행 상태 요약

| 단계 | 상태 |
|---|---|
| 0~2 (결정·문서·DB 설계) | 완료 (이전 세션) |
| 3 (솔루션 구성 + 통신 계층) | 완료. V3-1~V3-7 통과 |
| 4 (인증) | 완료. V4-1~V4-9 + admin 로그인 전부 통과 |
| 5 (조회) | 완료. 01/04/05 화면 |
| 6 (관심 / 견적) | 완료. 06/07 화면, 트랜잭션·금액 재계산 |
| 7 (관리자) | 완료. IF-ADM-001~012 + 08~12 화면(대시보드/차량·브랜드/옵션/통계/로그) 전부 구현·검증 |
| **8 (UI 개선)** | **완료.** 12화면을 와이어프레임 PDF에 맞춤 + 공통 스타일 + 로딩/중복클릭/빈 상태. 요구사항 57건 대조 → 4-2절·5-2절 |
| DB 실물 구성 (MySQL) | 완료. MySQL 8.0.45 — 2절 필독 |
| **9 (통합 테스트 + 완료보고서)** | **완료.** V9-1~V9-6 전부 PASS, 요구사항 57/57 ✅, 완료보고서(md+docx). 서버 DB 커넥션 풀 상한 5 → 5-3절 |

**TestClient 시나리오 누적 56개(V3~V9), 전부 PASS.** + `--db-down` 모드 1개(V9-4, DB를 멈춘 상태에서만 실행).

---

## 2. 로컬 환경 상태

- **.NET 8 SDK**: `winget install Microsoft.DotNet.SDK.8`로 8.0.425 설치됨(`global.json`이 고정). `dotnet --list-sdks`로 확인할 것.
- **MySQL**: `MySQL Server 8.0.45`, Windows 서비스 `MySQL80`으로 떠 있다(재부팅해도 자동 기동). root / `1234`. (이 PC에 있던 MySQL 8.4는 2026-09-23 삭제하고 8.0.45로 통일 — 설치는 사용자가 직접 함.) `danawa_car_v2` 스키마는 `sql/ddl_v2.sql` + `sql/seed.sql`로 만들었다. 반드시 `--default-character-set=utf8mb4` 옵션으로 실행할 것.
- **admin 계정**: **`login_id=admin`, 비밀번호 `Admin1234!`** (seed 더미 해시를 PBKDF2 실 해시로 갱신함, root로만 가능).
- **`sql/test_constraints.sh`**: `mysql` 바이너리를 직접 부른다. `~/bin/mysql` 셔임 필요(이 PC 전용):
  ```bash
  #!/bin/bash
  exec "/c/Program Files/MySQL/MySQL Server 8.0/bin/mysql.exe" "$@"
  ```
  root 비밀번호는 실행 전 `export MYSQL_PWD=1234`로 넘길 것. **주의**: 이 스크립트는 검증 중 실제 UPDATE/DELETE를 남긴다.
- **TestClient의 검증 원칙 (5·6단계에서 두 번 겪고 확정)**: seed 데이터의 절대 건수를 **하드코딩하지 않는다.** `test_constraints.sh`와 TestClient 자신(quote_add/favorite_set/admin_car_save 등을 실제로 여러 번 실행함)이 매 실행마다 데이터를 누적시킨다. DB를 직접 재조회해서 기대값을 계산하는 패턴(`V5daily_MainDailyTop`, `V5summary_MainSummary`, `V79_WeeklyTop5MatchesQuoteBasedDbQuery` 참고)을 8~9단계에서도 유지할 것.
- **Windows Smart App Control**: `dotnet build` 직후 바로 실행하면 `FileLoadException`으로 막힌다. **빌드 후 20~30초 대기** 후 실행.
- **서버 프로세스가 DLL을 잠그는 문제**: `netstat -ano | grep :9000`으로 PID를 찾아 `Stop-Process -Id <PID> -Force`로 끄고 다시 빌드할 것. 클라이언트가 떠 있으면 `Danawa.Client.dll`도 잠긴다(창 제목이 "다나와 자동차"로 시작하는 dotnet 프로세스).

---

## 3. 솔루션 구조 (실제 파일, 7단계까지 반영)

```
dnwa_v2/
├── DanawaV2.sln
├── global.json
├── src/
│   ├── Danawa.Shared/Contracts/
│   │   ├── (3단계) RequestBase/ResponseBase/RequestType/ResultCode.cs
│   │   ├── (4단계) CheckId*/Signup*/Login*.cs
│   │   ├── (5단계) BrandDto/CarSummaryDto/CarOptionDto/BrandListResponse/CarListRequest·Response/
│   │   │            CarSearchRequest/CarDetailRequest·Response/DailyTopCarDto/MainDailyTopResponse/
│   │   │            FavoriteTopCarDto/MainFavoriteTop5Response/ImageGetRequest·Response/MainSummaryResponse.cs
│   │   ├── (6단계) FavoriteSetRequest·Response/FavoriteDto/FavoriteListResponse/
│   │   │            QuoteAddRequest·Response/QuoteOptionDto/QuoteDto/QuoteListResponse/QuoteDeleteRequest.cs
│   │   └── (7단계) AdminBrandDto/AdminBrandListResponse/AdminBrandSaveRequest·Response/
│   │                AdminCarDto/AdminCarListRequest·Response/AdminCarSaveRequest·Response/AdminCarDeleteRequest/
│   │                AdminOptionDto/AdminOptionListRequest·Response/AdminOptionSaveRequest·Response/AdminOptionDeleteRequest/
│   │                DailyTopSummaryDto/AdminStatsSummaryResponse/WeeklyTopCarDto/AdminStatsWeeklyTop5Response/
│   │                AdminStatsDailyRequest·Response/DailyCountDto/AdminLogListRequest·Response/AdminLogDto.cs
│   ├── Danawa.Server/
│   │   ├── Net/ClientSession.cs        (admin_* 요청은 여기 한 곳에서만 권한을 검사한다 — ADM-01)
│   │   ├── Data/Db.cs · Security/PasswordHasher.cs
│   │   ├── Handlers/
│   │   │   ├── AuthHandler.cs (4단계) · CarHandler.cs (5단계)
│   │   │   ├── FavoriteHandler.cs · QuoteHandler.cs (6단계)
│   │   │   └── AdminHandler.cs (7단계 — 브랜드/차량/옵션 CRUD, 통계 3종, 로그 조회. 자체 권한 검사 없음 — ClientSession이 이미 보장)
│   │   └── images/img/{brand,car}/*.png (5단계 플레이스홀더)
│   ├── Danawa.Client/
│   │   ├── Services/ServerConnection.cs · ImageLoader.cs
│   │   ├── ViewModels/
│   │   │   ├── ViewModelBase/RelayCommand.cs
│   │   │   ├── ShellViewModel.cs (일반회원 셸) · AdminShellViewModel.cs (관리자 셸, 7단계에서 화면 전환 추가)
│   │   │   ├── LoginViewModel/SignupViewModel.cs
│   │   │   ├── CarCardViewModel/BrandCardViewModel.cs · MainViewModel(01)/CarListViewModel(04)/CarDetailViewModel(05)
│   │   │   ├── FavoriteCardViewModel/FavoriteListViewModel(06) · QuoteCardViewModel/QuoteListViewModel(07)
│   │   │   └── (7단계) AdminDashboardViewModel(08) · AdminCarBrandViewModel(09, 탭 2개+폼 2종) ·
│   │   │                AdminOptionViewModel(10) · AdminStatsViewModel(11, 막대차트+TOP5) · AdminLogViewModel(12) ·
│   │   │                DailyStatPointViewModel(11의 막대 1개)
│   │   ├── Views/ (11단계 화면마다 xaml+xaml.cs 1쌍, 7단계분: AdminDashboardView/AdminCarBrandView/AdminOptionView/AdminStatsView/AdminLogView)
│   │   └── App.xaml(.cs)
│   └── Danawa.TestClient/Program.cs      (V3~V8 시나리오 52개)
│
│   (8단계 추가) Client/App.xaml — 공통 스타일(색·버튼·카드·입력·표·탭·사이드바)
│                Client/Converters/WonToManwonConverter.cs, Client/ViewModels/WeeklyRankRow.cs (08/11 TOP5 한 줄)
```

---

## 4. 7단계에서 확정한 설계 — 다음 단계가 참고할 것

- **관리자 권한 검사는 분기 지점 한 곳**: `ClientSession.HandleRequestAsync`에서 `type`이 `"admin_"`으로 시작하면 라우팅(스위치문) 전에 세션을 확인한다(비로그인→UNAUTHORIZED, USER→FORBIDDEN). `AdminHandler`의 12개 메서드는 전부 이미 ADMIN인 걸 전제하고 권한 검사 코드가 없다 — `FavoriteHandler`/`QuoteHandler`(6단계, 핸들러마다 `CheckUserAuth` 호출)와 의도적으로 다른 패턴이다. type 문자열 접두사로 분기하는 방식이라 **7번째 admin_* interface가 생겨도 라우팅 스위치에 한 줄만 추가하면 자동으로 보호된다.**
- **admin_stats_summary가 "오늘 전체 견적 수"를 안 준다**: IF-ADM-009 응답은 `total_quotes`와 `daily_top`(오늘 1위 차량의 건수)뿐이라, 화면 08 명세가 요구하는 "오늘 견적 수" 카드를 채울 필드가 없다. 명세를 고치는 대신 **이미 공개된 `main_summary`(IF-CAR-008)를 관리자 세션에서도 그냥 같이 호출**해서 `today_quotes`를 가져왔다(`AdminDashboardViewModel`). `main_summary`는 admin_* 접두사가 아니라서 권한 게이트에 안 걸리고, ADMIN 세션이 호출해도 my_quotes/my_favorites가 0으로 나올 뿐 문제없이 동작한다.
- **통계 화면의 "0건 날 채우기"·"재조회 시 이전 결과 비우기"는 클라이언트 책임**: 서버(`AdminHandler.StatsDailyAsync`)는 0건인 날을 응답에서 아예 뺀다. `AdminStatsViewModel.SearchDailyAsync`가 `date_from~date_to`를 순회하며 없는 날짜를 0으로 채우고(`DailyStatPointViewModel`), 조회 시작 시 `DailyPoints.Clear()`를 먼저 호출한다(ADM-09).
- **09/10 화면의 목록 선택은 코드비하인드 이벤트가 아니라 `SelectedItem` 바인딩**으로 처리했다(`SelectedCarForList`/`SelectedBrandForList`/`SelectedOptionForList` 프로퍼티의 setter가 폼을 채운다). 코드비하인드에 로직을 두지 않는다는 화면명세 3절 규칙을 지키기 위함.

---

## 4-2. 8단계에서 확정한 설계 — 9단계가 참고할 것

- **화면 기준은 와이어프레임 PDF** (`Downloads\0922_new다나와_와이어프레임_12화면.pdf`, 12장, 텍스트 없는 이미지 PDF). 창 1280×800, 헤더/GNB 고정.
- **공통 스타일은 `App.xaml` 한 곳**: `PrimaryButton`/`SecondaryButton`/`PlainButton`/`HeartButton`/`TabButton`/`SideMenuButton`, `Card`, `EmptyStateBox`(Content=제목, Tag=설명), `LoadingText`, `RankTable`, DataGrid 기본 스타일. 선택 상태는 `Tag`에 bool을 바인딩한다 — 트리거 값은 반드시 `<sys:Boolean>True</sys:Boolean>` (문자열 "True"로 쓰면 bool과 매칭되지 않아 밑줄이 안 나왔다).
- **`IsBusy`는 `ViewModelBase`로 올렸다.** 5개 VM에 똑같이 복붙돼 있던 것을 합쳤다. 로딩 표시와 명령의 `canExecute: !IsBusy`(중복 클릭 방지)가 같은 값을 쓴다.
- **로그인 후 복귀 시 재조회**: `ViewModelBase.OnReturnedAfterLogin()`(가상 메서드). `ShellViewModel.OnLoginSucceeded`가 복귀 화면에 호출한다. 메인=전체 재조회, 목록=하트 재대조, 상세=is_favorite만 재조회(옵션 체크 유지).
- **목록/TOP5 카드 하트**: 목록 API에 필드를 늘리지 않고, 로그인 상태면 `favorite_list`를 한 번 더 불러 `car_id`로 대조한다(`CarCardViewModel.MarkFavoritesAsync`). 사용자 결정.
- **정렬은 서버가 한다**: `sort` = `POPULAR`(최근 7일 관심 수, 메인 TOP5와 같은 지표) / `PRICE_ASC` / 생략(car_id). 화이트리스트로 ORDER BY를 고르고 그 밖의 값은 VALIDATION. 목록 기본값은 인기순. 사용자 결정.
- **API에 필드를 더한 곳 (명세 먼저 수정)**: IF-FAV-002 `option_total`, IF-QUO-002 `brand_name`·`image_url`(**현재 값, 스냅샷 아님**), IF-ADM-009/010 `brand_name`.
- **관리자 09/10 화면의 삭제 버튼은 목록 행이 아니라 폼에** 있다(와이어프레임). 폼이 비어 있으면(신규) 삭제 버튼 비활성.
- 체크박스 Content의 `_`는 WPF 액세스 키로 처리된다 → `is__active`처럼 두 번 쓴다.
- **서버 점검 화면 (COM-01·COM-08, 사용자 요청)**: 실행 시 접속 실패 또는 사용 중 연결 끊김 → 콘텐츠 영역이 `ServerDownView`("서버 점검 중입니다" + [재접속])로 바뀐다.
  - `ServerConnection`이 접속 주소를 기억하고 `ReconnectAsync()`로 같은 주소에 새 TCP 연결을 연다. 끊김을 감지하면 죽은 스트림을 버리고 대기 중 요청을 전부 실패시킨다. 수신 루프는 자기 스트림만 읽는다(재접속 뒤 옛 루프가 새 연결 상태를 건드리지 않게).
  - 새 연결 = 새 세션 → 끊기는 순간 `LoginId=null`, 재접속 성공 시 비로그인 메인. 관리자 셸에서 끊기면 `App`이 일반 셸로 바꾼다(관리자 창 닫힘).
  - 끊긴 동안에는 `ShellViewModel.CurrentViewModel` setter가 점검 화면 외 이동을 무시하고, GNB 탭·로고·검색도 탭 상태를 바꾸지 않는다.
  - 재접속 실패 시 문구는 **"점검이 끝난 후 다시 시도해주세요"** 고정 + 마지막 시도 시각. 예외 원문("대상 컴퓨터에서 연결을 거부했으므로…")은 보여주지 않는다(사용자 요청).
- **메인 화면 하트 연동 (사용자 발견 버그 수정)**: 오늘의 인기 차량과 국산/수입 TOP5에 같은 차가 **서로 다른 `CarCardViewModel`**로 나온다. 한쪽 하트를 누르면 `MainViewModel.ToggleFavoriteAsync`가 같은 `car_id` 카드 전부에 `SyncFavoriteFrom`으로 상태를 맞춘다(양방향). TOP5의 "관심 N" 숫자는 즉시 갱신하지 않는다(재조회 시 반영).

## 5. 7단계 검증 — 전부 실제로 실행해서 확인함

**서버 (`Danawa.TestClient`)**: 46개 시나리오 전부 PASS.
V7-1(USER→FORBIDDEN), V7-2(비로그인→UNAUTHORIZED), V7-3(차량명 중복→CONFLICT), V7-4(차량 소프트삭제 후에도 기존 견적 보존 — 별도 브랜드/차량을 새로 만들어 seed를 안 건드리고 검증),
V7-5(옵션 등록→수정→삭제 전체 흐름), V7-6(옵션명 중복→CONFLICT), V7-8(0건 기간→빈 배열, 날짜 역전→VALIDATION),
V7-9(주간 TOP5가 DB 직접 집계와 일치 — 하드코딩 없이 즉석 계산해서 대조), V7-10(로그에 LOGIN/LOGOUT 둘 다, IP·시각 채워짐).

**클라이언트 (WPF, admin/Admin1234!로 로그인 → 관리자 셸 전환 확인, 5개 화면 전부 실제 조작 후 스크린샷)**:
- 08 대시보드: 총 견적 44 · 오늘 견적 41 · 오늘의 인기차량(아반떼 33건), 주간 TOP5 국산/수입 분리 표시.
- 09 차량·브랜드: 차량 탭에서 "테스트카74 (삭제됨)"이 목록에 남아있는 것(is_active=0도 함께 보여줌) 확인, 차량 클릭 → 폼이 정확히 채워짐(브랜드 콤보박스 포함), 브랜드 탭도 동일하게 확인.
- 10 옵션 관리: 차량 콤보박스에서 아반떼 선택 → 선루프/내비게이션/가죽시트 + TestClient가 만든 "(삭제됨)" 옵션까지 정상 표시.
- 11 통계: 기간 조회 → 막대 그래프(일자별 견적 수) + 주간 TOP5.
- 12 로그 조회: 아이디/권한/동작/접속IP/시각 컬럼, admin·테스트 계정들의 LOGIN/LOGOUT이 섞여서 전부 표시됨.

### 의도적으로 안 한 것 (범위 밖)

- 09/10 화면에 이미지 미리보기: 명세에 없어서 텍스트 경로 입력만 받는다.
- 관리자 통계 차트는 실제 차트 라이브러리 없이 `Rectangle` 너비로 그린 막대다 — CLAUDE.md 7절 "일정이 밀릴 때 후순위 제거 순서"의 1번(관리자 통계 차트)과 관련된 항목이라 더 다듬지 않았다. 필요하면 8단계(UI 개선)에서 손볼 것.

---

## 5-2. 8단계 검증 (2026-09-23, 이 PC)

- **TestClient 52개 PASS**: V8-1 정렬 POPULAR(DB 직접 대조) / V8-2 PRICE_ASC / V8-3 모르는 정렬값 → VALIDATION, 연결 유지 / V8-4 option_total / V8-5 견적함 brand_name·image_url / V8-6 관리자 요약·TOP5 brand_name.
- **`test_constraints.sh` 23/23 PASS** (danawa_app 거부가 ERROR 1142 권한 오류인지 따로 확인).
- **클라이언트는 UI Automation으로 실제 조작 + 스크린샷**, 12화면 전부: 로그인 후 복귀(국산차·가격순 유지), 하트 토글, 옵션 체크 합계, 견적 저장·삭제(DB `is_deleted=1`), 관심 해제 → 빈 상태, 검색 0건 빈 상태, 가입 검증 메시지, 관리자 옵션 저장 버튼 연속 2회 → 두 번째 거부(버튼 비활성)·DB 1행.
- **요구사항 대조**: `docs/06_요구사항대조표.md` — ✅ 55 / 🟡 2 / ⚠️ 0 / ❌ 0.
- **메인 하트 연동**: 테스트 계정으로 인기 차량 하트 ♥ → TOP5 아반떼도 ♥, TOP5 아반떼 하트 ♡ → 인기 차량도 ♡, DB 관심 0건 확인(UI 조작·스크린샷).
- **서버 점검 화면**: 일반회원 로그인 중 서버 종료 → 점검 화면·로그아웃 상태 → 꺼진 채 재접속 → 사유 표시 → 서버 기동 후 재접속 → 비로그인 메인. 관리자 로그인 중 서버 종료 → 일반 셸 점검 화면(관리자 창 닫힘 확인) → 재접속 후 admin 재로그인·대시보드 정상. 서버를 끈 채 클라이언트 실행 → 점검 화면으로 시작. 전부 UI 조작·스크린샷으로 확인.

## 5-3. 9단계 통합 테스트 (2026-09-23, 이 PC) — 전부 PASS

| # | 방법 | 결과 |
|---|---|---|
| V9-1 | TestClient: 새 계정 5개가 동시에(`Task.WhenAll`) 서로 다른 차량·옵션으로 견적 저장 | 각자 견적함 1건, 총액 = DB 가격 계산, DB `quotes.user_id` 일치 |
| V9-2 | TestClient: 한 연결에서 응답 안 기다리고 car_detail 20건 연속 발사 | 20건 모두 req_id별로 요청한 car_id와 일치, 중복 없음 |
| V9-3 | UI: 서버 종료 → 서버 점검 화면 → 서버 재기동 → [재접속] | 메인 복귀, "서버 연결됨" |
| V9-4 | `Danawa.TestClient.dll --db-down` (MySQL80 서비스 `Stop-Service`) | brand_list·login·car_list 전부 SERVER_ERROR, 같은 연결 ping OK, 새 연결 수락, 서버 PID 동일. DB 재기동 후 **서버 재시작 없이** 56개 PASS |
| V9-5 | `docs/06_요구사항대조표.md` | **57/57 ✅**. ADM-02는 V9-5a(브랜드 수정)로 확인 |
| V9-6 | `test_constraints.sh` | 23/23 |

- **DB 커넥션 풀 상한 5 (사용자 요청)**: `MySqlConnector`는 기본이 풀 사용·상한 100이었다. `appsettings.json` 접속 문자열에 `Pooling=true;Maximum Pool Size=5;`를 넣었다(코드 변경 없음, `Db.cs` 주석만).
  - 서버는 **연결(세션) 하나당 요청을 순서대로** 처리하므로 한 세션은 DB 연결을 동시에 1개만 쓴다 → 동시 세션이 5개를 넘으면 6번째부터는 풀 반납을 기다린다(기본 Connection Timeout 15초 초과 시 예외 → SERVER_ERROR).
  - **V9-pool**(TestClient): 12세션 × main_summary 10회 동시. 변경 전 기준 측정에서 서버 DB 연결 **12개**(FAIL) → 변경 후 **5개**, 120요청 전부 OK.
  - TestClient의 검증용 DB 조회 연결은 `Pooling=false`로 바꿨다(집계에 섞이지 않게).
- 테스트 후 DB는 **seed로 되돌렸다**(사용자 요청, 7절 5번).
- **완료보고서**: `danawa_v2/다나와자동차v2_완료보고서.md` + `.docx` (v1 대비 A1~A7/B1~B9/C1~C9/D, 요구사항 현황, 통합 테스트, 남은 과제). v1 점검 목록 원본은 `danawa_v2/00_서순_로드맵.md` 1절.

## 6. 3~9단계 검증 재현 방법

```bash
# 1) MySQL 확인
sc query MySQL80

# 2) 빌드
cd dnwa_v2 && dotnet build
# 20~30초 대기 (Smart App Control)

# 3) 서버 (백그라운드)
cd src/Danawa.Server && dotnet bin/Debug/net8.0/Danawa.Server.dll &

# 4) 검증 콘솔 — 56개 PASS 기대 (V9-4만 따로: MySQL80 멈춘 뒤 `... Danawa.TestClient.dll --db-down`)
cd ../Danawa.TestClient && dotnet bin/Debug/net8.0/Danawa.TestClient.dll

# 5) 클라이언트 (수동 확인용, GUI가 뜬다. admin/Admin1234!로 로그인하면 관리자 셸로 전환됨)
cd ../Danawa.Client && dotnet bin/Debug/net8.0-windows/Danawa.Client.dll
```

DB 제약 재검증: `MYSQL_PWD=1234 bash sql/test_constraints.sh` (23종, `~/bin/mysql` 셔임 필요 — 실행하면 실제 데이터 변경이 남는다).

---

## 7. 다음 세션이 제일 먼저 할 일

1. 이 문서 + `CLAUDE.md` + 완료보고서(`danawa_v2/다나와자동차v2_완료보고서.md`)를 읽는다. **서순 0~9단계는 전부 끝났다.**
2. 이 PC라면: MySQL80 서비스 확인, `dotnet --list-sdks`로 8.0.425 확인.
3. 다른 PC라면: .NET 8 SDK → MySQL 8.0.45 → `ddl_v2.sql`/`seed.sql`(utf8mb4 옵션 필수) → admin 실 해시 갱신(비밀번호 `Admin1234!`, PBKDF2-SHA256 210000회, salt 16B / hash 32B) → 3~9단계 재검증(6절).
4. 남은 것은 서순 밖의 선택 과제뿐이다 (완료보고서 "남은 과제"): E항목(관리자 이미지 업로드 / TLS / 랭킹 실시간 푸시), 메인 TOP5 "관심 N" 즉시 갱신, 실제 운영 데이터 적재(현재 seed는 샘플).
5. **DB는 2026-09-23 17:35에 seed 상태로 되돌렸고, 9단계 테스트 뒤 18:05경 다시 seed로 되돌렸다**(사용자 요청): `ddl_v2.sql` → `seed.sql` → admin 실 해시 재적용(`Admin1234!`, 오프라인 재계산으로 일치 확인). 브랜드 5 / 차량 10 / 옵션 22 / 회원 4(admin, user01~03) / 관심 19 / 견적 14. 되돌리기 직전 상태는 그 세션 스크래치 폴더에 mysqldump로 백업했다(세션 전용 임시 폴더라 다른 세션에서는 없을 수 있음).
   **주의**: TestClient(56개)와 `test_constraints.sh`는 실행할 때마다 데이터를 다시 쌓는다. 보고서용 화면을 seed 상태로 찍으려면 스크린샷을 먼저 찍고 테스트를 돌리거나, 테스트 후 같은 절차로 다시 되돌릴 것.
