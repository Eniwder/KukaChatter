using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;

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

    // ジェネリックメソッドによるプロパティ取得
    public static T GetConfigValue<T>(Func<Config, T> selector)
    {
        Config config = LoadConfig();
        return selector(config);
    }

    public static string GetPort() => GetConfigValue(c => c.port);
    public static bool IsSaveImpCv() => GetConfigValue(c => c.isSaveImpCv);
    public static string GetDevExProg() => GetConfigValue(c => c.devExProg);
    public static string GetImpInterval() => GetConfigValue(c => c.impInterval);
    public static string GetExProg() => GetConfigValue(c => c.exProg);
    public static string GetBgmOp() => GetConfigValue(c => c.bgm.op);
    public static string GetWatchPath() => GetConfigValue(c => c.watchPath);
    public static string GetSavePath() => GetConfigValue(c => c.savePath);
    public static Dictionary<string, int> GetBgmDay() => GetConfigValue(c => c.bgm.day);
    public static Dictionary<string, int> GetBgmNight() => GetConfigValue(c => c.bgm.night);

    public static string GetExProgPath(string trgPath)
    {
        string path = string.Empty;
        if (Application.isEditor)
        {
            // エディタ内での実行時（開発時）
            path = Path.Combine(Application.dataPath, GetDevExProg(), trgPath);
        }
        else
        {
            // ビルド後の実行ファイルからの実行時
            path = Path.Combine(Application.dataPath, GetExProg(), trgPath);
        }
        return path;
    }
}
