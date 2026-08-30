using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AudioProcessor
{

    [MenuItem("Tools/Process and Assign Audio")]
    public static void ProcessAndAssignAllAudio()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        string rawDir = "Assets/Audio/Raw";
        string playerDir = "Assets/Audio/Player";
        string bossDir = "Assets/Audio/Boss";

        if (!Directory.Exists(playerDir)) Directory.CreateDirectory(playerDir);
        if (!Directory.Exists(bossDir)) Directory.CreateDirectory(bossDir);

        string stompSrc = $"{rawDir}/Heavy_sand_thud_deep_#3-1787769968403.mp3";
        AudioClip stompClip = AssetDatabase.LoadAssetAtPath<AudioClip>(stompSrc);
        if (stompClip != null)
        {
            TrimAndSaveClip(stompClip, $"{bossDir}/Boss_Footstep_Stomp.wav", 0.015f);
        }

        string pRunSrc1 = $"{rawDir}/sand player running.mp3";
        AudioClip pRunClip1 = AssetDatabase.LoadAssetAtPath<AudioClip>(pRunSrc1);
        if (pRunClip1 != null)
        {
            TrimAndSaveClip(pRunClip1, $"{playerDir}/Player_Sand_Walk_Loop.wav", 0.01f);
            SliceIntoSteps(pRunClip1, playerDir, "Player_Sand_Walk_Step", 4);
        }

        string pRunSrc2 = $"{rawDir}/sand player runnng.mp3";
        AudioClip pRunClip2 = AssetDatabase.LoadAssetAtPath<AudioClip>(pRunSrc2);
        if (pRunClip2 != null)
        {
            TrimAndSaveClip(pRunClip2, $"{playerDir}/Player_Sand_Sprint_Loop.wav", 0.01f);
            SliceIntoSteps(pRunClip2, playerDir, "Player_Sand_Sprint_Step", 4);
        }

        CopyOrTrimClip($"{rawDir}/universfield-punch-140236.mp3", $"{bossDir}/Boss_Punch_Impact.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/dragon-studio-animalistic-grunt-463204.mp3", $"{bossDir}/Boss_Punch_Grunt.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/dragon-studio-violent-sword-slice-2-393841.mp3", $"{bossDir}/Boss_Claw_Slash.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/capaholiczsfx-creature-snarl-very-close-403154.mp3", $"{bossDir}/Boss_Claw_Snarl.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/dragon-studio-beast-growl-494304.mp3", $"{bossDir}/Boss_Beast_Growl.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/studiokolomna-fast-whoosh-118248.mp3", $"{bossDir}/Boss_Fast_Whoosh.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/Explosive_ground_pus_#4-1787769558987.mp3", $"{bossDir}/Boss_Leap_Launch.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/Heavy_rushing_wind_a_#4-1787769602349.mp3", $"{bossDir}/Boss_Leap_AirWhoosh.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/dragon-studio-gust-of-wind-511325.mp3", $"{bossDir}/Boss_Wind_Gust.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/Seismic_earth-shatte_#3-1787769635697.mp3", $"{bossDir}/Boss_Leap_CraterSlam.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/Deep,_bass-heavy,_re_#3-1787769917870.mp3", $"{bossDir}/Boss_Standing_Roar.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/yodguard-aggressive-monster-roar-3-533006.mp3", $"{bossDir}/Boss_Aggressive_Roar.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/Massive_heavy_sand_b_#1-1787770212041.mp3", $"{bossDir}/Boss_Death_BodySlam.wav", 0.01f);
        CopyOrTrimClip($"{rawDir}/Dying_breath_whisper_#1-1787770251222.mp3", $"{bossDir}/Boss_Death_Whisper.wav", 0.01f);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        AssignAudioToPlayerAndBoss();
        Debug.Log("<color=#55FF55><b>[AudioProcessor] All Player and Boss audio processed and assigned successfully!</b></color>");
    }

    private static void CopyOrTrimClip(string srcPath, string dstPath, float threshold)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(srcPath);
        if (clip != null)
        {
            TrimAndSaveClip(clip, dstPath, threshold);
        }
    }

    private static void TrimAndSaveClip(AudioClip clip, string dstPath, float silenceThreshold)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int startIndex = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > silenceThreshold)
            {
                startIndex = Mathf.Max(0, i - (clip.channels * 100));
                break;
            }
        }

        int endIndex = samples.Length - 1;
        for (int i = samples.Length - 1; i >= 0; i--)
        {
            if (Mathf.Abs(samples[i]) > silenceThreshold)
            {
                endIndex = Mathf.Min(samples.Length - 1, i + (clip.channels * 100));
                break;
            }
        }

        int trimmedCount = Mathf.Max(1, endIndex - startIndex + 1);
        float[] trimmedSamples = new float[trimmedCount];
        System.Array.Copy(samples, startIndex, trimmedSamples, 0, trimmedCount);

        SaveWavFile(dstPath, trimmedSamples, clip.channels, clip.frequency);
    }

    private static void SliceIntoSteps(AudioClip clip, string dstDir, string prefix, int sliceCount)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int totalFrames = clip.samples;
        int framesPerSlice = totalFrames / sliceCount;

        for (int s = 0; s < sliceCount; s++)
        {
            int startFrame = s * framesPerSlice;
            int frameLen = framesPerSlice;
            int startSample = startFrame * clip.channels;
            int sampleLen = frameLen * clip.channels;

            float[] sliceSamples = new float[sampleLen];
            System.Array.Copy(samples, startSample, sliceSamples, 0, sampleLen);

            int realStart = 0;
            for (int i = 0; i < sliceSamples.Length; i++)
            {
                if (Mathf.Abs(sliceSamples[i]) > 0.01f)
                {
                    realStart = Mathf.Max(0, i - (clip.channels * 50));
                    break;
                }
            }

            int validCount = sliceSamples.Length - realStart;
            float[] finalSamples = new float[validCount];
            System.Array.Copy(sliceSamples, realStart, finalSamples, 0, validCount);

            string path = $"{dstDir}/{prefix}_{s + 1}.wav";
            SaveWavFile(path, finalSamples, clip.channels, clip.frequency);
        }
    }

    private static void SaveWavFile(string path, float[] samples, int channels, int sampleRate)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {

            bw.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            bw.Write(36 + samples.Length * 2);
            bw.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            bw.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2);
            bw.Write((short)(channels * 2));
            bw.Write((short)16);

            bw.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            bw.Write(samples.Length * 2);

            short[] intData = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                bw.Write(intData[i]);
            }
        }
    }

    public static void AssignAudioToPlayerAndBoss()
    {

        var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("PlayerCapsule");
        PlayerController player = playerObj != null ? playerObj.GetComponent<PlayerController>() : Object.FindAnyObjectByType<PlayerController>();

        List<AudioClip> playerSteps = new List<AudioClip>();
        for (int i = 1; i <= 4; i++)
        {
            var c1 = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Player/Player_Sand_Walk_Step_{i}.wav");
            if (c1 != null) playerSteps.Add(c1);
            var c2 = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Player/Player_Sand_Sprint_Step_{i}.wav");
            if (c2 != null) playerSteps.Add(c2);
        }

        if (player != null && playerSteps.Count > 0)
        {
            player.footstepSounds = playerSteps.ToArray();
            EditorUtility.SetDirty(player);
        }

        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BossMutant/Prefabs/Mutant_Boss.prefab");
        if (bossPrefab != null)
        {
            ZombieAI ai = bossPrefab.GetComponent<ZombieAI>();
            if (ai != null)
            {
                var punchImpact = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Punch_Impact.wav");
                var clawSlash = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Claw_Slash.wav");
                var leapLaunch = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Leap_Launch.wav");
                var leapAir = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Leap_AirWhoosh.wav");
                var leapCrater = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Leap_CraterSlam.wav");
                var standingRoar = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Standing_Roar.wav");
                var aggroRoar = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Aggressive_Roar.wav");
                var stomp = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Footstep_Stomp.wav");
                var deathSlam = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Death_BodySlam.wav");
                var deathWhisper = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Death_Whisper.wav");
                var grunt = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Boss_Punch_Grunt.wav");

                ai.attackRoars = new AudioClip[] { grunt };
                ai.idleGroans = new AudioClip[] { stomp };
                ai.deathSounds = new AudioClip[] { deathSlam, deathWhisper };
                ai.hurtSounds = new AudioClip[] { punchImpact };
                ai.bossStandingRoar = standingRoar;

                EditorUtility.SetDirty(ai);
                PrefabUtility.SavePrefabAsset(bossPrefab);
            }
        }
    }
}

