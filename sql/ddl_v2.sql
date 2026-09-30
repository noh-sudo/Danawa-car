/* =========================================================
   다나와 자동차 v2 — DDL
   대상: MySQL 8.0
   문자셋: utf8mb4 / utf8mb4_unicode_ci
   ========================================================= */

DROP DATABASE IF EXISTS danawa_car_v2;
CREATE DATABASE danawa_car_v2
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;
USE danawa_car_v2;

/* ---------------------------------------------------------
   1. users — 회원
   v1 대비: login_id(이름)가 PK였던 것을 대리키 user_id로 교체.
            roles 테이블(값만 있고 속성이 없음)을 없애고 ENUM으로 대체.
            무염 SHA-256 → PBKDF2(salt + 반복횟수)로 교체.
   --------------------------------------------------------- */
CREATE TABLE users (
  user_id    INT           NOT NULL AUTO_INCREMENT COMMENT '회원 대리키',
  login_id   VARCHAR(30)   NOT NULL                COMMENT '로그인 아이디',
  pw_hash    VARBINARY(32) NOT NULL                COMMENT 'PBKDF2-SHA256 해시 32B',
  pw_salt    VARBINARY(16) NOT NULL                COMMENT '사용자별 salt 16B',
  pw_iter    INT           NOT NULL DEFAULT 210000 COMMENT 'PBKDF2 반복 횟수',
  role       ENUM('USER','ADMIN') NOT NULL DEFAULT 'USER' COMMENT '권한',
  is_active  TINYINT(1)    NOT NULL DEFAULT 1      COMMENT '1=사용, 0=탈퇴',
  created_at DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (user_id),
  UNIQUE KEY uq_users_login_id (login_id),
  CONSTRAINT ck_users_iter CHECK (pw_iter >= 100000)
) ENGINE=InnoDB COMMENT='회원';

/* ---------------------------------------------------------
   2. brands — 브랜드
   v1 대비: 브랜드명이 PK였던 것을 대리키로 교체.
            origin_type(국산/수입)을 cars에서 brands로 이동.
            → 같은 브랜드 안에서 값이 엇갈릴 수 없게 된다.
   --------------------------------------------------------- */
CREATE TABLE brands (
  brand_id    INT          NOT NULL AUTO_INCREMENT COMMENT '브랜드 대리키',
  brand_name  VARCHAR(30)  NOT NULL                COMMENT '브랜드명',
  brand_icon  VARCHAR(255) NULL                    COMMENT '로고 이미지 경로',
  origin_type ENUM('DOMESTIC','IMPORT') NOT NULL   COMMENT '국산/수입',
  is_active   TINYINT(1)   NOT NULL DEFAULT 1,
  created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (brand_id),
  UNIQUE KEY uq_brands_name (brand_name)
) ENGINE=InnoDB COMMENT='브랜드';

/* ---------------------------------------------------------
   3. cars — 차량
   v1 대비: 차량명 PK → 대리키. DELETE 대신 is_active 소프트 삭제.
            연비 역전(min > max), 음수 가격을 CHECK로 차단.
   --------------------------------------------------------- */
CREATE TABLE cars (
  car_id       INT          NOT NULL AUTO_INCREMENT COMMENT '차량 대리키',
  brand_id     INT          NOT NULL                COMMENT '브랜드 FK',
  car_name     VARCHAR(50)  NOT NULL                COMMENT '차량명',
  image_url    VARCHAR(255) NULL                    COMMENT '차량 이미지 경로',
  car_price    INT          NOT NULL                COMMENT '기본 가격(옵션 제외)',
  fuel_min     DECIMAL(4,2) NULL                    COMMENT '최소 연비',
  fuel_max     DECIMAL(4,2) NULL                    COMMENT '최대 연비',
  release_date DATE         NULL                    COMMENT '출시일',
  is_active    TINYINT(1)   NOT NULL DEFAULT 1      COMMENT '1=판매중, 0=삭제(소프트)',
  created_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (car_id),
  UNIQUE KEY uq_cars_brand_name (brand_id, car_name),
  KEY ix_cars_brand_active (brand_id, is_active),
  KEY ix_cars_name (car_name),
  CONSTRAINT fk_cars_brand FOREIGN KEY (brand_id) REFERENCES brands(brand_id),
  CONSTRAINT ck_cars_price CHECK (car_price >= 0),
  CONSTRAINT ck_cars_fuel  CHECK (fuel_min IS NULL OR fuel_max IS NULL OR fuel_min <= fuel_max)
) ENGINE=InnoDB COMMENT='차량';

