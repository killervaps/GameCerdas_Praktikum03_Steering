# GameCerdas_Praktikum03_Steering

## Identitas

| Keterangan | Isi |
|---|---|
| Nama Kelompok | Xx_Sigm4_M4le_67_xX |
| Anggota 1 | Muhammad Farrel Fathin Wibowo / 5025231233 |
| Anggota 2 | Danny Rachmadian Yusuf Satryatama / 5025231240 |
| Anggota 3 | Valensio Arvin Putra Setiawan / 5025231273 |

## Praktikum: Autonomous Steering Agent — Movement AI & Steering Behaviors

### Tujuan Praktikum

Pada praktikum ini mahasiswa akan membuat sebuah NPC Autonomous Steering Agent yang mampu bergerak secara mandiri di dalam lingkungan game.

NPC akan memiliki kemampuan:

- Bergerak menuju target.
- Melambat ketika mendekati target.
- Berhenti pada jarak tertentu dari target.
- Menghadap ke arah gerak.
- Bergerak secara acak menggunakan Wander ketika tidak memiliki target.
- Mendeteksi obstacle menggunakan sensor fisika.
- Menghindari obstacle menggunakan Obstacle Avoidance.
- Menggabungkan beberapa steering behavior.
- Melakukan tuning parameter AI melalui Inspector.
- Melakukan debugging menggunakan Gizmos.

### Behavior

**Behavior wajib:** Arrive, Wander, Obstacle Avoidance

**Behavior pengembangan:**

- **NPC Color** — warna NPC berubah sesuai behavior yang sedang aktif.
- **Flee** — NPC kabur saat Player terlalu dekat (toggle on/off).
- **Separation** — antar NPC saling menjaga jarak agar tidak menumpuk.
- **Pursue** — NPC mengejar posisi prediksi Player (toggle on/off).

> Keterangan di atas hanya preview singkat pencapaian praktikum. Penjelasan lengkap ada di bagian berikutnya.

---

## Penjelasan Logika Kode

Penjelasan mengikuti urutan baris pada masing-masing file: Player dahulu, kemudian NPC (`SteeringSensor`, `SteeringAgent`, `SteeringDebug`).

Bagian yang termasuk fitur pengembangan ditandai dengan:

- **[PENGEMBANGAN: NPC Color]**
- **[PENGEMBANGAN: Flee]**
- **[PENGEMBANGAN: Separation]**
- **[PENGEMBANGAN: Pursue]**

### 1. Player — `Assets/Scripts/SimplePlayerController.cs`

**Field**

- `moveSpeed` — kecepatan gerak Player (unit/detik).
- `turnSpeed` — kecepatan Player berputar menghadap arah gerak.
- `controller` — referensi `CharacterController` milik Player.

**`Velocity` (property)** — **[PENGEMBANGAN: Pursue]**

`public Vector3 Velocity => controller.velocity;` membuka kecepatan Player (bawaan `CharacterController`) agar NPC bisa memprediksi posisi Player berikutnya.

**`Awake()`**

Mengambil komponen `CharacterController` dengan `GetComponent` sekali di awal.

**`Update()`**

1. Membaca input `Horizontal` dan `Vertical` dengan `GetAxisRaw`.
2. Menyusun vector arah `(horizontal, 0, vertical)`.
3. Jika panjang vector > 1 (gerak diagonal), dinormalisasi agar Player tidak bergerak lebih cepat saat diagonal.
4. `controller.Move(direction * moveSpeed * Time.deltaTime)` menggerakkan Player.
5. Jika sedang ada input, arah hadap dihitung dengan `Quaternion.LookRotation(direction)` lalu diputar halus dengan `Slerp` memakai `turnSpeed * Time.deltaTime`.

### 2. NPC — `Assets/Scripts/SteeringSensor.cs`

Sensor obstacle berbasis fisika yang dipakai NPC.

**Field (Obstacle Sensor)**

- `sensorDistance` — jarak jangkau sensor ke depan.
- `sensorRadius` — radius bola SphereCast (mewakili lebar badan NPC).
- `sensorHeight` — ketinggian titik asal sensor dari posisi NPC.
- `obstacleMask` — layer yang dianggap obstacle (project ini memakai layer 6).

**Field (Avoidance)**

- `forwardBias` — seberapa besar arah gerak asli ikut ditambahkan pada arah menghindar, supaya NPC tidak berbalik total.

