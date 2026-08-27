-- Migration script: Import backup playtime data into PostgreSQL
-- Generated from: club-playtime (1).db
-- Players: 30
-- Daily records: 566

-- Player mapping (backup local ID -> RobloxUserId):
--   LocalId=2 -> RobloxUserId=979135569 (tobiasfunder) 698581s
--   LocalId=6 -> RobloxUserId=3850928177 (MinakoArisato24) 52085s
--   LocalId=7 -> RobloxUserId=10304177841 (Qaz_palm) 18901s
--   LocalId=8 -> RobloxUserId=2857560695 (l_sudda) 207461s
--   LocalId=9 -> RobloxUserId=1913855092 (subtoviper12) 351108s
--   LocalId=11 -> RobloxUserId=2051107290 (justcherishme) 151541s
--   LocalId=12 -> RobloxUserId=1538890744 (Zainab_IsCute) 15420s
--   LocalId=13 -> RobloxUserId=414303227 (saanavi_thebeast) 26583s
--   LocalId=14 -> RobloxUserId=7794493516 (kaya_maitra) 139580s
--   LocalId=15 -> RobloxUserId=946057131 (tossinsomsalad) 413524s
--   LocalId=16 -> RobloxUserId=2513720962 (Velourify) 12962s
--   LocalId=17 -> RobloxUserId=1723196157 (bryan_58b) 69845s
--   LocalId=18 -> RobloxUserId=3726921662 (elpepemojidecalavera) 32905s
--   LocalId=19 -> RobloxUserId=4027874914 (Mx_Cherry6) 51125s
--   LocalId=20 -> RobloxUserId=3827442122 (saathvika276) 68170s
--   LocalId=21 -> RobloxUserId=9280088192 (STEALOP78) 182660s
--   LocalId=22 -> RobloxUserId=6127300164 (nodforaslap) 248183s
--   LocalId=23 -> RobloxUserId=1937688587 (afrenchfighter) 108312s
--   LocalId=25 -> RobloxUserId=7808878766 (kotes_18) 161239s
--   LocalId=26 -> RobloxUserId=5133395976 (elgatooti) 328437s
--   LocalId=27 -> RobloxUserId=2243793833 (KKB05Nihat) 126288s
--   LocalId=28 -> RobloxUserId=2281712407 (EGIRLSLAYER04) 51976s
--   LocalId=29 -> RobloxUserId=3237411560 (anarchy_xyz) 42762s
--   LocalId=30 -> RobloxUserId=2333869817 (Redshiii) 299197s
--   LocalId=31 -> RobloxUserId=5704757917 (Kostia_bel10) 47226s
--   LocalId=32 -> RobloxUserId=3352126866 (1x11x111x1111x1112) 56826s
--   LocalId=33 -> RobloxUserId=63245367 (ThePimpleGamer) 27841s
--   LocalId=34 -> RobloxUserId=4254123040 (0O000000000000O000O0) 64944s
--   LocalId=35 -> RobloxUserId=627739590 (Lemon3201) 200505s
--   LocalId=36 -> RobloxUserId=9810875747 (Daugiezzz) 212955s

