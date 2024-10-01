using System.IO;
using UnityEngine;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine.Networking;
using System;

public class FileWatcher : MonoBehaviour
{
    private FileSystemWatcher watcher;
    public AudioSource audioSource;

    void Start()
    {
        if (Application.isEditor)
        {
            return;
        }
#if !NO_READER
        // FileSystemWatcherの設定
        watcher = new FileSystemWatcher();
        watcher.Path = ConfigLoader.getExProgPath(ConfigLoader.GetWatchPath());
        watcher.Filter = "*.wav";
        watcher.Changed += OnCreated;
        watcher.Created += OnCreated;
        watcher.EnableRaisingEvents = true;
        string savePath = ConfigLoader.getExProgPath(ConfigLoader.GetSavePath());
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
#endif
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        // 新しいWAVファイルが作成されたときの処理
        Debug.Log("New file detected: " + e.FullPath);

        // メインスレッドで再生と削除を行う
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            StartCoroutine(PlayAndDelete(e.FullPath));
        });
    }

    private System.Collections.IEnumerator PlayAndDelete(string filePath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + filePath, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                clip.name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                yield return StartCoroutine(CvMotionManager.Instance.InsertPlayClipWithMotion(clip));
            }
        }

        // ファイルを削除
        if (File.Exists(filePath))
        {
            // 保存する場合は
            if (ConfigLoader.IsSaveImpCv())
            {
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string fileName = timestamp + "_" + Path.GetFileName(filePath);
                string destinationFilePath = Path.Combine(ConfigLoader.getExProgPath(ConfigLoader.GetSavePath()), fileName);
                File.Move(filePath, destinationFilePath);
                Debug.Log("File moved: " + destinationFilePath);
            }
            else
            {
                File.Delete(filePath);
                Debug.Log("File deleted: " + filePath);
            }
        }
    }

    void OnDestroy()
    {
        // リソースを解放
        watcher.Dispose();
    }
}