**Variabel internal dan property**

- `obstacleDetected`, `lastHit` — hasil deteksi terakhir.
- `ObstacleDetected` (property) — dibaca `SteeringAgent` untuk mengetahui apakah sedang ada obstacle. **[PENGEMBANGAN: NPC Color]** memakainya untuk menentukan state *Avoiding*.
- `LastHit` (property) — informasi hit terakhir.

**`GetAvoidanceDirection(movementDirection)`**

1. Menyetel `obstacleDetected = false`. Jika arah gerak hampir nol, langsung mengembalikan `Vector3.zero`.
2. Arah gerak dinormalisasi, titik asal = posisi NPC + `Vector3.up * sensorHeight`.
3. `Physics.SphereCast` ditembakkan ke arah gerak sejauh `sensorDistance`, hanya terhadap `obstacleMask`, mengabaikan trigger.
4. Jika kena: `obstacleDetected = true`; arah menghindar = normal permukaan yang terkena (diproyeksikan ke bidang horizontal, Y = 0, dinormalisasi) ditambah `movementDirection * forwardBias`, lalu dinormalisasi.
5. Jika tidak kena: mengembalikan `Vector3.zero`.

**`OnDrawGizmosSelected()`**

Menggambar bola di titik asal, garis sepanjang `sensorDistance` ke depan, dan bola di ujungnya, sebagai visual debugging jangkauan sensor.

### 3. NPC — `Assets/Scripts/SteeringAgent.cs`

**`enum BehaviorState`** — **[PENGEMBANGAN: NPC Color]**

Daftar state: `Arrive`, `Wander`, `Flee`, `Pursue`, `Separating`, `Avoiding`. Dipakai untuk menentukan warna NPC.

**Field (dikelompokkan dengan `[Header]` di Inspector)**

- *Target*: `target` (tujuan Arrive), `useTarget` (toggle Arrive).
- *Movement*: `maxSpeed` (batas kecepatan), `maxAcceleration` (batas perubahan kecepatan per detik), `turnSpeed` (kecepatan menoleh).
- *Arrive*: `slowRadius` (mulai melambat), `stopRadius` (berhenti).
- *Wander*: `wanderSpeed`, `wanderChangeInterval` (jeda ganti arah), `wanderAngleChange` (sudut acak maksimum).
- *Flee* **[PENGEMBANGAN: Flee]**: `fleeEnabled` (toggle), `fleeTarget` (yang dihindari, diisi Player), `fleeRadius` (jarak pemicu).
- *Pursue* **[PENGEMBANGAN: Pursue]**: `pursueEnabled` (toggle), `pursueTarget` (Player bertipe `SimplePlayerController`), `maxPredictionTime` (batas waktu prediksi).
- *Obstacle Avoidance*: `sensor` (referensi `SteeringSensor`), `avoidanceWeight` (kekuatan arah menghindar).
- *Behavior Visualization* **[PENGEMBANGAN: NPC Color]**: `bodyRenderer` dan warna tiap state (`arriveColor`, `wanderColor`, `fleeColor`, `pursueColor`, `avoidingColor`, `separatingColor`).
- *Separation* **[PENGEMBANGAN: Separation]**: `agentMask` (layer NPC), `separationRadius` (jangkauan deteksi tetangga), `separationWeight` (kekuatan dorongan menjauh).

**Variabel internal**

- `BaseColorId`, `ColorId` — ID properti shader (`_BaseColor` untuk URP, `_Color` untuk shader biasa), di-cache agar efisien. **[PENGEMBANGAN: NPC Color]**
- `velocity` — kecepatan NPC saat ini. `wanderDirection`, `wanderTimer` — status Wander.
- `currentBehavior`, `propertyBlock` — state aktif dan `MaterialPropertyBlock` untuk warna. **[PENGEMBANGAN: NPC Color]**
- `isSeparating` — penanda Separation sedang aktif. **[PENGEMBANGAN: Separation]**
- `Velocity`, `CurrentBehavior` (property) — akses baca dari luar; `Velocity` dipakai `SteeringDebug`.

**`Start()`**

Arah Wander awal = arah depan NPC dan timer diisi penuh. **[PENGEMBANGAN: NPC Color]** Jika `bodyRenderer` kosong, dicari otomatis dengan `GetComponentInChildren<Renderer>()`, lalu `MaterialPropertyBlock` dibuat.

