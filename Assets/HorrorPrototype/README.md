# Tahap pertama: pintu, kunci, dan jumpscare

Scene: `Assets/FpsHorrorKit/Scenes/MainScene.unity`.

- Pintu target: `Single_Door_1/Door_Leaf`. Mulai dengan `isLocked = true` dan `hasKey = false`.
- Kunci: `StageOne_LockedDoor/Temporary_Key_Yellow`, posisi dunia **(-7, 1.25, -1.7)**. Kunci kuning melayang dekat titik awal pemain, sekitar 1,7 meter ke arah depan dari posisi awal. Dekati, arahkan tengah layar, lalu tekan **E** dalam jarak 1,5 meter.
- Hantu: `StageOne_LockedDoor/Temporary_Ghost/KuntilanakMerah_Model`. Menggunakan prefab `KuntilanakMerah/Prefabs/KuntilanakMerah_Visual.prefab` dengan material HDRP/Lit, normal map, dan mask map. Model tersembunyi sampai pintu selesai terbuka. GhostAppearance mendekatkannya dari 2,8 ke 1,3 meter dalam 0,2 detik, dengan suara sementara, lalu menyembunyikannya setelah 1,2 detik. Wajah menghadap kamera; Vertical Offset disetel berdasarkan penanda FaceAnchor.
- Pemicu berlaku satu kali per sesi Play Mode. Menutup atau membuka kembali pintu tidak mengulang jumpscare. Mulai ulang Play Mode untuk meresetnya.

## Pengujian di Unity

1. Tunggu Unity menyelesaikan import dan kompilasi. Jika scene meminta reload perubahan dari disk, pilih **Reload**.
2. Buka MainScene. Opsional: jalankan **Tools > Horror Prototype > Validate Stage One** sebelum Play Mode. Console akan menampilkan hasil validasi referensi.
3. Masuk Play Mode. Sebelum mengambil kunci, dekati `Single_Door_1`, arahkan pandangan ke daun pintu, lalu tekan E. Pintu harus tetap tertutup dengan pesan “Pintu terkunci - cari kunci”.
4. Kembali ke kunci kuning dekat spawn, dekati dan tekan E. Objek kunci harus menghilang.
5. Kembali ke pintu. Pesan menjadi “Gunakan kunci [E]”. Tekan E: pintu membuka, kemudian hantu dan suara muncul sesudah rotasi membuka selesai.
6. Tunggu hantu menghilang. Tutup dan buka ulang pintu dengan E. Jumpscare tidak boleh terulang.
7. Hentikan dan mulai ulang Play Mode. Kunci dan pintu kembali ke keadaan awal.
8. Untuk memeriksa validasi kunci, kosongkan sementara Compatible Door pada kunci dalam Play Mode lalu tekan E. Console menampilkan peringatan, tanpa NullReferenceException; kunci tetap tersedia. Hentikan Play Mode untuk mengembalikan referensi yang tersimpan.

## Mengganti model dan suara

Pada `Temporary_Ghost`, komponen `GhostAppearance` memiliki field **Ghost Model**, **Audio Source**, dan **Scare Clip**. Model Kuntilanak Merah berada pada child `KuntilanakMerah_Model`; model dan suara tetap dapat diganti terpisah. Skala model 1 dengan tinggi sekitar 1,7 meter, putaran lokal FBX Y = 180 derajat, dan Vertical Offset sekitar -1,563 meter. FaceAnchor merupakan penanda tinggi wajah untuk penyetelan, bukan komponen runtime. Model harus tersembunyi sebelum Play Mode dan tidak memiliki collider yang menghalangi pemain. Gunakan layer **Ignore Raycast** untuk modelnya.

Ganti Scare Clip dengan AudioClip pilihan. Sound Volume, Duration, Approach Time, Start Distance, End Distance, dan Vertical Offset bisa diatur di Inspector. Tidak perlu mengubah `DoorSystem` atau pengaturan kamera untuk mengganti penampilan jumpscare.

`DoorJumpscare` menangani event pintu dan batas satu kali. `GhostAppearance` menangani penampilan dan suara. Tahap ini berupa kemunculan hantu sementara; belum mencakup patroli atau pengejaran.

## Perubahan

- `DoorSystem.cs`: event `onDoorOpened`, dipanggil setelah rotasi membuka mencapai target; tidak dipanggil saat menutup.
- `ITOKey.cs`: validasi Compatible Door, pencegahan pengambilan ganda, dan penghilangan objek setelah pickup.
- `MainScene.unity`: override kunci pada satu instance `Single_Door_1` dan satu grup objek baru `StageOne_LockedDoor`.
- Folder ini: dua komponen runtime, validator Editor, empat material HDRP/Unlit, dan satu suara sementara.

Pengaturan scene untuk tangga, pencahayaan, lentera, kamera, dan pintu lain dipertahankan. Referensi dan logika runtime diperiksa pada project Unity terpisah; penampilan akhir dalam pencahayaan MainScene tetap perlu dilihat langsung di Game View.

## Pemasangan Kuntilanak Merah

- Cadangan sebelum penggantian: `Backups/MainScene_before_KuntilanakMerah_2026-10-08.unity`, di luar Assets agar tidak menjadi scene tambahan saat import.
- Aset baru berada di `Assets/HorrorPrototype/KuntilanakMerah`; kredit pembuat dan catatan penyesuaian berada di `CREDITS.md`.
- Hanya isi model hantu dan Vertical Offset pada MainScene yang diganti. Komponen pintu, kunci, DoorJumpscare, dan GhostAppearance tetap menggunakan logika sebelumnya.
- Tidak tersedia animasi berjalan/menyerang pada sumber: klip bawaan hanya satu pose. Rig tetap tersedia untuk animasi lanjutan; pemasangan ini menggunakan gerak mendekat yang sudah ada.
- Uji Play Mode pada salinan project Unity 6000.0.43f1 dengan HDRP 17.0.4 lulus: validasi referensi, collider pickup, pintu terkunci, pengambilan kunci, event setelah pintu selesai terbuka, gerakan 2,8 → 1,3 meter, posisi/arah wajah, model menghilang, dan batas sekali. Render HDRP juga diperiksa. Pengujian otomatis memanggil Interact setelah raycast, menggunakan prefab pintu asli dan objek tahap pertama; input keyboard E serta seluruh lingkungan MainScene dicoba melalui langkah manual di atas.
