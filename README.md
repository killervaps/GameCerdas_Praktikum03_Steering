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

**Field.** Kecepatan gerak (`moveSpeed`), kecepatan menoleh (`turnSpeed`), dan referensi `CharacterController` milik Player.

**`Velocity`** — **[PENGEMBANGAN: Pursue]**
Property baca-saja yang mengembalikan kecepatan Player saat ini (dari `CharacterController`). NPC yang sedang Pursue memakainya untuk menebak ke mana Player akan bergerak.

**`Awake()`**
Saat game mulai, komponen `CharacterController` diambil sekali dan disimpan untuk dipakai di `Update()`.

**`Update()`**
1. Membaca tombol arah (horizontal dan vertical) dan menyusunnya menjadi satu arah gerak.
2. Jika arah gerak lebih panjang dari 1 (misalnya menekan dua tombol sekaligus untuk gerak diagonal), arahnya dinormalisasi supaya gerak diagonal tidak lebih cepat.
3. Player digerakkan sejauh `arah × moveSpeed × Time.deltaTime`.
4. Jika ada input, Player diputar perlahan menghadap arah geraknya. Jika tidak ada input, rotasi tidak diubah.

### 2. NPC — `Assets/Scripts/SteeringSensor.cs`

**Field.** Jarak dan lebar sensor (`sensorDistance`, `sensorRadius`), ketinggian titik tembak (`sensorHeight`), layer yang dianggap obstacle (`obstacleMask`), dan `forwardBias` untuk menentukan seberapa banyak arah gerak asli dipertahankan saat menghindar.

**`obstacleDetected` dan `ObstacleDetected`** — **[PENGEMBANGAN: NPC Color]**
Penanda apakah sensor sedang mengenai obstacle. `SteeringAgent` membacanya untuk mengetahui kapan NPC harus berstatus *Avoiding* (merah). `lastHit` dan `LastHit` menyimpan data benturan terakhir.

**`GetAvoidanceDirection(movementDirection)`**
1. Penanda `obstacleDetected` direset menjadi `false`. Jika arah gerak yang diberikan hampir nol, fungsi langsung mengembalikan "tidak ada arah menghindar".
2. Sensor menembakkan `SphereCast` dari posisi NPC (dinaikkan sebesar `sensorHeight`) lurus ke arah gerak sejauh `sensorDistance`, dan hanya memperhatikan layer obstacle.
3. Jika mengenai sesuatu, `obstacleDetected` menjadi `true`. Arah menghindar dibuat dari normal permukaan yang terkena (arah "menjauhi dinding"), lalu ditambah sedikit arah gerak asli (`forwardBias`) supaya NPC tidak berbalik total, dan dinormalisasi.
4. Jika tidak mengenai apa pun, fungsi mengembalikan "tidak ada arah menghindar".

**`OnDrawGizmosSelected()`**
Menggambar bola di titik tembak, garis lurus ke depan sepanjang `sensorDistance`, dan bola di ujung garis, sehingga jangkauan sensor terlihat di Scene view.

### 3. NPC — `Assets/Scripts/SteeringAgent.cs`

**`BehaviorState`** — **[PENGEMBANGAN: NPC Color]**
Daftar state NPC: `Arrive`, `Wander`, `Flee`, `Pursue`, `Separating`, `Avoiding`. Dipakai untuk menentukan warna.

**Field.** Semuanya dapat diatur dari Inspector dan dikelompokkan per behavior:
- Target dan Movement: tujuan Arrive, toggle `useTarget`, lalu batas kecepatan (`maxSpeed`), batas percepatan (`maxAcceleration`), dan kecepatan menoleh (`turnSpeed`).
- Arrive: `slowRadius` (mulai melambat) dan `stopRadius` (berhenti).
- Wander: kecepatan, jeda ganti arah, dan sudut acak maksimum.
- Flee **[PENGEMBANGAN: Flee]**: toggle `fleeEnabled`, target yang dihindari (Player), dan `fleeRadius`.
- Pursue **[PENGEMBANGAN: Pursue]**: toggle `pursueEnabled`, Player yang dikejar, dan `maxPredictionTime`.
- Obstacle Avoidance: referensi `SteeringSensor` dan `avoidanceWeight`.
- Visualisasi **[PENGEMBANGAN: NPC Color]**: `Renderer` model dan satu warna untuk tiap state.
- Separation **[PENGEMBANGAN: Separation]**: `agentMask` (layer NPC), `separationRadius`, dan `separationWeight`.

