#!/bin/bash
# 제약조건이 실제로 막아주는지 확인한다. 전부 "거부"되어야 통과.
DB=danawa_car_v2
PASS=0; FAIL=0

expect_fail () {
  local name="$1"; shift
  local sql="$1"
  local out
  out=$(mysql --default-character-set=utf8mb4 -uroot -D "$DB" -e "$sql" 2>&1)
  if [ $? -ne 0 ]; then
    PASS=$((PASS+1))
    printf '  [거부됨 OK] %-38s %s\n' "$name" "$(echo "$out" | head -1 | cut -c1-110)"
  else
    FAIL=$((FAIL+1))
    printf '  [통과됨 ##] %-38s <-- 막지 못함!\n' "$name"
  fi
}

expect_ok () {
  local name="$1"; shift
  local sql="$1"
  local out
  out=$(mysql --default-character-set=utf8mb4 -uroot -D "$DB" -e "$sql" 2>&1)
  if [ $? -eq 0 ]; then
    PASS=$((PASS+1)); printf '  [정상 OK  ] %s\n' "$name"
  else
    FAIL=$((FAIL+1)); printf '  [실패 ##  ] %-38s %s\n' "$name" "$(echo "$out" | head -1)"
  fi
}

echo "== 1. UNIQUE / PRIMARY KEY =="
expect_fail "users.login_id 중복"          "INSERT INTO users(login_id,pw_hash,pw_salt) VALUES('user01',UNHEX(REPEAT('00',32)),UNHEX(REPEAT('00',16)));"
expect_fail "brands.brand_name 중복"       "INSERT INTO brands(brand_name,origin_type) VALUES('현대','IMPORT');"
expect_fail "cars (brand_id,car_name) 중복" "INSERT INTO cars(brand_id,car_name,car_price) VALUES(1,'아반떼',19000000);"
expect_fail "favorites 같은 차 중복 담기"   "INSERT INTO favorites(user_id,car_id) VALUES(2,1);"
expect_fail "quote_options 같은 옵션 2번"   "INSERT INTO quote_options VALUES(1,1,'선루프',900000);"

echo "== 2. CHECK =="
expect_fail "cars.car_price 음수"           "INSERT INTO cars(brand_id,car_name,car_price) VALUES(1,'테스트차',-1);"
expect_fail "cars 연비 역전(min>max)"       "INSERT INTO cars(brand_id,car_name,car_price,fuel_min,fuel_max) VALUES(1,'테스트차2',1000,20.00,10.00);"
expect_fail "car_options.price 음수"        "INSERT INTO car_options(car_id,option_name,price) VALUES(1,'테스트옵션',-500);"
expect_fail "quotes.total_price 음수"       "INSERT INTO quotes(user_id,car_id,car_name_snap,car_price_snap,total_price) VALUES(2,1,'아반떼',19000000,-100);"
expect_fail "users.pw_iter 너무 작음"       "INSERT INTO users(login_id,pw_hash,pw_salt,pw_iter) VALUES('weak',UNHEX(REPEAT('00',32)),UNHEX(REPEAT('00',16)),1000);"

echo "== 3. FOREIGN KEY =="
expect_fail "cars.brand_id 없는 브랜드"     "INSERT INTO cars(brand_id,car_name,car_price) VALUES(999,'유령차',1000);"
expect_fail "quotes.user_id 없는 회원"      "INSERT INTO quotes(user_id,car_id,car_name_snap,car_price_snap,total_price) VALUES(999,1,'아반떼',19000000,19000000);"
expect_fail "참조 중인 브랜드 DELETE"       "DELETE FROM brands WHERE brand_id=1;"

echo "== 4. ENUM / NOT NULL =="
expect_fail "users.role 정의 밖의 값"       "INSERT INTO users(login_id,pw_hash,pw_salt,role) VALUES('x1',UNHEX(REPEAT('00',32)),UNHEX(REPEAT('00',16)),'SUPER');"
expect_fail "access_log.ip_addr NULL"       "INSERT INTO access_log(user_id,action,ip_addr) VALUES(2,'LOGIN',NULL);"

echo
echo "== 5. danawa_app 최소권한 =="
APP="mysql --default-character-set=utf8mb4 -udanawa_app -pApp!Danawa2026 -D $DB"
app_fail () {
  local name="$1"; local sql="$2"; local out
  out=$($APP -e "$sql" 2>&1)
  if [ $? -ne 0 ]; then PASS=$((PASS+1)); printf '  [거부됨 OK] %-38s %s\n' "$name" "$(echo "$out"|head -1|cut -c1-110)"
  else FAIL=$((FAIL+1)); printf '  [통과됨 ##] %-38s <-- 막지 못함!\n' "$name"; fi
}
app_ok () {
  local name="$1"; local sql="$2"; local out
  out=$($APP -e "$sql" 2>&1)
  if [ $? -eq 0 ]; then PASS=$((PASS+1)); printf '  [정상 OK  ] %s\n' "$name"
  else FAIL=$((FAIL+1)); printf '  [실패 ##  ] %-38s %s\n' "$name" "$(echo "$out"|head -1)"; fi
}
app_fail "DELETE FROM cars"        "DELETE FROM cars WHERE car_id=1;"
app_fail "DELETE FROM quotes"      "DELETE FROM quotes WHERE quote_id=1;"
app_fail "DROP TABLE favorites"    "DROP TABLE favorites;"
app_fail "ALTER TABLE cars"        "ALTER TABLE cars ADD COLUMN tmp INT;"
app_fail "CREATE TABLE"            "CREATE TABLE tmp_t(a INT);"
app_ok   "SELECT 허용"             "SELECT COUNT(*) FROM cars;"
app_ok   "favorites DELETE 허용"   "DELETE FROM favorites WHERE user_id=4 AND car_id=6;"
app_ok   "quotes 소프트삭제 UPDATE" "UPDATE quotes SET is_deleted=1 WHERE quote_id=14;"

echo
echo "-------- 결과: 통과 $PASS / 실패 $FAIL --------"
[ $FAIL -eq 0 ] || exit 1
