# Pemeriksaan Kuntilanak Merah (Scary)

Tanggal: 8 Oktober 2026. Folder sumber: `C:/Users/Naufal Ahmad A/Downloads/Kuntilanak-merah/`.

**Hasil: layak sebagai pengganti visual hantu jumpscare setelah material HDRP, orientasi, dan posisi kepala disesuaikan. Belum dipasang ke MainScene. Paket tidak menyediakan animasi gerak yang siap digunakan.**

Pemeriksaan menggunakan pembacaan FBX biner/PNG dan impor native pada project inspeksi terpisah dengan **Unity 6000.0.43f1**. Project utama menggunakan **HDRP 17.0.4**. Folder sumber di Downloads tidak diubah. MainScene, GhostAppearance.cs, dan DoorJumpscare.cs memiliki hash yang sama sebelum dan sesudah pemeriksaan.

## Isi paket dan model

Folder teratas berisi empat tekstur PNG dan `source/kuntilanak_merah.zip`. FBX masih berada di dalam ZIP, pada `kuntilanak_merah/kuntilanak_merah/kuntilanak_merah_and_rig.fbx`.

| Bagian | Temuan |
|---|---|
| Format | FBX biner versi 7400, sekitar 7,89 MB |
| Geometri sumber | Satu mesh `char1`, 14.016 control point, 28.630 segitiga, satu UV set |
| Hasil impor Unity | Satu SkinnedMeshRenderer, 30.614 vertex, 28.630 segitiga, satu submesh/material |
| Rig/skin | 24 tulang pada skin, 24 bind pose |
| Ekspresi wajah | Tidak ada blendshape |
| Ukuran pose mesh setelah skin dibake | Lebar 1,622 m × tinggi 1,700 m × kedalaman 0,410 m |
| Orientasi | Vektor Head ke headfront mengarah +Z pada hasil impor |
| Posisi kepala | Head sekitar Y=1,443 m, relatif terhadap penempatan asal model |

Jumlah vertex impor lebih besar daripada control point FBX karena vertex render dapat dipecah pada batas normal/UV. Ini tidak berarti ada dua model.

Bounds SkinnedMeshRenderer sekitar 2,198 m tinggi lebih besar daripada geometri pose sebenarnya; pengukuran tinggi 1,700 m di atas menggunakan vertex mesh yang sudah dibake. Jangan menggunakan bounds renderer saja untuk menentukan skala karakter.

## Tekstur dan material

Keempat tekstur berukuran **2048 × 2048**:

| File di folder textures | Isi dan kondisi |
|---|---|
| texture_0.png | Atlas warna RGBA: rambut hitam, pakaian merah, kulit pucat, dan mata merah |
| KuntilanakMerah_biped_texture_0_normal.png | RGB normal map; perlu diimpor sebagai Normal Map dan diperiksa arah normalnya saat preview |
| KuntilanakMerah_biped_texture_0_roughness.png | Grayscale roughness; perlu dikonversi ke smoothness untuk HDRP |
| KuntilanakMerah_biped_texture_0_metallic.png | Grayscale metallic; nilainya tidak semuanya nol |

Atlas warna di ZIP bernama `KuntilanakMerah_biped_texture_0.png`. Walaupun hash file PNG berbeda, **pixel hasil decode identik** dengan `textures/texture_0.png`. Normal, metallic, dan roughness dalam kedua lokasi juga identik secara pixel.

Alpha atlas warna berada pada 169–255 dan hampir seluruhnya 255; tidak ada alpha nol. Ini tidak menunjukkan kebutuhan transparansi/cutout penuh. Mulai dari material Opaque dan periksa rambut pada preview sebelum memilih teknik tambahan.

FBX mengandung satu material `Material_1`. Pada importer Unity yang diperiksa, shader-nya **Standard**, dengan `texture_0.png` pada **base color sekaligus emission**. Normal map belum terpasang. Referensi tekstur FBX juga masih menyebut path ekspor lama `/tmp/.../Character_output.fbm/texture_0.png`, sehingga relink tekstur perlu diperiksa ketika diimpor.

Tidak ditemukan material `.mat` siap HDRP, shader khusus, AO map, emission mask khusus, prefab Unity, Animator Controller, atau audio hantu dalam paket. Mata merah pada atlas bukan bukti adanya emission mask mata yang terpisah. Jangan memakai seluruh atlas sebagai emission tanpa meninjau hasilnya karena tubuh dapat ikut menyala.

## Rig dan animasi