**Variabel internal.** `velocity` (kecepatan NPC saat ini), `wanderDirection` dan `wanderTimer` (status Wander), `currentBehavior` dan `propertyBlock` untuk warna **[PENGEMBANGAN: NPC Color]**, serta `isSeparating` **[PENGEMBANGAN: Separation]**. Property `Velocity` dibaca oleh `SteeringDebug`, dan `CurrentBehavior` untuk membaca state.

**`Start()`**
Arah Wander awal disamakan dengan arah depan NPC dan timer Wander diisi penuh. **[PENGEMBANGAN: NPC Color]** Jika `Renderer` belum diisi, dicari otomatis di child NPC, lalu `MaterialPropertyBlock` dibuat.

**`Update()`**
Dijalankan setiap frame dengan urutan berikut:
1. **Memilih behavior dasar.** Diperiksa berurutan: kalau Flee terpicu maka NPC kabur; kalau tidak dan Pursue aktif maka NPC mengejar; kalau tidak dan Arrive aktif (`useTarget` dan ada target) maka NPC menuju target; kalau tidak ada satu pun maka NPC Wander. Pilihan ini menghasilkan `desiredVelocity` (kecepatan yang diinginkan) dan mengisi `currentBehavior`.
2. **Separation.** `desiredVelocity` disesuaikan agar NPC menjauh dari NPC lain yang terlalu dekat. **[PENGEMBANGAN: Separation]**
3. **Obstacle Avoidance.** `desiredVelocity` disesuaikan lagi agar NPC membelok menjauhi obstacle di depannya. Karena dilakukan paling akhir, avoidance yang menentukan arah akhir.
4. **Menentukan state untuk warna.** Jika sedang Separation maka state menjadi *Separating*. Jika sensor mendeteksi obstacle maka state menjadi *Avoiding* (menimpa Separating karena dicek belakangan). **[PENGEMBANGAN: NPC Color]**
5. **Mewarnai NPC** sesuai state. **[PENGEMBANGAN: NPC Color]**
6. **Mengubah kecepatan.** `velocity` digeser bertahap menuju `desiredVelocity`, dibatasi `maxAcceleration × Time.deltaTime` per frame, lalu panjangnya dibatasi `maxSpeed`.
7. NPC **digerakkan** lalu **diputar** menghadap arah geraknya.

**`CalculateArrive()`**
Meneruskan posisi `target` ke `ArriveToward()`, tempat perhitungan Arrive sebenarnya berada.

**`GetPursuePredictedPosition()`** — **[PENGEMBANGAN: Pursue]**
1. Menghitung jarak NPC ke Player.
2. Waktu prediksi = jarak dibagi `maxSpeed`, tetapi tidak boleh melebihi `maxPredictionTime`. Artinya, semakin jauh Player, semakin jauh ke depan tebakannya.
3. Titik prediksi = posisi Player sekarang + (kecepatan Player × waktu prediksi).

**`CalculatePursue()`** — **[PENGEMBANGAN: Pursue]**
Mengambil titik prediksi tadi lalu memakai **logika Arrive** (`ArriveToward`) untuk menuju titik itu. Karena itu NPC tetap melambat dan berhenti sesuai `slowRadius` dan `stopRadius`. Bedanya dengan Arrive biasa: yang dituju adalah titik di depan Player, bukan posisi Player sekarang.

**`ArriveToward(targetPosition)`**
1. Menghitung arah dan jarak ke titik tujuan (ketinggian diabaikan).
2. Jika jarak sudah dalam `stopRadius`, kecepatan yang diinginkan nol, sehingga NPC berhenti.
3. Jika jarak masih dalam `slowRadius`, kecepatan diperkecil sebanding dengan sisa jarak: makin dekat ke `stopRadius`, makin pelan.
4. Jika masih jauh, NPC berjalan dengan `maxSpeed`.
5. Hasilnya adalah arah ke tujuan dikalikan kecepatan tersebut.

**`IsFleeTriggered()`** — **[PENGEMBANGAN: Flee]**
Mengembalikan `false` bila Flee dimatikan atau targetnya kosong. Jika aktif, fungsi mengecek apakah jarak NPC ke Player sudah sama atau lebih kecil dari `fleeRadius`. Pola toggle dan pengecekan jarak ini meniru Arrive.

**`CalculateFlee()`** — **[PENGEMBANGAN: Flee]**
Memakai ide yang sama dengan Arrive, tetapi arahnya dibalik: dari Player menuju NPC. NPC selalu lari dengan `maxSpeed` tanpa melambat. Jika posisi NPC persis sama dengan Player (tidak ada arah), NPC memakai arah depannya.

