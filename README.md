# 다나와 자동차

**C# TCP 서버 + WPF(MVVM) 클라이언트 + MySQL 8.0** 구조로 만든 프로젝트입니다.


```
danawa_v2/
├── sql/                   DDL, 샘플 데이터, 제약 검증 스크립트
└── dnwa_v2/                실제 코드 (DanawaV2.sln)
    └── src/
        ├── Danawa.Shared/     서버·클라이언트 공용 (프로토콜, DTO)
        ├── Danawa.Server/     TCP 서버 (콘솔)
        └── Danawa.Client/     WPF 클라이언트
```

----
## 사전 준비

**.NET 8 SDK**와 **MySQL 8.0**이 필요합니다.

```
winget install Microsoft.DotNet.SDK.8
winget install Oracle.MySQL
```

두 버전 다 설치돼 있는지 확인해주세요.

```
dotnet --list-sdks     # 8.0.x가 보여야 함
mysql --version         # 8.0.x
```

----
## 서버를 키기 전에 확인해주세요

**1. DB 스키마 + 샘플 데이터 적용**

`sql/ddl_v2.sql`이 스키마·테이블·제약조건·최소권한 계정(`danawa_app`)을 만듭니다. 한글이 깨지지 않도록 **반드시 `utf8mb4` 옵션을 붙여주세요.**

```bash
mysql --default-character-set=utf8mb4 -uroot -p < sql/ddl_v2.sql
mysql --default-character-set=utf8mb4 -uroot -p < sql/seed.sql
```

`sql/ddl_v2.sql`의 `danawa_app` 비밀번호는 `CHANGE_ME_APP_PASSWORD` 자리표시자로 돼 있습니다. 실행 전에 원하는 비밀번호로 바꿔주세요(아래 `appsettings.json`에 넣을 값과 같아야 합니다).

**2. 서버 설정 파일**

`dnwa_v2/src/Danawa.Server/appsettings.json`은 저장소에 올라가지 않습니다(`.gitignore`). 직접 만들어주세요.

```json
{
  "Port": 9000,
  "DbConnectionString": "Server=127.0.0.1;Port=3306;Database=danawa_car_v2;User ID=danawa_app;Password=<위에서 정한 비밀번호>;Pooling=true;Maximum Pool Size=5;"
}
```

**3. 관리자 계정**

`seed.sql`의 `admin` 계정은 `pw_hash`가 테스트용 더미 값이라 그대로는 로그인이 안 됩니다. 회원가입 경로로는 관리자 계정을 만들 수 없으므로(R5), PBKDF2-SHA256(반복 210000, salt 16B) 해시를 직접 계산해 `users.pw_hash`/`pw_salt`/`pw_iter`를 갱신해야 합니다.

**4. 제약조건 확인 (선택)**

```bash
MYSQL_PWD=<root 비밀번호> bash sql/test_constraints.sh
```

23종 전부 "거부됨 OK"가 나와야 정상입니다.

----
## 실행

같은 PC에서 서버·클라이언트를 함께 테스트하는 경우:

```bash
cd dnwa_v2
dotnet build
# Windows Smart App Control 때문에 빌드 직후 바로 실행하면 막힐 수 있습니다.

dotnet run --project src/Danawa.Server            # 터미널 1
dotnet run --project src/Danawa.Client             # 터미널 2
```

다른 PC에서 클라이언트만 실행하는 경우:

1. `src/Danawa.Client`와 `src/Danawa.Shared` 두 폴더를 상대 경로(형제 폴더)를 유지한 채로 옮깁니다.
2. `Danawa.Client/appsettings.json`의 `ServerHost`를 서버 PC의 LAN IP로 바꿉니다.
   ```json
   { "ServerHost": "ip주소", "ServerPort": 9000 }
   ```
3. 서버 PC의 방화벽에서 TCP 9000 인바운드를 허용합니다.
4. 클라이언트 PC에서:
   ```bash
   dotnet run --project Danawa.Client/Danawa.Client.csproj
   ```

클라이언트에서 관리자 계정 로그인은 위 3번에서 실 해시로 갱신한 `admin` 계정으로 합니다.
