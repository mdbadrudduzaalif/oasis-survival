# Oasis Zombie Survival — Complete Asset & Material Inventory

## 1. Weapon & Combat Systems
| Asset Pack Name | Role in Game | Models & Prefabs | Materials Used |
| :--- | :--- | :--- | :--- |
| Ultimate Weapon System | Primary Player Firearm & Ejected Brass | M4CHA.fbx (M4A1 Rifle with moving bolt, trigger, and mag), Bullet_Shell.fbx, Bullet_Shell_Physics.prefab, BallProjectile.prefab | M4A1_mat.mat, Bullet_Shell_Gold.mat, Sem_Muzzle.mat, prototype_black_dff.mat, lambert1.mat |
| FreeWeaponSounds (Sonniss / David Dumais) | Gunshot & Tactical Reload Audio | 40 Audio Clips (assault_rifle_gunshot_01..03.wav, drop_the_mag.wav, insert_the_mag.wav, bolt.wav, sil_gunshot.wav) | N/A (Audio Pack) |
| Gece Studio | Alternate Firearm Reference | Rifle_HK416.fbx, Rifle_HK416.prefab | M_Rifle-HK416.mat, M_Rifle-HK416_Fantastic.mat |

## 2. Zombie Characters & Enemy AI
| Asset Pack Name | Role in Game | Models & Prefabs | Materials Used |
| :--- | :--- | :--- | :--- |
| ZombieMale_AAB (Mixamo / AAA Zombie Pack) | Horde Enemy Base (Suit_Civilian, Shirtless_Crawler, BOSS_Goliath_Berserker) | ZombieMale_AAB.fbx, ZombieMale_AAB_BodyParts.fbx, 53 FBX Animations (attack, crawl, bite, scream, run, walk, death, reaction_hit), Prefabs: Suit_Civilian, Shirtless_Crawler, Skinless_Berserker | ZombieMale_Body_MAT_URP.mat, ZombieMale_Head_V1_MAT 1.mat, ZombieMale_LowerBody_MAT_URP.mat, ZombieMale_Shirt_MAT_URP.mat, Zombie_Eyes_MAT_URP.mat, ZombieMale_Teeth_MAT_V1.mat |
| NewPunch (Shirtless Zombie FREE Pack) | Horde Enemy Variant (Shirtless_Ghoul) | ShirtlessZombie_FREE.fbx, ShirtlessZombie_BodyParts_FREE.fbx, Shirtless_Ghoul.prefab | ZombieBB_Body_URP.mat, ZombieBB_Clothes_URP.mat, ZombieBB_Body_V3.mat, ZombieBB_Clothes_V3.mat |
| Tensori (Skinless Horror Pack) | Berserker Variant & Weapon Props | skinless zombie.fbx, fireaxe.fbx, angry01.fbx, idle01.fbx, idle02.fbx | Skin.mat, Pants.mat, Axe.mat |
| ZombieHorrorPackageFree | Zombie Screams, Groans, Bites & Blood Audio | 44 Audio Clips (Zombie_Attack_Bite_001..002.wav, Blood_Splash_A_001..003.wav, Foley_BodyFall_001..003.wav, Zombie_Eat.wav, Zombie_Scream.wav) | N/A (Audio Pack) |

## 3. Environment & Level Design (The Desert Oasis Map)
| Category | Meshes & Models | Materials Used (49 Materials) |
| :--- | :--- | :--- |
| Terrain, Dunes & Cliffs | Terrain_01_Mesh.fbx, Cliff_01_Mesh to Cliff_03_Mesh, Mountain_01_Mesh, Mountain_02_Mesh, BigRock_01_Mesh, Rock_01_Mesh to Rock_06_Mesh | OasisTerrain_Mat.mat, OasisTerrainExtra_Mat.mat, Cliff_01_Mat.mat, Cliff_02_Mat.mat, Mountain_01_Mat.mat, Mountain_02_Mat.mat, BigRock_01_Mat.mat, Rock_01_Mat.mat, Gravel_01_Mat.mat |
| Skybox & Oasis Water | Sky Dome & Water Plane | OasisSunsetSkybox_Mat.mat, OasisWater_Mat.mat |
| Desert Foliage & Palms | Palm_Mesh.fbx (Palm_2 to Palm_6), Grass_Mesh.fbx (Grass_2 to Grass_Long_4), Fountain_Mesh.fbx, ST_Wind_Cliff.fbx | PalmBark_Mat.mat, PalmLeafAtlas_Mat.mat, PalmAtlas_Mat.mat, DarkBark_Mat.mat, GrassAtlas_Mat.mat, LongGrassAtlas_Mat.mat, LargeGrassAtlas_Mat.mat, ShortGrassAtlas_Mat.mat, FountainAtlas_Mat.mat, FountainStem_Mat.mat, SagoLeaf_Mat.mat |
| Bedouin Campsite & Props | Tent_01_Mesh to Tent_10_Mesh, Carpets_Big_01_Mesh, Carpets_Ground_01_Mesh, Cushions_01_Mesh, Firepit_01_Mesh, Shisha_01_Mesh, Teaset_Teapot_01_Mesh, Teleporter_Platform_01_Mesh | Tent_01_Mat.mat, Tent_Stand_01_Mat.mat, Tent_Strings_01_Mat.mat, Tent_Tiling_01_Mat.mat, Carpets_01_Mat.mat, Carpets_02_Mat.mat, Carpets_Big_01_Mat.mat, Carpets_Big_02_Mat.mat, Cushions_01_Mat.mat, Cushions_02_Mat.mat, Cushions_03_Mat.mat, Firepit_01_Mat.mat, Shisha_01_Mat.mat, Teaset_01_Mat.mat, Teleporter_Platform_01_Mat.mat |
| Surface Decals | Terrain Detail Projections | SandDecal_Mat.mat, GravelDecal_Mat.mat, LeakDecal_Mat.mat, BlackGrungeDecal_M.mat, WhiteGrungeDecal_Mat.mat |

## 4. Survival Pickups & Drops
| Asset Pack Name | Role in Game | Models & Prefabs | Materials Used |
| :--- | :--- | :--- | :--- |
| AmmoBox (Military Ammo Box) | 3D Physical Ammo Resupply Drop | AmmoBox.FBX, AmmoBox_Pickup.prefab | AmmoBox.mat |
| First aid jar | 3D Physical Health Restorative Drop | Firstaid.fbx, HealthJar_Pickup.prefab | Firstaid.mat, Firstaid_2.mat, HealthJar_Glass.mat, HealthJar_Core.mat, HealthJar_Cap.mat |

## 5. Weather, Particles & UI Framework
| Component / Pack | Role in Game | Assets & Textures | Materials / Shaders |
| :--- | :--- | :--- | :--- |
| Dynamic Sandstorm Weather | 3D Atmospheric Dust & Flying Sand FX | TX_WispySmoke03b_8x8.png, TX_TinyStones_D.png, GraininessDustWave_T_N.png | DustCloudMat (URP Particle Alpha Blend), SandGrainsMat (URP Stretched Particle) |
| SharedAssets & TextMeshPro | Player Rig, Camera & HUD Font Assets | PlayerCapsule.prefab, MainCamera.prefab, RuntimeDataCanvas_Prefab.prefab | LiberationSans SDF - Drop Shadow.mat, LiberationSans SDF - Outline.mat, BackgroundCirkle.mat |
| Gabriel Pereira | Pause System Integration | Pause Manager.prefab | Default URP UI Materials |
