using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.IO;

public class BGMController : MonoBehaviour
{
    public AudioSource audioSource; // BGMを再生するためのAudioSource
    private AudioClip[] bgmDay; // 04:00～17:59のBGM
    private AudioClip[] bgmNight; // 18:00～03:59のBGM
    private System.Random random = new System.Random();
    private Dictionary<string, AudioClip> audioClips;
    private Dictionary<string, int> bgmListDay;
    private Dictionary<string, int> bgmListNight;
    private string bgmOp;


    private async void Start()
    {
        audioSource.volume = PlayerPrefs.GetFloat("bgmVolumeSlider", 0f);
        // StreamingAssetフォルダから動的にBGMを読み込む
        audioClips = new Dictionary<string, AudioClip>();
        bgmOp = ConfigLoader.GetBgmOp();
        // 最初のBGMだけ同期的に読み込む
        await AwaitCoroutine(LoadAudioClipAsync(bgmOp));
        PlayBGM(bgmOp);
        bgmListDay = ConfigLoader.GetBgmDay();
        bgmListNight = ConfigLoader.GetBgmNight();
        StartCoroutine(LoadAudioClipsAsync());
    }

    public Task AwaitCoroutine(IEnumerator coroutine)
    {
        var tcs = new TaskCompletionSource<bool>();

        StartCoroutine(RunCoroutine(coroutine, tcs));

        return tcs.Task;
    }
    private IEnumerator RunCoroutine(IEnumerator coroutine, TaskCompletionSource<bool> tcs)
    {
        yield return StartCoroutine(coroutine);
        tcs.SetResult(true);
    }
    private AudioType GetAudioType(string uri)
    {
        switch (Path.GetExtension(uri).ToLower())
        {
            case ".ogg":
                return AudioType.OGGVORBIS;
            case ".wav":
                return AudioType.WAV;
            case ".mp3":
                return AudioType.MPEG;
            default:
                return AudioType.UNKNOWN;
        }
    }
    private IEnumerator LoadAudioClipAsync(string clipName)
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "BGM", clipName);
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, GetAudioType(clipName)))
        {
            ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = true;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                {
                    audioClips.Add(clipName, clip);
                }
            }
            else
            {
                Debug.LogError($"Failed to load audio clip at {path}: {request.error}");
            }
        }
    }

    private IEnumerator LoadAudioClipsAsync()
    {
        string[] clipNames = GetAllClips(); // クリップ名の配列を取得
        var tasks = clipNames.Select(clipName => AwaitCoroutine(LoadAudioClipAsync(clipName))).ToArray();
        yield return Task.WhenAll(tasks);
    }

    private string[] GetAllClips()
    {
        // 両方の辞書のキーを取得
        var dayKeys = bgmListDay.Keys;
        var nightKeys = bgmListNight.Keys;

        // 重複しないキーをリストとして取得
        var uniqueKeys = dayKeys.Except(nightKeys)
                               .Concat(nightKeys)
                               .ToList();

        if (uniqueKeys.Contains(bgmOp))
        {
            uniqueKeys.Remove(bgmOp);
        }
        return uniqueKeys.ToArray();
    }

    private void PlayBGM(string name = null)
    {

        // 現在の時間を取得
        DateTime now = DateTime.Now;
        // 日中(04:00～17:59)と夜(18:00～03:59)でランダムに曲を取り出す
        string selectedBgmName = (name != null) ?
            name : (now.Hour >= 4 && now.Hour < 18) ?
            SelectRandomBGM(bgmListDay) : SelectRandomBGM(bgmListNight);

        AudioClip selectedBgm = audioClips[selectedBgmName];

        // BGMを再生
        if (selectedBgm != null)
        {
            audioSource.clip = selectedBgm;
            audioSource.Play();
            StartCoroutine(CheckBGMEnd());
        }
    }

    private IEnumerator CheckBGMEnd()
    {
        // 現在のBGMが終了するまで待つ
        yield return new WaitForSeconds(audioSource.clip.length);

        // 終了後、次のBGMを再生
        PlayBGM();
    }

    private string SelectRandomBGM(Dictionary<string, int> bgmRates)
    {
        int totalProbability = 0;
        foreach (var rate in bgmRates.Values)
        {
            totalProbability += rate;
        }

        int randomValue = random.Next(0, totalProbability);
        int cumulativeProbability = 0;

        foreach (var entry in bgmRates)
        {
            cumulativeProbability += entry.Value;
            if (randomValue <= cumulativeProbability)
            {
                return entry.Key;
            }
        }

        return bgmRates.First().Key;
    }
}