/* ---------------------------------------------------------
   4. car_options — 차량 옵션
   --------------------------------------------------------- */
CREATE TABLE car_options (
  option_id   INT         NOT NULL AUTO_INCREMENT COMMENT '옵션 대리키',
  car_id      INT         NOT NULL                COMMENT '차량 FK',
  option_name VARCHAR(50) NOT NULL                COMMENT '옵션명',
  price       INT         NOT NULL DEFAULT 0      COMMENT '옵션 가격',
  is_active   TINYINT(1)  NOT NULL DEFAULT 1,
  PRIMARY KEY (option_id),
  UNIQUE KEY uq_options_car_name (car_id, option_name),
  CONSTRAINT fk_options_car FOREIGN KEY (car_id) REFERENCES cars(car_id),
  CONSTRAINT ck_options_price CHECK (price >= 0)
) ENGINE=InnoDB COMMENT='차량 옵션';

/* ---------------------------------------------------------
   5. quotes — 견적 (v1의 quotes + quote_detail 통합)
   v1은 quotes:quote_detail이 1:1인데 테이블만 갈라져 있었다.
   1:1이면 합치는 것이 맞다 — JOIN 한 번이 줄고 중복 출력 버그의 원인도 사라진다.
   차량명·차량가는 저장 시점 스냅샷을 함께 보관한다.
   --------------------------------------------------------- */
CREATE TABLE quotes (
  quote_id       INT         NOT NULL AUTO_INCREMENT COMMENT '견적 대리키',
  user_id        INT         NOT NULL                COMMENT '회원 FK',
  car_id         INT         NOT NULL                COMMENT '차량 FK',
  car_name_snap  VARCHAR(50) NOT NULL                COMMENT '차량명 스냅샷',
  car_price_snap INT         NOT NULL                COMMENT '차량 기본가 스냅샷',
  total_price    INT         NOT NULL                COMMENT '서버가 재계산한 총액',
  is_deleted     TINYINT(1)  NOT NULL DEFAULT 0      COMMENT '1=견적함에서 삭제',
  created_at     DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (quote_id),
  KEY ix_quotes_user (user_id, is_deleted, created_at),
  KEY ix_quotes_created (created_at),
  KEY ix_quotes_car_created (car_id, created_at),
  CONSTRAINT fk_quotes_user FOREIGN KEY (user_id) REFERENCES users(user_id),
  CONSTRAINT fk_quotes_car  FOREIGN KEY (car_id)  REFERENCES cars(car_id),
  CONSTRAINT ck_quotes_total CHECK (total_price >= 0),
  CONSTRAINT ck_quotes_carprice CHECK (car_price_snap >= 0)
) ENGINE=InnoDB COMMENT='견적';

/* ---------------------------------------------------------
   6. quote_options — 견적에 포함된 옵션
   복합 PK로 같은 견적에 같은 옵션이 두 번 들어가는 것을 막는다.
   옵션명·단가 스냅샷을 보관해 나중에 옵션 가격을 바꿔도
   과거 견적 금액이 흔들리지 않게 한다.
   --------------------------------------------------------- */
CREATE TABLE quote_options (
  quote_id          INT         NOT NULL COMMENT '견적 FK',
  option_id         INT         NOT NULL COMMENT '옵션 FK',
  option_name_snap  VARCHAR(50) NOT NULL COMMENT '옵션명 스냅샷',
  option_price_snap INT         NOT NULL COMMENT '옵션 단가 스냅샷',
  PRIMARY KEY (quote_id, option_id),
  KEY ix_qo_option (option_id),
  CONSTRAINT fk_qo_quote  FOREIGN KEY (quote_id)  REFERENCES quotes(quote_id),
  CONSTRAINT fk_qo_option FOREIGN KEY (option_id) REFERENCES car_options(option_id),
  CONSTRAINT ck_qo_price CHECK (option_price_snap >= 0)
) ENGINE=InnoDB COMMENT='견적 옵션';

