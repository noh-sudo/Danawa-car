USE danawa_car_v2;

/* 회원 — pw_hash/pw_salt는 테스트용 더미 바이너리 */
INSERT INTO users (login_id, pw_hash, pw_salt, role) VALUES
 ('admin',  UNHEX(REPEAT('AB',32)), UNHEX(REPEAT('11',16)), 'ADMIN'),
 ('user01', UNHEX(REPEAT('CD',32)), UNHEX(REPEAT('22',16)), 'USER'),
 ('user02', UNHEX(REPEAT('EF',32)), UNHEX(REPEAT('33',16)), 'USER'),
 ('user03', UNHEX(REPEAT('12',32)), UNHEX(REPEAT('44',16)), 'USER');

INSERT INTO brands (brand_name, brand_icon, origin_type) VALUES
 ('현대',   'img/brand/hyundai.png', 'DOMESTIC'),
 ('기아',   'img/brand/kia.png',     'DOMESTIC'),
 ('제네시스','img/brand/genesis.png', 'DOMESTIC'),
 ('BMW',    'img/brand/bmw.png',     'IMPORT'),
 ('벤츠',   'img/brand/benz.png',    'IMPORT');

INSERT INTO cars (brand_id, car_name, image_url, car_price, fuel_min, fuel_max, release_date) VALUES
 (1, '아반떼',  'img/car/avante.png',  19000000, 14.50, 15.70, '2025-03-10'),
 (1, '쏘나타',  'img/car/sonata.png',  27000000, 12.30, 13.80, '2025-06-01'),
 (1, '팰리세이드','img/car/palisade.png',43000000, 9.20, 10.50, '2026-01-15'),
 (2, '쏘렌토',  'img/car/sorento.png', 35000000, 10.80, 12.10, '2025-08-20'),
 (2, 'K5',      'img/car/k5.png',      28000000, 12.70, 13.90, '2025-05-05'),
 (3, 'G80',     'img/car/g80.png',     62000000,  9.50, 10.80, '2025-11-11'),
 (4, '320i',    'img/car/320i.png',    58000000, 11.20, 12.40, '2025-09-09'),
 (4, 'X5',      'img/car/x5.png',      98000000,  8.10,  9.30, '2026-02-02'),
 (5, 'E250',    'img/car/e250.png',    71000000, 10.10, 11.20, '2025-12-12'),
 (5, 'GLC',     'img/car/glc.png',     85000000,  9.00, 10.20, '2026-03-03');

INSERT INTO car_options (car_id, option_name, price) VALUES
 (1,'선루프',900000),(1,'내비게이션',700000),(1,'가죽시트',1200000),
 (2,'선루프',1000000),(2,'HUD',1500000),(2,'통풍시트',800000),
 (3,'파노라마루프',2000000),(3,'7인승 패키지',1800000),
 (4,'스마트크루즈',1400000),(4,'전동트렁크',600000),
 (5,'선루프',1000000),(5,'하이패스',300000),
 (6,'렉시콘사운드',2500000),(6,'후륜조향',3000000),
 (7,'M스포츠패키지',4500000),(7,'하만카돈',1900000),
 (8,'파노라마루프',3200000),(8,'에어서스펜션',5000000),
 (9,'AMG라인',3800000),(9,'부메스터',2200000),
 (10,'AMG라인',3500000),(10,'디지털라이트',2800000);

/* 견적 — 오늘/최근 7일/그 이전으로 흩뿌린다 */
INSERT INTO quotes (user_id, car_id, car_name_snap, car_price_snap, total_price, created_at) VALUES
 (2, 1, '아반떼', 19000000, 20600000, NOW()),
 (3, 1, '아반떼', 19000000, 19700000, NOW()),
 (4, 1, '아반떼', 19000000, 19000000, NOW()),
 (2, 2, '쏘나타', 27000000, 28800000, NOW()),
 (3, 4, '쏘렌토', 35000000, 36400000, NOW() - INTERVAL 1 DAY),
 (4, 4, '쏘렌토', 35000000, 35600000, NOW() - INTERVAL 2 DAY),
 (2, 5, 'K5',     28000000, 29000000, NOW() - INTERVAL 3 DAY),
 (3, 7, '320i',   58000000, 62500000, NOW() - INTERVAL 1 DAY),
 (4, 7, '320i',   58000000, 59900000, NOW() - INTERVAL 4 DAY),
 (2, 8, 'X5',     98000000,101200000, NOW() - INTERVAL 2 DAY),
 (3, 9, 'E250',   71000000, 74800000, NOW() - INTERVAL 5 DAY),
 (4,10, 'GLC',    85000000, 88500000, NOW() - INTERVAL 6 DAY),
 (2, 6, 'G80',    62000000, 64500000, NOW() - INTERVAL 9 DAY),
 (3, 3, '팰리세이드',43000000,45000000, NOW() - INTERVAL 12 DAY);

INSERT INTO quote_options (quote_id, option_id, option_name_snap, option_price_snap) VALUES
 (1,1,'선루프',900000),(1,2,'내비게이션',700000),
 (2,2,'내비게이션',700000),
 (4,4,'선루프',1000000),(4,6,'통풍시트',800000),
 (5,9,'스마트크루즈',1400000),
 (6,10,'전동트렁크',600000),
 (7,11,'선루프',1000000),
 (8,15,'M스포츠패키지',4500000),
 (9,16,'하만카돈',1900000),
 (10,17,'파노라마루프',3200000),
 (11,19,'AMG라인',3800000),
 (12,21,'AMG라인',3500000),
 (13,13,'렉시콘사운드',2500000),
 (14,7,'파노라마루프',2000000);

INSERT INTO favorites (user_id, car_id, created_at) VALUES
 (2,1,NOW()),(3,1,NOW() - INTERVAL 1 DAY),(4,1,NOW() - INTERVAL 2 DAY),
 (2,2,NOW() - INTERVAL 1 DAY),(3,2,NOW() - INTERVAL 3 DAY),
 (2,4,NOW() - INTERVAL 2 DAY),(3,4,NOW() - INTERVAL 4 DAY),(4,4,NOW() - INTERVAL 5 DAY),
 (2,5,NOW() - INTERVAL 6 DAY),
 (2,7,NOW()),(3,7,NOW() - INTERVAL 1 DAY),(4,7,NOW() - INTERVAL 2 DAY),
 (2,8,NOW() - INTERVAL 3 DAY),(3,8,NOW() - INTERVAL 4 DAY),
 (2,9,NOW() - INTERVAL 5 DAY),(3,9,NOW() - INTERVAL 6 DAY),
 (2,10,NOW() - INTERVAL 2 DAY),
 (3,6,NOW() - INTERVAL 10 DAY),
 (4,6,NOW() - INTERVAL 8 DAY);

INSERT INTO access_log (user_id, action, ip_addr, created_at) VALUES
 (2,'LOGIN','192.168.0.21',NOW() - INTERVAL 3 HOUR),
 (2,'LOGOUT','192.168.0.21',NOW() - INTERVAL 2 HOUR),
 (3,'LOGIN','192.168.0.22',NOW() - INTERVAL 1 DAY),
 (3,'LOGOUT','192.168.0.22',NOW() - INTERVAL 1 DAY + INTERVAL 40 MINUTE),
 (1,'LOGIN','127.0.0.1',NOW() - INTERVAL 5 HOUR),
 (4,'LOGIN','192.168.0.24',NOW() - INTERVAL 2 DAY);