- **Generic berhasil:** avatar Generic valid dan skin terimpor.
- **Auto Humanoid gagal:** importer melaporkan `Required human bone 'LeftFoot' not found`. Source FBX memiliki tulang bernama LeftFoot, tetapi auto-mapping tidak mengisi slot Human LeftFoot. Menonaktifkan Optimize Bones dan mempertahankan hierarchy saja belum menyelesaikan auto-mapping.
- **Humanoid manual berhasil pada salinan inspeksi:** pertahankan hierarchy, nonaktifkan Optimize Bones, petakan `LeftFoot → LeftFoot`, dan kosongkan pemetaan Jaw otomatis ke `headfront`. Avatar kemudian menunjukkan `isValid=true` dan `isHuman=true`. Karena perubahan diuji bersama, pemeriksaan ini tidak mengklaim setiap perubahan tersebut secara individual wajib.
- Tidak ada rig jari lengkap atau blendshape wajah yang siap untuk ekspresi terperinci.
- Ada satu track bernama `Armature|Armature|clip0|baselayer`. Namun, 225 kurva sumber masing-masing hanya memiliki satu key pada waktu 0; tidak ada kurva yang berubah. Import Generic menghasilkan klip nominal 1 detik tanpa curve binding. Import Humanoid memperingatkan klip memiliki 0 frame dan menghasilkan animasi kosong.

Dengan demikian, paket **memiliki rig dan pose statis, tetapi tidak memiliki gerakan idle, berjalan, menyerang, atau jumpscare yang siap dimainkan**. Retarget animasi Humanoid eksternal dimungkinkan setelah pemetaan rig diperbaiki, tetapi kualitas deformasi saat retarget belum diuji.

## Kecocokan dengan HDRP dan sistem jumpscare saat ini

Mesh, UV, skin, dan PNG dapat diimpor di Unity 6000.0.43f1. Rendering akhir dalam HDRP/MainScene belum diuji dan material Standard tidak dianggap sebagai material HDRP siap pakai.

Saat pemasangan nanti:

1. Buat material **HDRP/Lit**, relink atlas warna ke Base Color dengan sRGB aktif, lalu pasang normal map sebagai Normal Map.
2. Susun **Mask Map**: R=metallic; G=1 sebagai AO netral karena AO tidak tersedia; B=1 sebagai detail mask netral tanpa detail map; A=smoothness=1−roughness. Import mask sebagai data linear, sRGB nonaktif. Periksa respons metallic pada kulit/kain saat preview.
3. Gunakan Generic untuk kemunculan statis sederhana, atau gunakan pemetaan Humanoid manual yang sudah diuji jika akan menambahkan animasi eksternal. Jangan mengandalkan klip bawaan sebagai gerakan.
4. Tempatkan model di bawah wrapper visual pada `Temporary_Ghost`, lalu isi field Ghost Model. Model impor menghadap +Z, sedangkan placeholder menghadap -Z; gunakan rotasi wrapper Y sekitar 180° dan cek wajah menghadap kamera.
5. Mulai dari skala impor 1 untuk tinggi sekitar 1,70 m. Sesuaikan posisi lokal atau Vertical Offset pada GhostAppearance berdasarkan posisi kepala/eyes di Game View; offset placeholder tidak dapat langsung diasumsikan sesuai.
6. Pertahankan model tersembunyi sebelum pemicu, hindari collider penghalang, dan gunakan Ignore Raycast. Suara tetap dari komponen Audio Source/Scare Clip yang terpisah karena paket tidak menyertakan audio.

Gerakan mendekat yang dibuat GhostAppearance tetap dapat menggerakkan seluruh model meskipun model belum mempunyai animasi tubuh. Pemeriksaan ini tidak mengganti model, suara, pengaturan kamera, pencahayaan, lentera, tangga, atau koneksi pintu pada MainScene.

Dokumentasi: [HDRP 17.0.4 — Mask and detail maps](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/Mask-Map-and-Detail-Map.html).

## Kredit dan lisensi

"Kuntilanak Merah (Scary)" (https://skfb.ly/pIwIx) by fdhlan21 is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

Kredit juga disimpan di `Assets/HorrorPrototype/CREDITS.md`. Identitas model/pembuat/lisensi berasal dari informasi pengguna; tautan pendek Sketchfab tidak dapat dibuka melalui alat web pada pemeriksaan ini. Ketentuan atribusi dapat diperiksa pada [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/): sertakan kredit, tautan lisensi, dan keterangan perubahan saat mendistribusikan adaptasi.

Data pengukuran tersedia di `source-analysis.json` dan `unity-import-analysis.json`; pesan importer terdapat pada log inspeksi. Tidak ada instalasi model di project utama pada tahap ini.