**`Update()`**

Urutan kerja setiap frame:

1. **Memilih behavior dasar** (menghasilkan `desiredVelocity`) dengan prioritas: **Flee > Pursue > Arrive > Wander**. Cabang Arrive/Wander adalah bawaan praktikum; cabang Flee dan Pursue adalah tambahan.
2. `ApplySeparation` — blend gaya menjauhi NPC lain. **[PENGEMBANGAN: Separation]**
3. `ApplyObstacleAvoidance` — blend arah menghindari obstacle.
4. Menentukan state untuk warna: jika `isSeparating` maka *Separating*, lalu jika `sensor.ObstacleDetected` maka *Avoiding* (prioritas tertinggi karena diperiksa paling akhir). **[PENGEMBANGAN: NPC Color]**
5. `UpdateBehaviorColor()`. **[PENGEMBANGAN: NPC Color]**
6. `velocity = MoveTowards(velocity, desiredVelocity, maxAcceleration * Time.deltaTime)` — kecepatan berubah bertahap menuju yang diinginkan.
7. `ClampMagnitude(velocity, maxSpeed)` — kecepatan dibatasi.
8. `ApplyMovement()` dan `UpdateRotation()`.

**`CalculateArrive()`**

Memanggil `ArriveToward(target.position)`. Sebelumnya berisi logika Arrive langsung; logika itu dipindah ke `ArriveToward` agar bisa dipakai bersama Pursue.

**`GetPursuePredictedPosition()`** — **[PENGEMBANGAN: Pursue]**

Menghitung titik prediksi Player: `posisiPlayer + Player.Velocity * waktuPrediksi`, dengan `waktuPrediksi = min(jarak / maxSpeed, maxPredictionTime)`. Semakin jauh Player, semakin jauh ke depan prediksinya.

**`CalculatePursue()`** — **[PENGEMBANGAN: Pursue]**

Memakai ulang **logika Arrive** (`ArriveToward`) dengan titik prediksi sebagai tujuan, sehingga NPC ikut melambat dan berhenti sesuai `slowRadius`/`stopRadius`. Beda dengan Arrive biasa: yang dituju adalah titik di depan Player, bukan posisi Player saat ini.

**`ArriveToward(Vector3 targetPosition)`**

1. Vector ke target, sumbu Y diabaikan; jarak dihitung dengan `magnitude`.
2. Jarak ≤ `stopRadius` → kecepatan nol (berhenti).
3. Jarak < `slowRadius` → kecepatan = `maxSpeed` × `Clamp01((jarak − stopRadius) / (slowRadius − stopRadius))`, turun linear.
4. Selain itu kecepatan = `maxSpeed`.
5. Hasil = `toTarget.normalized * desiredSpeed`.

**`IsFleeTriggered()`** — **[PENGEMBANGAN: Flee]**

Bernilai `true` jika `fleeEnabled` aktif, `fleeTarget` ada, dan jarak (kuadrat) NPC ke target ≤ `fleeRadius²`. Memakai pola toggle `useTarget` dan cek jarak seperti Arrive.

**`CalculateFlee()`** — **[PENGEMBANGAN: Flee]**

Adaptasi dari struktur Arrive dengan arah dibalik: `posisiNPC − posisiTarget`, dinormalisasi, dikali `maxSpeed` (tanpa deselerasi). Jika posisi persis sama, dipakai arah depan NPC agar tidak nol.

**`CalculateWander()`**

Timer dikurangi `Time.deltaTime`. Saat habis, sudut acak antara `−wanderAngleChange` dan `+wanderAngleChange` dipakai memutar arah depan NPC (`Quaternion.Euler`), Y dinolkan, dinormalisasi, dan timer diisi ulang. Mengembalikan `wanderDirection * wanderSpeed`.

**`CalculateSeparation()`** — **[PENGEMBANGAN: Separation]**

1. `Physics.OverlapSphere` di posisi NPC dengan radius `separationRadius` pada layer `agentMask` untuk mencari NPC di sekitar.
2. Untuk tiap Collider, dicari `SteeringAgent`-nya (`GetComponentInParent`); dilewati jika kosong atau diri sendiri.
3. Vector menjauh = `posisiNPC − posisiTetangga` (Y = 0), ditambahkan dengan bobot `1 / jarak²` (lebih dekat, lebih kuat).
4. Hasilnya dirata-rata dengan jumlah tetangga.