-- Insert daily playtime records
-- These will be inserted with ON CONFLICT to avoid duplicates
INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-15', 2438
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-15', 4081
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-15', 4620
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-15', 6658
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-15', 5280
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-15', 1320
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-15', 2580
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 15362
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 5880
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 360
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 3300
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 4320
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 7080
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 60
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 2220
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 6060
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 5340
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 9060
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 22981
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 1320
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 5460
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-16', 8881
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 24613
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 3060
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 10261
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 2732
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 2309
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 1976
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 659
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 4079
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 9542
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 7443
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-17', 120
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 34563
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 45063
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 14401
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 30841
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 1260
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 1260
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 1440
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 13680
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 9359
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 16259
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 5220
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 240
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 3720
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 3060
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 10440
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 1560
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-18', 480
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 720
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 12990
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 31650
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 1380
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 4200
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 60
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 13710
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 1080
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 2820
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 180
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 8760
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 1080
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 120
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 5220
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 1740
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 5880
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 5550
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 14280
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 600
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-19', 2100
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 67115
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 120
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 12601
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 64364
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 4086
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 13413
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 3329
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 1201
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 810
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 9007
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 3021
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 780
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 7059
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 540
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 4182
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 8127
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 6931
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-20', 3483
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 14592
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 44116
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 6842
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 2040
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 7080
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 300
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 2160
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 420
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 3720
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 1980
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 2700
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 5763
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 240
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 29959
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-21', 8281
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 15720
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 4560
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 6240
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 16920
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 29726
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 4320
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 8220
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 2640
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 2160
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 8400
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 13920
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 8220
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 660
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 600
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 1080
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-22', 3000
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 32043
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 22744
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 15300
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 3180
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 44220
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 2640
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 180
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 7080
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 15843
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 6660
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 180
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 20343
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 1380
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 480
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 360
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-23', 4380
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 11943
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 3780
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 8281
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 23229
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 1681
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 5103
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 6542
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 60
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 8884
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 60
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 663
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 2340
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 900
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 1920
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 6000
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-24', 600
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 49940
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 7980
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 2734
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 3240
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 1200
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 1560
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 12123
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 5596
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 1260
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 60
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 4141
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 60
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 3902
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-25', 420
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 48313
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 3001
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 5041
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 4020
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 180
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 7023
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 3721
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 6963
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 4441
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 660
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 960
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 9842
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 541
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-26', 2520
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 9921
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 3960
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 17884
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 1261
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 6903
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 480
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 5044
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 27371
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 6783
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 901
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 1080
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 2101
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 13202
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 3604
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-27', 6061
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 180
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 360
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 4980
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 6183
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 69277
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 4440
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 2400
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 60
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 1623
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 13625
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 3601
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 2641
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 11403
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-28', 11345
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 13740
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 31155
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 8702
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 8175
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 2160
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 60
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 7745
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 17111
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 480
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 1141
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 60
FROM "Players" p WHERE p."RobloxUserId" = 1538890744
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 841
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-29', 5702
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 180
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 960
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 6379
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 5223
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 22546
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 599
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 1677
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 9939
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 2040
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 900
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 6541
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 16905
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-30', 3801
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 15420
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 1260
FROM "Players" p WHERE p."RobloxUserId" = 1723196157
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 36242
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 4201
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 960
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 10141
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 6481
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 840
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 1743
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 60
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 7621
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 3300
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 10320
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-07-31', 1321
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 28741
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 8299
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 4440
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 17101
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 4200
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 3060
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 14539
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 8760
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 10261
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 5100
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 6480
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 180
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 15961
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-01', 12740
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 10701
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 2721
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 14040
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 1560
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 4680
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 2580
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 4020
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 6660
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 5640
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 23056
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 1020
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 1681
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 7620
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 3960
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-02', 8100
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 40983
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 18062
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 360
FROM "Players" p WHERE p."RobloxUserId" = 10304177841
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 25562
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 5040
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 8041
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 5041
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 4260
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 3480
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 1140
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 120
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 9902
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 9600
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-03', 120
FROM "Players" p WHERE p."RobloxUserId" = 2333869817
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 32194
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 7440
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 3541
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 4740
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 4260
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 1021
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 13502
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 14001
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-04', 6919
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 18211
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 1920
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 5100
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 2521
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 4622
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 15762
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 3963
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 4140
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 420
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 10943
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-05', 2643
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 24067
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 2040
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 20642
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 4920
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 3000
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 6661
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 180
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 480
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 4446
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 840
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 5706
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 5220
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-06', 360
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 11041
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 12541
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 5940
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 2820
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 840
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 16321
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 12202
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 7140
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 24124
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 5280
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 3600
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-07', 2580
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 1740
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 1260
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 9761
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 21161
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 1140
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 2100
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 12060
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 9180
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 16020
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 60
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 1500
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-08', 660
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-09', 2460
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-09', 2100
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-09', 10981
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-09', 240
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-09', 6961
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-09', 2040
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-09', 1680
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-09', 3661
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 5880
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 11280
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 20341
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 2520
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 4380
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 21362
FROM "Players" p WHERE p."RobloxUserId" = 3352126866
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 11880
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 840
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 1500
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-10', 180
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 9240
FROM "Players" p WHERE p."RobloxUserId" = 3352126866
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 4200
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 2941
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 3720
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 15481
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 38868
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 7382
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 20684
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 10320
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 3000
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 5400
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 11683
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 480
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 840
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 6361
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 15945
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-11', 60
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 6960
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 8821
FROM "Players" p WHERE p."RobloxUserId" = 3352126866
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 16140
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 6960
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 5340
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 47763
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 9241
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 16260
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 2641
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 360
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 5820
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 5280
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 4740
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 1140
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 12481
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 360
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-12', 7020
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 4681
FROM "Players" p WHERE p."RobloxUserId" = 3352126866
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 32460
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 15721
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 10862
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 34343
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 3000
FROM "Players" p WHERE p."RobloxUserId" = 63245367
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 2881
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 9783
FROM "Players" p WHERE p."RobloxUserId" = 3850928177
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 4980
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 180
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 60
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 4140
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 480
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 1440
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 8760
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 7800
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 8040
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-13', 2940
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 1500
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 2100
FROM "Players" p WHERE p."RobloxUserId" = 63245367
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 1920
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 7920
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 9660
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 23041
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 7321
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 1980
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 25322
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 1020
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 10981
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 600
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 13741
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 1860
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 480
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 7261
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 2040
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-14', 8220
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 29646
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 14935
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 8400
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 4200
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 4380
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 300
FROM "Players" p WHERE p."RobloxUserId" = 63245367
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 2700
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 960
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 19862
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 4201
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 4440
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 17461
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 120
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 1560
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 12900
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 7502
FROM "Players" p WHERE p."RobloxUserId" = 3352126866
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-15', 6300
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 15840
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 16380
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 6900
FROM "Players" p WHERE p."RobloxUserId" = 6127300164
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 9480
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 2400
FROM "Players" p WHERE p."RobloxUserId" = 3352126866
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 10440
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 5520
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 4020
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 9480
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 27660
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 1440
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 3720
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 960
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 720
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 2340
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 300
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 1320
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-16', 1260
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 10980
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 11162
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 17403
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 9783
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 9442
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 3901
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 841
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 21188
FROM "Players" p WHERE p."RobloxUserId" = 9810875747
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 21738
FROM "Players" p WHERE p."RobloxUserId" = 7808878766
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 21852
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 300
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 4500
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 300
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 5400
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-17', 1741
FROM "Players" p WHERE p."RobloxUserId" = 2513720962
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-18', 27062
FROM "Players" p WHERE p."RobloxUserId" = 9810875747
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-18', 840
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-18', 18422
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-18', 6180
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-18', 2400
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-18', 1980
FROM "Players" p WHERE p."RobloxUserId" = 3352126866
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-18', 2640
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-18', 120
FROM "Players" p WHERE p."RobloxUserId" = 3726921662
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 5880
FROM "Players" p WHERE p."RobloxUserId" = 9810875747
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 300
FROM "Players" p WHERE p."RobloxUserId" = 9280088192
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 1620
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 18782
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 360
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 6300
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 3000
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 2100
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 16082
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 1320
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 1080
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 840
FROM "Players" p WHERE p."RobloxUserId" = 3352126866
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-21', 3060
FROM "Players" p WHERE p."RobloxUserId" = 63245367
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 39844
FROM "Players" p WHERE p."RobloxUserId" = 9810875747
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 7800
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 9180
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 3840
FROM "Players" p WHERE p."RobloxUserId" = 63245367
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 27005
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 8162
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 60
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 1320
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 780
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 7680
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 5403
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 2280
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 3481
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 8761
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-22', 2161
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 32003
FROM "Players" p WHERE p."RobloxUserId" = 9810875747
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 13862
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 15721
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 1320
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 7260
FROM "Players" p WHERE p."RobloxUserId" = 1937688587
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 7560
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 26603
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 2460
FROM "Players" p WHERE p."RobloxUserId" = 3827442122
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 420
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 2400
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 600
FROM "Players" p WHERE p."RobloxUserId" = 2281712407
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 180
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 420
FROM "Players" p WHERE p."RobloxUserId" = 3237411560
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 19022
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 4921
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 11342
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-23', 3480
FROM "Players" p WHERE p."RobloxUserId" = 63245367
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 50833
FROM "Players" p WHERE p."RobloxUserId" = 9810875747
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 13740
FROM "Players" p WHERE p."RobloxUserId" = 5133395976
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 4621
FROM "Players" p WHERE p."RobloxUserId" = 63245367
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 24961
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 2580
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 5040
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 11460
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 11049
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 120
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 13081
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 15129
FROM "Players" p WHERE p."RobloxUserId" = 979135569
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 14280
FROM "Players" p WHERE p."RobloxUserId" = 946057131
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 9429
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 300
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-24', 2100
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 36145
FROM "Players" p WHERE p."RobloxUserId" = 9810875747
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 7440
FROM "Players" p WHERE p."RobloxUserId" = 63245367
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 2762
FROM "Players" p WHERE p."RobloxUserId" = 627739590
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 420
FROM "Players" p WHERE p."RobloxUserId" = 4254123040
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 11229
FROM "Players" p WHERE p."RobloxUserId" = 2857560695
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 420
FROM "Players" p WHERE p."RobloxUserId" = 7794493516
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 60
FROM "Players" p WHERE p."RobloxUserId" = 1913855092
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 2760
FROM "Players" p WHERE p."RobloxUserId" = 4027874914
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 7743
FROM "Players" p WHERE p."RobloxUserId" = 2243793833
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 120
FROM "Players" p WHERE p."RobloxUserId" = 414303227
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 2342
FROM "Players" p WHERE p."RobloxUserId" = 2051107290
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

