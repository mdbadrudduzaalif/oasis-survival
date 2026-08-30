# Oasis Survival: Desert Defense - University Project Deliverable

## 📌 Project Overview
**Oasis Survival** is a 3D First-Person Zombie Wave-Defense Shooter built in **Unity (Universal Render Pipeline)** with a complete 3-Tier architecture and relational database backend (**Microsoft SQL Server** + **ASP.NET Core Web API**).

---

## 🚀 Quick Start (1-Click Run)

### 1. Database Setup:
* Double-click **`Setup_Database.bat`** to automatically execute `OasisShooterDB.sql` on your local SQL Server instance.

### 2. Launch Backend API Server:
* Double-click **`Start_Server.bat`** to start the ASP.NET Core REST API service listening on `http://localhost:5000`.

### 3. Play the Game:
* Open this folder in **Unity Hub** (Unity 6 / 2022+ URP).
* Open scene: `Assets/Scenes/Oasis/Oasis Survival.unity` and press **Play**!

---

## 🎮 Key Gameplay Features
* **Dynamic Wave Survival:** 10 escalating waves featuring Walker, Fast Runner, and Heavy Berserker zombies, concluding with an epic **Goliath Boss Encounter**.
* **Sandstorm & Tornado Weather System:** Environmental hazards (raging sandstorms and roaming tornadoes) activate on Waves 4, 7, and 10.
* **150-Point Cyberpunk Shield System:** Pick up blue shield drops to automatically absorb up to 150 points of damage before health is reduced.
* **Berserk Power-Up Mode (`Q` Key):** Store up to 2 red Berserk jars. Press `Q` to trigger 12 seconds of $1.5\times$ speed, $2.0\times$ weapon damage, and a glowing crimson weapon aura.
* **First Aid Inventory (`E` Key):** Store up to 3 green potion jars to restore $+50\text{ HP}$ on demand.
* **Cyberpunk Tactical HUD:** Real-time bottom-left health, shield, 3D jar studios, dynamic hitmarkers, and boss health bar.

---

## 🕹️ Controls Guide
| Key / Input | Action |
| :--- | :--- |
| **`W`, `A`, `S`, `D`** | Player Movement |
| **`Left Shift`** | Sprint |
| **`Space`** | Jump |
| **`Mouse0` (Left Click)** | Fire HK416 Rifle |
| **`Mouse1` (Right Click)** | Aim Down Sights (ADS) |
| **`R`** | Reload Magazine |
| **`Q`** | Activate Berserk Surge (Consumes 1 Red Jar) |
| **`E`** | Drink Health Potion (Consumes 1 Green Jar) |

---

## 🗄️ 3-Tier Database & Backend Architecture
* **Client Tier:** Unity URP Client (`Assets/Scripts/Network/`)
* **Application Tier:** ASP.NET Core REST API Server (`Server/`)
* **Data Tier:** Microsoft SQL Server (`Assets/Database/OasisShooterDB.sql`)

---

## 📁 Code Structure & Modular Architecture
The codebase is structured into single-responsibility, modular components:
* **`Scripts/Player/`**: `PlayerMovement.cs`, `PlayerHealth.cs`, `PlayerInventory.cs`, `PlayerLook.cs`.
* **`Scripts/Combat/`**: `WeaponShooter.cs`, `WeaponRecoil.cs`, `WeaponReload.cs`, `BerserkEffect.cs`.
* **`Scripts/AI/`**: `ZombieStats.cs`, `ZombieNavigation.cs`, `ZombieCombat.cs`, `ZombieLoot.cs`.
* **`Scripts/Survival/`**: `WaveController.cs`, `SpawnManager.cs`, `SandstormManager.cs`, `SupplyDropManager.cs`, `TornadoController.cs`.
* **`Scripts/Network/`**: `DatabaseConfig.cs`, `AuthService.cs`, `LeaderboardService.cs`.