/* ---------------------------------------------------------
   7. favorites — 관심 목록
   v1은 UNIQUE가 없어 같은 차를 몇 번이든 담을 수 있었고,
   is_deleted로 지우다 보니 해제 후 재등록이 꼬였다.
   (user_id, car_id) 복합 PK + 실제 DELETE로 토글을 단순하게 만든다.
   --------------------------------------------------------- */
CREATE TABLE favorites (
  user_id    INT      NOT NULL COMMENT '회원 FK',
  car_id     INT      NOT NULL COMMENT '차량 FK',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (user_id, car_id),
  KEY ix_fav_car_created (car_id, created_at),
  CONSTRAINT fk_fav_user FOREIGN KEY (user_id) REFERENCES users(user_id),
  CONSTRAINT fk_fav_car  FOREIGN KEY (car_id)  REFERENCES cars(car_id)
) ENGINE=InnoDB COMMENT='관심 목록';

/* ---------------------------------------------------------
   8. access_log — 접속 로그
   v1은 로그인만, 그것도 기록 함수가 호출되지 않았다.
   action으로 로그인/로그아웃을 함께 남기고 접속 IP도 보관한다.
   --------------------------------------------------------- */
CREATE TABLE access_log (
  log_id     BIGINT      NOT NULL AUTO_INCREMENT,
  user_id    INT         NOT NULL COMMENT '회원 FK',
  action     ENUM('LOGIN','LOGOUT') NOT NULL COMMENT '동작',
  ip_addr    VARCHAR(45) NOT NULL COMMENT '접속 IP (IPv6 고려 45자)',
  created_at DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (log_id),
  KEY ix_log_created (created_at),
  KEY ix_log_user_created (user_id, created_at),
  CONSTRAINT fk_log_user FOREIGN KEY (user_id) REFERENCES users(user_id)
) ENGINE=InnoDB COMMENT='접속 로그';

/* =========================================================
   서버 전용 계정 — 필요한 동작만 준다.
   DDL(CREATE/DROP/ALTER) 권한 없음, DELETE는 favorites에만.
   ========================================================= */
DROP USER IF EXISTS 'danawa_app'@'localhost';
CREATE USER 'danawa_app'@'localhost' IDENTIFIED BY 'CHANGE_ME_APP_PASSWORD';

GRANT SELECT ON danawa_car_v2.* TO 'danawa_app'@'localhost';

GRANT INSERT ON danawa_car_v2.users         TO 'danawa_app'@'localhost';
GRANT INSERT ON danawa_car_v2.quotes        TO 'danawa_app'@'localhost';
GRANT INSERT ON danawa_car_v2.quote_options TO 'danawa_app'@'localhost';
GRANT INSERT ON danawa_car_v2.favorites     TO 'danawa_app'@'localhost';
GRANT INSERT ON danawa_car_v2.access_log    TO 'danawa_app'@'localhost';
GRANT INSERT ON danawa_car_v2.brands        TO 'danawa_app'@'localhost';
GRANT INSERT ON danawa_car_v2.cars          TO 'danawa_app'@'localhost';
GRANT INSERT ON danawa_car_v2.car_options   TO 'danawa_app'@'localhost';

GRANT UPDATE ON danawa_car_v2.quotes      TO 'danawa_app'@'localhost';  -- 소프트 삭제
GRANT UPDATE ON danawa_car_v2.brands      TO 'danawa_app'@'localhost';  -- 관리자 수정
GRANT UPDATE ON danawa_car_v2.cars        TO 'danawa_app'@'localhost';  -- 관리자 수정/소프트 삭제
GRANT UPDATE ON danawa_car_v2.car_options TO 'danawa_app'@'localhost';  -- 관리자 수정

GRANT DELETE ON danawa_car_v2.favorites TO 'danawa_app'@'localhost';    -- 관심 해제만 실제 삭제

FLUSH PRIVILEGES;
