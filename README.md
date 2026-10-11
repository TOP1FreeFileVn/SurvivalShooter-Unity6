# Survival Shooter (Unity 6 + URP)

Game bắn súng sinh tồn góc nhìn top-down, làm từ asset bài tập `Game3D_Survivor Shooter Asset`
của repo [cditc2025/LapTrinhGameCoBan](https://github.com/cditc2025/LapTrinhGameCoBan).

## Chạy game
1. Mở project bằng **Unity 6000.0.78f1**.
2. Mở scene `Assets/SurvivalShooter/Scenes/SurvivalShooter.unity` và bấm **Play**.

## Điều khiển
- **WASD / phím mũi tên**: di chuyển
- **Chuột**: ngắm
- **Giữ chuột trái**: bắn
- Hỗ trợ tay cầm (cần trái + cò phải)

## Gameplay (kiểu Brotato / Vampire Survivors)
- **20 wave có giới hạn thời gian.** Quái spawn liên tục, máu và sát thương tăng theo wave.
  Zombunny từ wave 1, ZomBear từ wave 3, Hellephant từ wave 6.
- Hết giờ thì quái còn lại biến mất, EXP trên sàn tự bay về người chơi.
- **Kinh nghiệm:** quái chết rơi viên EXP màu xanh, đến gần là hút vào.
- **Lên cấp:** hết wave thì chọn 1 trong 3 thẻ nâng cấp (click hoặc phím 1/2/3).
  Thẻ chia 3 độ hiếm Thường / Hiếm / Sử thi, cấp càng cao càng dễ ra thẻ hiếm:
  - Thường: sát thương, tốc độ bắn, máu tối đa, tốc độ chạy, tầm hút EXP, giáp, hồi máu, chí mạng
  - Hiếm: thêm EXP, đạn xuyên, hút máu, sát thương chí mạng, hào quang lửa, Liều Mạng
  - Sử thi: thêm tia đạn, Cuồng Nộ, Bất Tử
- Sống sót qua 20 wave là thắng. Game lưu điểm và wave cao nhất. Bấm nút hoặc phím **R** để chơi lại.

## Cấu trúc
- `Assets/SurvivalShooter/Scripts/` – script gameplay (Player, Enemy, Managers, Upgrades, Pickups, UI)
- `Assets/SurvivalShooter/Editor/SurvivalShooterBuilder.cs` – dựng lại toàn bộ scene qua menu
  **Tools > Survival Shooter > Build Game**
- `Assets/SurvivalShooter/Generated/` – prefab quái, viên EXP, Animator Controller, NavMesh

Asset gốc thuộc Unity Technologies (Survival Shooter tutorial), xem `SurvivalShooter_Third-PartyNotice.txt`.
