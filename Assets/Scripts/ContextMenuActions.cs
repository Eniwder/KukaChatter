using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Net.Http;
using System.Threading.Tasks;
using System;


public class ContextMenuActions : MonoBehaviour
{
    public Toggle impReaderToggle;
    // public Toggle smartFrameToggle;
    public Slider bgmVolumeSlider;
    public Slider cvVolumeSlider;
    public Toggle characterReverseToggle;
    public Toggle characterPositionToggle;
    public Slider characterTallSlider;
    public Button stopButton;
    public Button exitButton;
    public Transform characterTransform;
    private Vector3 defaultScale;

    public AudioSource bgmAudioSource;
    public AudioSource cvAudioSource;
    private bool inited = false;

    void Start()
    {
        defaultScale = characterTransform.localScale;

        impReaderToggle.isOn = PlayerPrefs.GetInt("impReaderToggle", 1) == 1;
#if NO_READER
        impReaderToggle.gameObject.SetActive(false);
#else
        impReaderToggle.gameObject.SetActive(true);
#endif
        // smartFrameToggle.isOn = PlayerPrefs.GetInt("smartFrameToggle", 1) == 1;
        bgmVolumeSlider.value = PlayerPrefs.GetFloat("bgmVolumeSlider", 0.1f);
        cvVolumeSlider.value = PlayerPrefs.GetFloat("cvVolumeSlider", 0.1f);
        characterReverseToggle.isOn = PlayerPrefs.GetInt("characterReverseToggle", 0) == 1;
        characterPositionToggle.isOn = PlayerPrefs.GetInt("characterPositionToggle", 0) == 1;
        characterTallSlider.value = PlayerPrefs.GetInt("characterTallSlider", 50);

        PlayerPrefs.SetInt("minimize", 0);

        impReaderToggle.onValueChanged.AddListener(OnImpReaderToggle);
        // smartFrameToggle.onValueChanged.AddListener(OnSmartFrameToggle);
        bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeSlider);
        cvVolumeSlider.onValueChanged.AddListener(OnCvVolumeSlider);
        characterReverseToggle.onValueChanged.AddListener(OnCharacterReverseToggle);
        characterPositionToggle.onValueChanged.AddListener(OnCharacterPositionToggle);
        characterTallSlider.onValueChanged.AddListener(OnCharacterTallSlider);

        OnCharacterReverseToggle(characterReverseToggle.isOn);
        OnCharacterTallSlider(characterTallSlider.value);
        OnBgmVolumeSlider(bgmVolumeSlider.value);
        OnCvVolumeSlider(cvVolumeSlider.value);

        stopButton.onClick.AddListener(OnStopButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);

        float dx = PlayerPrefs.GetFloat("characterPosX", characterTransform.position.x);
        float dy = PlayerPrefs.GetFloat("characterPosY", characterTransform.position.y);
        characterTransform.position = new Vector3(dx, dy, characterTransform.position.z);
        inited = true;
    }


    public async void OnStopButtonClicked()
    {
        // ゲームを一時停止
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0.333f; // FixedUpdateの頻度を下げる
        Application.targetFrameRate = 10; // フレームレートを制限して負荷を下げる

        // BGMの再生を停止
        bgmAudioSource.Pause();
        cvAudioSource.Stop();

        await NetworkHelper.PostJsonAsync("toggle-stop", "{ \"stop\": true }");

        // ウィンドウを最小化（タスクバーに格納）
        MinimizeWindow();
    }

#if UNITY_STANDALONE_WIN
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();
        
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        private const int SW_MINIMIZE = 6;
        const int GWL_EXSTYLE = -20;
        const uint WS_EX_LEFT = 0x00000000;
        const uint WS_EX_LAYERD = 0x080000;
        const uint WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_TOOLWINDOW = 0x00000080;
#endif
    private void MinimizeWindow()
    {
#if UNITY_STANDALONE_WIN
            PlayerPrefs.SetInt("minimize", 1);
            var currentWindow = GetActiveWindow();
            SetWindowLong(currentWindow, GWL_EXSTYLE, WS_EX_LEFT);
            ShowWindow(currentWindow, SW_MINIMIZE);
#endif
    }
    async void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            // フォーカスが戻ったときにゲームを再開
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f; // 通常の値に戻す
            Application.targetFrameRate = 60;
            // BGMの再生を再開
            if (!bgmAudioSource.isPlaying)
            {
                gameObject.SetActive(!inited);
                bgmAudioSource.Play();
            }
            // NetworkHelper.PostJsonAsync("focus", "{}");
            PlayerPrefs.SetInt("minimize", 0);
            await NetworkHelper.PostJsonAsync("toggle-stop", "{ \"stop\": false }");
        }
    }

    // TODO asyncにするかどうか
    public async void OnExitButtonClicked()
    {
        await NetworkHelper.PostJsonAsync("close", "{}");

        PlayerPrefs.SetFloat("characterPosX", characterTransform.position.x);
        PlayerPrefs.SetFloat("characterPosY", characterTransform.position.y);

        PlayerPrefs.Save();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private async void OnImpReaderToggle(bool isOn)
    {
        PlayerPrefs.SetInt("impReaderToggle", isOn ? 1 : 0);
        string jsonContent = "{\"impread\": " + (isOn ? "true" : "false") + "}";
        await NetworkHelper.PostJsonAsync("toggle-impread", jsonContent);
    }

    private async void OnSmartFrameToggle(bool isOn)
    {
        PlayerPrefs.SetInt("smpReaderToggle", isOn ? 1 : 0);
        string jsonContent = "{\"frame\": " + (isOn ? "true" : "false") + "}";
        await NetworkHelper.PostJsonAsync("toggle-frame", jsonContent);
    }

    private void OnCharacterReverseToggle(bool isOn)
    {
        PlayerPrefs.SetInt("characterReverseToggle", isOn ? 1 : 0);
        Vector3 currentScale = characterTransform.localScale;
        characterTransform.localScale = new Vector3(isOn ? -Mathf.Abs(currentScale.x) : Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
    }

    private void OnCharacterPositionToggle(bool isOn)
    {
        PlayerPrefs.SetInt("characterPositionToggle", isOn ? 1 : 0);
    }

    private void OnBgmVolumeSlider(float value)
    {
        PlayerPrefs.SetFloat("bgmVolumeSlider", value);
        bgmAudioSource.volume = value;
        // ストリーミング再生の負荷を減らすために音量がゼロの場合は再生を停止する(影響はほぼゼロだけど…)
        if (value == 0f)
        {
            bgmAudioSource.Pause();
        }
        else
        {
            if (!bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Play();
            }
        }
    }
    private void OnCvVolumeSlider(float value)
    {
        PlayerPrefs.SetFloat("cvVolumeSlider", value);
        cvAudioSource.volume = value;
    }
    private void OnCharacterTallSlider(float value)
    {
        int addScale = (int)value;
        PlayerPrefs.SetInt("characterTallSlider", addScale);
        characterTransform.localScale =
             new Vector3((Mathf.Abs(defaultScale.x) + addScale) * (characterReverseToggle.isOn ? -1 : 1), defaultScale.y + addScale, defaultScale.z);
    }



}