**`ApplySeparation(desiredVelocity)`** — **[PENGEMBANGAN: Separation]**

Memakai pola *blend* dari **Obstacle Avoidance**: `arah = desiredVelocity + separation * separationWeight`, dinormalisasi, dikali kecepatan (minimal `wanderSpeed`). Mengatur `isSeparating`. Berbeda dari Flee, Separation tidak meng-override behavior lain, melainkan berjalan bersamaan dengannya.

**`ApplyObstacleAvoidance(desiredVelocity)`**

1. Jika `sensor` kosong, kembalikan `desiredVelocity` apa adanya.
2. Arah cek = arah `desiredVelocity` (atau arah depan jika hampir nol).
3. Minta arah menghindar dari `sensor.GetAvoidanceDirection`.
4. Jika ada: `arah = arahCek + arahMenghindar * avoidanceWeight`, dinormalisasi, dikali kecepatan (minimal `wanderSpeed` agar NPC yang sedang berhenti tetap bisa menghindar).
5. Jika tidak ada, `desiredVelocity` tidak diubah.

**`UpdateBehaviorColor()`** — **[PENGEMBANGAN: NPC Color]**

Memilih warna berdasarkan `currentBehavior` (`switch`), lalu menerapkannya lewat `MaterialPropertyBlock` (`GetPropertyBlock` → `SetColor` untuk `_BaseColor` dan `_Color` → `SetPropertyBlock`). Cara ini tidak membuat instance material baru per NPC.

**`ApplyMovement()`**

`transform.position += velocity * Time.deltaTime`.

**`UpdateRotation()`**

Mengabaikan Y dan tidak berputar jika hampir diam. Selain itu NPC diputar halus dengan `Slerp` ke `LookRotation(arahVelocity)` memakai `turnSpeed * Time.deltaTime`.

**`OnDrawGizmosSelected()`**

Menggambar lingkaran `stopRadius` dan `slowRadius`, serta garis ke `target`. Tambahan: lingkaran `fleeRadius` dan garis ke `fleeTarget` **[PENGEMBANGAN: Flee]**; garis dan bola titik prediksi **[PENGEMBANGAN: Pursue]**; lingkaran magenta `separationRadius` **[PENGEMBANGAN: Separation]**.

### 4. NPC — `Assets/Scripts/SteeringDebug.cs`

- `agent`, `velocityScale` — referensi `SteeringAgent` dan pengali panjang garis.
- `Reset()` — otomatis mengisi `agent` dari komponen di objek yang sama saat script ditambahkan.
- `OnDrawGizmosSelected()` — menggambar garis dari posisi NPC (+1 di atas) sepanjang `agent.Velocity * velocityScale` beserta bola kecil di ujungnya, sehingga arah dan besar kecepatan NPC terlihat di Scene view.

---

## Pertanyaan Praktikum

### 1. Apa perbedaan Seek dan Arrive?

**Seek** menuju target dengan kecepatan penuh sampai tiba, tanpa melambat. **Arrive** juga menuju target, tetapi melambat setelah masuk `slowRadius` dan berhenti pada `stopRadius`, sehingga berhenti dengan halus di dekat target.

### 2. Mengapa Seek murni dapat menyebabkan agent melewati target?

Seek tidak pernah mengurangi kecepatan, dan kecepatan agent tidak bisa berubah seketika (dibatasi percepatan). Saat tiba di target, agent masih bergerak cepat sehingga melewatinya, lalu berbalik dan melewatinya lagi, sehingga berputar atau berosilasi di sekitar target (overshoot).

### 3. Apa fungsi `direction.normalized`?

`normalized` mengubah vector menjadi panjang 1 dengan arah tetap. Fungsinya:

- Mengambil arahnya saja, sehingga kecepatan dapat diatur terpisah, misalnya `toTarget.normalized * desiredSpeed`. Tanpa itu, kecepatan bergantung pada jarak ke target.
- Pada Player, mencegah gerak diagonal lebih cepat (panjang diagonal tanpa normalisasi ≈ 1,41).
- Membuat penjumlahan beberapa arah (mis. arah tujuan + arah menghindar) berimbang.

### 4. Mengapa movement dikalikan dengan `Time.deltaTime`?