INSERT INTO "DailyPlaytime" ("PlayerId", "Date", "PlaySeconds")
SELECT p."Id", '2026-08-25', 901
FROM "Players" p WHERE p."RobloxUserId" = 5704757917
ON CONFLICT ("PlayerId", "Date") DO UPDATE SET "PlaySeconds" = GREATEST("DailyPlaytime"."PlaySeconds", EXCLUDED."PlaySeconds");

-- Update TotalPlaySeconds for each player
UPDATE "Players" SET "TotalPlaySeconds" = 698581 WHERE "RobloxUserId" = 979135569;
UPDATE "Players" SET "TotalPlaySeconds" = 52085 WHERE "RobloxUserId" = 3850928177;
UPDATE "Players" SET "TotalPlaySeconds" = 18901 WHERE "RobloxUserId" = 10304177841;
UPDATE "Players" SET "TotalPlaySeconds" = 207461 WHERE "RobloxUserId" = 2857560695;
UPDATE "Players" SET "TotalPlaySeconds" = 351108 WHERE "RobloxUserId" = 1913855092;
UPDATE "Players" SET "TotalPlaySeconds" = 151541 WHERE "RobloxUserId" = 2051107290;
UPDATE "Players" SET "TotalPlaySeconds" = 15420 WHERE "RobloxUserId" = 1538890744;
UPDATE "Players" SET "TotalPlaySeconds" = 26583 WHERE "RobloxUserId" = 414303227;
UPDATE "Players" SET "TotalPlaySeconds" = 139580 WHERE "RobloxUserId" = 7794493516;
UPDATE "Players" SET "TotalPlaySeconds" = 413524 WHERE "RobloxUserId" = 946057131;
UPDATE "Players" SET "TotalPlaySeconds" = 12962 WHERE "RobloxUserId" = 2513720962;
UPDATE "Players" SET "TotalPlaySeconds" = 69845 WHERE "RobloxUserId" = 1723196157;
UPDATE "Players" SET "TotalPlaySeconds" = 32905 WHERE "RobloxUserId" = 3726921662;
UPDATE "Players" SET "TotalPlaySeconds" = 51125 WHERE "RobloxUserId" = 4027874914;
UPDATE "Players" SET "TotalPlaySeconds" = 68170 WHERE "RobloxUserId" = 3827442122;
UPDATE "Players" SET "TotalPlaySeconds" = 182660 WHERE "RobloxUserId" = 9280088192;
UPDATE "Players" SET "TotalPlaySeconds" = 248183 WHERE "RobloxUserId" = 6127300164;
UPDATE "Players" SET "TotalPlaySeconds" = 108312 WHERE "RobloxUserId" = 1937688587;
UPDATE "Players" SET "TotalPlaySeconds" = 161239 WHERE "RobloxUserId" = 7808878766;
UPDATE "Players" SET "TotalPlaySeconds" = 328437 WHERE "RobloxUserId" = 5133395976;
UPDATE "Players" SET "TotalPlaySeconds" = 126288 WHERE "RobloxUserId" = 2243793833;
UPDATE "Players" SET "TotalPlaySeconds" = 51976 WHERE "RobloxUserId" = 2281712407;
UPDATE "Players" SET "TotalPlaySeconds" = 42762 WHERE "RobloxUserId" = 3237411560;
UPDATE "Players" SET "TotalPlaySeconds" = 299197 WHERE "RobloxUserId" = 2333869817;
UPDATE "Players" SET "TotalPlaySeconds" = 47226 WHERE "RobloxUserId" = 5704757917;
UPDATE "Players" SET "TotalPlaySeconds" = 56826 WHERE "RobloxUserId" = 3352126866;
UPDATE "Players" SET "TotalPlaySeconds" = 27841 WHERE "RobloxUserId" = 63245367;
UPDATE "Players" SET "TotalPlaySeconds" = 64944 WHERE "RobloxUserId" = 4254123040;
UPDATE "Players" SET "TotalPlaySeconds" = 200505 WHERE "RobloxUserId" = 627739590;
UPDATE "Players" SET "TotalPlaySeconds" = 212955 WHERE "RobloxUserId" = 9810875747;

-- Done! Verify with:
-- SELECT "Username", "TotalPlaySeconds" FROM "Players" ORDER BY "TotalPlaySeconds" DESC LIMIT 10;
-- SELECT COUNT(*) FROM "DailyPlaytime";
