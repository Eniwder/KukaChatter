using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

[System.Serializable]
public class Config
{
    public string port;
    public string devExProg;
    public string exProg;
    public string watchPath;
    public string savePath;
    public string impInterval;
    public bool isSaveImpCv;
    public Bgms bgm;

    public class Bgms
    {
        [JsonProperty("op")]
        public string op;

        [JsonProperty("day")]
        public Dictionary<string, int> day { get; set; }

        [JsonProperty("night")]
        public Dictionary<string, int> night { get; set; }
    }
}

public static class ConfigLoader
{
    private static Config _config;

    public static Config LoadConfig()
    {
        if (_config == null)
        {
            // StreamingAssetsフォルダ内のconfig.jsonのパスを取得
            string path = Path.Combine(Application.streamingAssetsPath, "config.json");

            // JSONファイルを読み込む
            string json = File.ReadAllText(path);

            // JSONをパースしてConfigオブジェクトに変換
            _config = JsonConvert.DeserializeObject<Config>(json);
        }

        return _config;
    }

    public static string GetPort()
    {
        Config config = LoadConfig();
        return config.port;
    }
    public static bool IsSaveImpCv()
    {
        Config config = LoadConfig();
        return config.isSaveImpCv;
    }
    public static string GetDevExProg()
    {
        Config config = LoadConfig();
        return config.devExProg;
    }
    public static string GetImpInterval()
    {
        Config config = LoadConfig();
        return config.impInterval;
    }
    public static string GetExProg()
    {
        Config config = LoadConfig();
        return config.exProg;
    }
    public static string GetBgmOp()
    {
        Config config = LoadConfig();
        return config.bgm.op;
    }
    public static string GetWatchPath()
    {
        Config config = LoadConfig();
        return config.watchPath;
    }
    public static string GetSavePath()
    {
        Config config = LoadConfig();
        return config.savePath;
    }
    public static Dictionary<string, int> GetBgmDay()
    {
        Config config = LoadConfig();
        return config.bgm.day;
    }

    public static Dictionary<string, int> GetBgmNight()
    {
        Config config = LoadConfig();
        return config.bgm.night;
    }

    public static string getExProgPath(string trgPath){
        string path = string.Empty;
        if (Application.isEditor)
        {
            // エディタ内での実行時（開発時）
            path = Path.Combine(Application.dataPath, ConfigLoader.GetDevExProg(), trgPath);
        }
        else
        {
            // ビルド後の実行ファイルからの実行時
            path = Path.Combine(Application.dataPath, ConfigLoader.GetExProg(), trgPath);
        }
        // Debug.Log(path);
        return path;
    }

}