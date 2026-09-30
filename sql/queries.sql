USE danawa_car_v2;

SELECT '--- Q1. 오늘의 인기 차량 (오늘 견적 수 1위, 범위식 조건) ---' AS '';
SELECT c.car_id, c.car_name, b.brand_name, c.car_price, COUNT(*) AS quote_count
FROM quotes q
JOIN cars c   ON c.car_id = q.car_id
JOIN brands b ON b.brand_id = c.brand_id
WHERE q.created_at >= CURDATE()
  AND q.created_at <  CURDATE() + INTERVAL 1 DAY
  AND q.is_deleted = 0
GROUP BY c.car_id, c.car_name, b.brand_name, c.car_price
ORDER BY quote_count DESC, c.car_id
LIMIT 1;

SELECT '--- Q2-1. 주간 관심 TOP5 (국산) : 최근 7일 = INTERVAL 6 DAY ---' AS '';
SELECT c.car_name, b.brand_name, COUNT(*) AS favorite_count
FROM favorites f
JOIN cars c   ON c.car_id = f.car_id AND c.is_active = 1
JOIN brands b ON b.brand_id = c.brand_id
WHERE b.origin_type = 'DOMESTIC'
  AND f.created_at >= CURDATE() - INTERVAL 6 DAY
GROUP BY c.car_id, c.car_name, b.brand_name
ORDER BY favorite_count DESC, c.car_name
LIMIT 5;

SELECT '--- Q2-2. 주간 관심 TOP5 (수입) ---' AS '';
SELECT c.car_name, b.brand_name, COUNT(*) AS favorite_count
FROM favorites f
JOIN cars c   ON c.car_id = f.car_id AND c.is_active = 1
JOIN brands b ON b.brand_id = c.brand_id
WHERE b.origin_type = 'IMPORT'
  AND f.created_at >= CURDATE() - INTERVAL 6 DAY
GROUP BY c.car_id, c.car_name, b.brand_name
ORDER BY favorite_count DESC, c.car_name
LIMIT 5;

SELECT '--- Q3-1. 견적함 헤더 (user01) : 견적 1건 = 1행 ---' AS '';
SELECT q.quote_id, q.car_name_snap, q.car_price_snap, q.total_price, q.created_at
FROM quotes q
WHERE q.user_id = 2 AND q.is_deleted = 0
ORDER BY q.created_at DESC;

SELECT '--- Q3-2. 위 견적들의 옵션 (헤더와 분리 조회) ---' AS '';
SELECT qo.quote_id, qo.option_name_snap, qo.option_price_snap
FROM quote_options qo
JOIN quotes q ON q.quote_id = qo.quote_id
WHERE q.user_id = 2 AND q.is_deleted = 0
ORDER BY qo.quote_id, qo.option_id;

SELECT '--- Q3-3. 총액 검산: 차량가 + 옵션합 = total_price 인가 ---' AS '';
SELECT q.quote_id, q.car_name_snap, q.car_price_snap,
       COALESCE(SUM(qo.option_price_snap),0) AS option_sum,
       q.car_price_snap + COALESCE(SUM(qo.option_price_snap),0) AS calc_total,
       q.total_price,
       CASE WHEN q.car_price_snap + COALESCE(SUM(qo.option_price_snap),0) = q.total_price
            THEN 'OK' ELSE 'MISMATCH' END AS check_result
FROM quotes q
LEFT JOIN quote_options qo ON qo.quote_id = q.quote_id
GROUP BY q.quote_id, q.car_name_snap, q.car_price_snap, q.total_price
ORDER BY q.quote_id;

SELECT '--- Q4. 차량 검색 (차량명 또는 브랜드명 부분 일치, 비활성 제외) ---' AS '';
SELECT c.car_id, b.brand_name, c.car_name, c.car_price
FROM cars c
JOIN brands b ON b.brand_id = c.brand_id
WHERE c.is_active = 1 AND b.is_active = 1
  AND (c.car_name LIKE CONCAT('%','쏘','%') OR b.brand_name LIKE CONCAT('%','쏘','%'))
ORDER BY b.brand_name, c.car_name;

SELECT '--- Q5-1. 관리자 통계: 총 견적 수 ---' AS '';
SELECT COUNT(*) AS total_quotes FROM quotes WHERE is_deleted = 0;

SELECT '--- Q5-2. 관리자 통계: 기간별 일자별 견적 건수 ---' AS '';
SELECT DATE(created_at) AS day, COUNT(*) AS cnt
FROM quotes
WHERE created_at >= CURDATE() - INTERVAL 6 DAY
  AND created_at <  CURDATE() + INTERVAL 1 DAY
GROUP BY DATE(created_at)
ORDER BY day;

SELECT '--- Q5-3. 관리자 통계: 주간 견적 TOP5 (국산) ---' AS '';
SELECT c.car_name, COUNT(*) AS quote_count
FROM quotes q
JOIN cars c   ON c.car_id = q.car_id
JOIN brands b ON b.brand_id = c.brand_id
WHERE b.origin_type = 'DOMESTIC'
  AND q.created_at >= CURDATE() - INTERVAL 6 DAY
  AND q.is_deleted = 0
GROUP BY c.car_id, c.car_name
ORDER BY quote_count DESC, c.car_name
LIMIT 5;

SELECT '--- Q6. 접속 로그 조회 (로그인/로그아웃 + IP) ---' AS '';
SELECT u.login_id, u.role, l.action, l.ip_addr, l.created_at
FROM access_log l
JOIN users u ON u.user_id = l.user_id
ORDER BY l.created_at DESC
LIMIT 10;
