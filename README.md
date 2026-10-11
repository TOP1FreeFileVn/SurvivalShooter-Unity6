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

## Gameplay
- Quái spawn liên tục từ 3 hướng, tốc độ spawn tăng dần:
  Zombunny (100 HP, 10 điểm), ZomBear (120 HP, 15 điểm), Hellephant (300 HP, 50 điểm).
- Hết máu -> Game Over, lưu điểm cao nhất, tự chơi lại sau 5 giây.

## Cấu trúc
- `Assets/SurvivalShooter/Scripts/` – script gameplay (Player, Enemy, Managers, CameraFollow)
- `Assets/SurvivalShooter/Editor/SurvivalShooterBuilder.cs` – dựng lại toàn bộ scene qua menu
  **Tools > Survival Shooter > Build Game**
- `Assets/SurvivalShooter/Generated/` – prefab quái, Animator Controller, NavMesh

Asset gốc thuộc Unity Technologies (Survival Shooter tutorial), xem `SurvivalShooter_Third-PartyNotice.txt`.
