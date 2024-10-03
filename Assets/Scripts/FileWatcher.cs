using System.IO;
using UnityEngine;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;

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
        watcher.Path = ConfigLoader.GetExProgPath(ConfigLoader.GetWatchPath());
        watcher.Filter = "*.wav";
        // watcher.Changed += OnCreated;
        watcher.Created += OnCreated;
        watcher.EnableRaisingEvents = true;
        string savePath = ConfigLoader.GetExProgPath(ConfigLoader.GetSavePath());
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
        // createdのあと少し待つ
        yield return new WaitForSeconds(3f);
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

        yield return StartCoroutine(HandleFileAsync(filePath));
    }

    private System.Collections.IEnumerator HandleFileAsync(string filePath)
    {
        // 保存する場合
        if (ConfigLoader.IsSaveImpCv())
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = timestamp + "_" + Path.GetFileName(filePath);
            string destinationFilePath = Path.Combine(ConfigLoader.GetExProgPath(ConfigLoader.GetSavePath()), fileName);

            // ファイルを非同期で移動
            var moveTask = Task.Run(() =>
            {
                try
                {
                    File.Move(filePath, destinationFilePath);
                    Debug.Log("File moved: " + destinationFilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error moving file: " + ex.Message);
                }
            });

            // 非同期タスクが完了するまで待つ
            yield return new WaitUntil(() => moveTask.IsCompleted);
        }
        else
        {
            // ファイルを非同期で削除
            var deleteTask = Task.Run(() =>
            {
                try
                {
                    File.Delete(filePath);
                    Debug.Log("File deleted: " + filePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error deleting file: " + ex.Message);
                }
            });

            // 非同期タスクが完了するまで待つ
            yield return new WaitUntil(() => deleteTask.IsCompleted);
        }
    }

    void OnDestroy()
    {
        if (watcher != null)
        {
            watcher.Dispose();
        }
    }
}
