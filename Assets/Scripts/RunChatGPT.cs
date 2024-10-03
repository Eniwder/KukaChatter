using System.IO;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;

public class RunChatGPT : MonoBehaviour
{
    private async void Start()
    {
        if (Application.isEditor)
        {
            return;
        }
        string impRead = (PlayerPrefs.GetInt("impReaderToggle", 1) == 1 ? "true" : "false");
#if NO_READER
        impRead = "false";
#endif
        string programPath = ConfigLoader.getExProgPath("Live2D-ChatGPT.exe");
        string arguments = "--port=" + ConfigLoader.GetPort() +
                           " --impRead=" + impRead +
                           " --impInterval=" + ConfigLoader.GetImpInterval();

        string workingDirectory = Path.GetDirectoryName(programPath);
        // UnityEngine.Debug.Log(arguments);
        try
        {
            await Task.Run(() =>
            {
                // プロセスの開始情報を設定
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = programPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                    CreateNoWindow = true // 新しいウィンドウを作成しない
                };
                Process.Start(startInfo);
                UnityEngine.Debug.Log("Program started: " + programPath);
            });
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to start program: " + ex.Message);
        }
    }
}