**`CalculateWander()`**
1. Timer Wander dikurangi waktu frame.
2. Ketika timer habis, dipilih sudut acak (dalam batas `wanderAngleChange`), arah depan NPC diputar sebesar sudut itu menjadi arah Wander baru, dan timer diisi ulang.
3. Selama timer belum habis, arah lama dipertahankan. Hasilnya arah Wander dikalikan `wanderSpeed`.

**`CalculateSeparation()`** — **[PENGEMBANGAN: Separation]**
1. Mencari semua Collider di sekitar NPC dalam `separationRadius` pada layer NPC.
2. Untuk setiap Collider, dicari `SteeringAgent`-nya. Yang bukan NPC atau diri sendiri dilewati.
3. Dihitung arah "menjauh" dari NPC tetangga tersebut. Semakin dekat tetangganya, semakin kuat dorongannya. Jika posisinya persis sama (tidak ada arah), tetangga itu dilewati.
4. Semua dorongan dirata-rata dan dikembalikan sebagai arah menjauh.

**`ApplySeparation(desiredVelocity)`** — **[PENGEMBANGAN: Separation]**
1. Meminta arah menjauh dari `CalculateSeparation()`. Jika hampir nol, `isSeparating` menjadi `false` dan `desiredVelocity` dikembalikan tanpa perubahan.
2. Jika ada, `isSeparating` menjadi `true`. Arah menjauh dikalikan `separationWeight` lalu **dijumlahkan** ke `desiredVelocity`, sehingga NPC tetap menuju tujuannya tetapi membelok menjauhi tetangga. Caranya meniru Obstacle Avoidance, dan berbeda dengan Flee yang menggantikan behavior lain.
3. Hasil penjumlahan dinormalisasi lalu diberi kecepatan (minimal `wanderSpeed` supaya NPC yang sedang berhenti tetap bisa bergeser).

**`ApplyObstacleAvoidance(desiredVelocity)`**
1. Jika NPC tidak punya sensor, `desiredVelocity` dikembalikan tanpa perubahan.
2. Arah yang dicek sensor adalah arah `desiredVelocity`. Jika NPC sedang diam, dipakai arah depannya.
3. Sensor diminta memberi arah menghindar. Jika tidak ada obstacle, `desiredVelocity` dikembalikan tanpa perubahan.
4. Jika ada, arah menghindar dikalikan `avoidanceWeight` lalu **dijumlahkan** ke arah tujuan, dinormalisasi, dan diberi kecepatan (minimal `wanderSpeed`). Makin besar `avoidanceWeight`, makin kuat NPC membelok.

**`UpdateBehaviorColor()`** — **[PENGEMBANGAN: NPC Color]**
1. Jika tidak ada `Renderer`, fungsi berhenti.
2. Warna dipilih sesuai `currentBehavior` (secara bawaan hijau untuk Arrive).
3. Warna diterapkan lewat `MaterialPropertyBlock` ke `_BaseColor` dan `_Color`, sehingga material asli tidak diduplikasi per NPC.

**`ApplyMovement()`**
Posisi NPC ditambah `velocity × Time.deltaTime`, sehingga jarak yang ditempuh sesuai waktu, bukan jumlah frame.

**`UpdateRotation()`**
1. Kecepatan diambil tanpa komponen ketinggian. Jika hampir nol (NPC diam), rotasi tidak diubah.
2. Jika bergerak, NPC diputar perlahan (dengan `turnSpeed`) sampai menghadap arah kecepatannya.

**`OnDrawGizmosSelected()`**
Saat NPC dipilih di Scene view, digambar lingkaran `stopRadius` dan `slowRadius` serta garis ke target. Tambahan dari pengembangan: lingkaran dan garis Flee kuning **[PENGEMBANGAN: Flee]**, garis dan bola titik prediksi Pursue **[PENGEMBANGAN: Pursue]**, dan lingkaran magenta `separationRadius` **[PENGEMBANGAN: Separation]**.

### 4. NPC — `Assets/Scripts/SteeringDebug.cs`

**Field.** Referensi ke `SteeringAgent` dan pengali panjang garis (`velocityScale`).

**`Reset()`**
Dipanggil Unity saat komponen ini pertama kali ditambahkan. `SteeringAgent` di objek yang sama diisi otomatis ke field `agent`.

**`OnDrawGizmosSelected()`**
Jika `agent` ada, digambar garis dari posisi NPC (dinaikkan 1 unit) sepanjang kecepatan NPC × `velocityScale`, dengan bola kecil di ujungnya. Dari garis ini terlihat ke mana NPC bergerak dan seberapa cepat.

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