`Time.deltaTime` adalah waktu antar frame. Mengalikan kecepatan (unit/detik) dengan `deltaTime` mengubahnya menjadi jarak per frame, sehingga pergerakan sama cepatnya di komputer dengan FPS tinggi maupun rendah (frame-rate independent).

### 5. Apa perbedaan `Raycast` dan `SphereCast`?

- **Raycast**: sinar tipis tanpa ketebalan. Bisa lolos lewat celah sempit walaupun badan NPC tidak muat.
- **SphereCast**: sinar bervolume berbentuk bola berradius tertentu (`sensorRadius`), sehingga lebar badan NPC ikut diperhitungkan. Project ini memakai SphereCast pada `SteeringSensor`.

### 6. Mengapa obstacle menggunakan Layer tersendiri?

Agar sensor hanya mendeteksi obstacle (`obstacleMask`), tidak mengenai objek lain seperti lantai, Player, NPC lain, atau diri sendiri, yang akan dianggap penghalang secara keliru. Layer juga membuat query fisika lebih efisien dan mudah diatur: objek baru cukup dimasukkan ke layer obstacle. Hal yang sama berlaku pada `agentMask` untuk Separation.

### 7. Apa fungsi Avoidance Weight?

`avoidanceWeight` menentukan seberapa kuat arah menghindar dibanding arah tujuan asli saat keduanya digabung. Nilai besar membuat NPC membelok tajam menjauhi obstacle; nilai kecil membuat NPC lebih condong ke tujuannya dan berisiko menabrak.

### 8. Mengapa NPC masih dapat terjebak pada obstacle tertentu walaupun memiliki obstacle avoidance?

Obstacle avoidance bersifat reaktif dan lokal: hanya melihat ke depan sejauh sensor dan tidak mengetahui peta. Akibatnya NPC dapat terjebak pada:

- Jalan buntu atau dinding berbentuk huruf U, karena menghindar satu sisi dapat mengarahkan ke sisi lain yang tertutup.
- Dinding yang dihadapi tegak lurus, karena arah menghindar berlawanan dengan arah tujuan sehingga NPC bisa bolak-balik (osilasi).
- Tujuan yang berada di balik obstacle, karena gaya menuju target dan gaya menghindar saling meniadakan.

Sensor yang hanya satu arah depan juga tidak melihat obstacle di samping.

### 9. Apa perbedaan Obstacle Avoidance dan Pathfinding?

- **Obstacle Avoidance**: reaktif, lokal, dihitung setiap frame dari sensor, tanpa tahu peta.
- **Pathfinding** (mis. A\*/NavMesh): merencanakan rute lengkap dari posisi awal ke tujuan berdasarkan peta, sehingga dapat menemukan jalan memutar. Keduanya biasanya dikombinasikan: pathfinding untuk rute global, avoidance untuk menghindari rintangan dinamis di sepanjang rute.

### 10. Apa akibat `Max Acceleration` terlalu kecil?

Kecepatan berubah sangat lambat menuju `desiredVelocity`. NPC terasa berat: lama mencapai kecepatan penuh, lambat berbelok, tidak sempat mengerem sehingga dapat melewati target walaupun memakai Arrive, dan bereaksi terlambat terhadap obstacle sehingga bisa menabrak.

### 11. Apa akibat `Slow Radius` terlalu besar?

NPC mulai melambat dari jarak yang sangat jauh, sehingga merayap pelan dalam waktu lama sebelum tiba dan tidak pernah mencapai `maxSpeed` bila jarak awalnya masih di dalam radius. Jika nilainya ≤ `stopRadius`, zona perlambatan hilang (range dijaga minimal 0,001) sehingga NPC berhenti mendadak seperti Seek.

### 12. Mengapa visual debugging penting dalam pengembangan Game AI?

Perilaku AI terjadi dari perhitungan vector yang tidak terlihat di layar. Visual debugging (Gizmos, warna state) membuat nilai internal terlihat: jangkauan sensor, radius Arrive/Flee/Separation, arah velocity, dan titik prediksi. Dengan begitu penyebab perilaku aneh mudah ditemukan, parameter mudah di-tuning saat game berjalan, dan bug tidak ditebak-tebak lewat log angka. Warna NPC pada project ini juga langsung menunjukkan behavior mana yang sedang aktif.
