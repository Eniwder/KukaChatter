using System.Collections;
using System.Collections.Generic;
using Live2D.Cubism.Framework.Motion;
using UnityEngine;

public class CvMotionManager : MonoBehaviour
{
    public static CvMotionManager Instance { get; private set; }
    public AudioSource audioSource;
    private static System.Random random = new System.Random();

    public CubismMotionController _motionController;
    public Dictionary<string, AudioClip> cvClips;
    private Dictionary<string, AnimationClip> animationClips;
    private bool canCv = true;
    // CVとモーションの紐づけ
    private  Dictionary<string, string> cvWithMotion = new Dictionary<string, string>()
    {
        {"Juewa_Touch_Breast_01", "CH_Juewa@Live2D_TCH1_Bra"},
        {"Juewa_Touch_Breast_02", "CH_Juewa@Live2D_TCH1_Bra"},
        {"Juewa_Touch_Breast_03", "CH_Juewa@Live2D_TCH2_Bra"},
        {"Juewa_Touch_Hand_01", "CH_Juewa@Live2D_TCH1_Hand"},
        {"Juewa_Touch_Hand_05", "CH_Juewa@Live2D_TCH2_Hand"},
        {"Juewa_Touch_Head_01", "CH_Juewa@Live2D_TCH1_Head"},
        {"Juewa_Touch_Hip_01", "CH_Juewa@Live2D_TCH1_Hip"},
        {"Juewa_Touch_Hip_02", "CH_Juewa@Live2D_TCH2_Hip_1"},

        {"Juewa_Touch_Unhappy", "CH_Juewa@Live2D_Angry"},
        {"Juewa_Unhappy_Greet", "CH_Juewa@Live2D_Angry"},
        {"Juewa_After_Marriage_Touch", "CH_Juewa@Live2D_Shy_Complete"},

        {"Juewa_Chat_01", "CH_Juewa@Live2D_Greet1_1"},
        {"Juewa_Chat_03", "CH_Juewa@Live2D_Greet1_1"},
        {"Juewa_Chat_04", "CH_Juewa@Live2D_Greet1_1"},
        {"Juewa_Chat_05", "CH_Juewa@Live2D_Greet3_1"},
        {"Juewa_Evening_Night_Greet_01", "CH_Juewa@Live2D_Greet1"},
        {"Juewa_Sunny_Morning_Greet_03", "CH_Juewa@Live2D_Greet3_2"},

        {"Juewa_Skill_01", "CH_Juewa@Live2D_Joyful_Complete"},
        {"Juewa_Skill_02", "CH_Juewa@Live2D_Joyful_Complete"},
        {"Juewa_Skill_02_01", "CH_Juewa@Live2D_Serious"},
        {"Juewa_Skill_02_02", "CH_Juewa@Live2D_Serious"},
        {"Juewa_Skill_03", "CH_Juewa@Live2D_Joyful_Complete"},
        {"Juewa_Attack", "CH_Juewa@Live2D_Serious"},
        {"Juewa_Strike", "CH_Juewa@Live2D_Serious"},
        {"Juewa_Striked", "CH_Juewa@Live2D_Serious"},
        {"Juewa_Win", "CH_Juewa@Live2D_Joyful_Complete"},

        {"喜", "CH_Juewa@Live2D_Joyful_loop"},
        {"哀", "CH_Juewa@Live2D_Serious_loop"},
        {"驚", "CH_Juewa@Live2D_Terrified_loop"},
    };  

    // 汎用的にAudioClipを再生し、必要に応じてアニメーションも再生する関数
    public void PlayCvWithMotion(string cv)
    {
        if (!canCv) return;
        if(cvClips.ContainsKey(cv)){
            canCv = false;
            audioSource.PlayOneShot(cvClips[cv]);
            StartCoroutine(OnCvComplete(cvClips[cv].length));
        }
        // CVに紐づくモーションがあれば動作
        if(cvWithMotion.ContainsKey(cv)){
            _motionController.PlayAnimation(animationClips[cvWithMotion[cv]], 0, 3, isLoop: false);
        }
    }

    // 「読み上げ機能」のための割り込みCV読み上げ
    public IEnumerator InsertPlayClipWithMotion(AudioClip clip){
        while (!canCv)
        {
            yield return null;
        }
        canCv = false;
        audioSource.PlayOneShot(clip);
        StartCoroutine(OnCvComplete(clip.length));
        if(cvWithMotion.ContainsKey(clip.name)){
            _motionController.PlayAnimation(animationClips[cvWithMotion[clip.name]], 0, 3, isLoop: true);
            StartCoroutine(OnManualAnimationComplete(clip.length));
        }
    }

    private IEnumerator OnCvComplete(float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        canCv = true;
    }

    private IEnumerator OnManualAnimationComplete(float clipLength){
        yield return new WaitForSeconds(clipLength);
        OnAnimationComplete(0f);
    }

    public void OnAnimationComplete(float instanceId)
    {
        int randValue = random.Next(100);
        string standMotion = randValue < 96 ? "CH_Juewa@Live2D_Standby1" : (randValue < 98 ? "CH_Juewa@Live2D_Standby2" : "CH_Juewa@Live2D_Standby_Sad1");
        _motionController.PlayAnimation(
            animationClips[standMotion],
            0,
            3,
            isLoop: true
        );
    }

    // サンプルとしてStartに適用
    void Start()
    {
        // CVクリップをロード
        cvClips = new Dictionary<string, AudioClip>();
        AudioClip[] _cvClips = Resources.LoadAll<AudioClip>("CV");
        foreach (AudioClip ac in _cvClips)
        {
            cvClips.Add(ac.name, ac);
        }

        animationClips = new Dictionary<string, AnimationClip>();
        _motionController = GetComponent<CubismMotionController>();
        _motionController.AnimationEndHandler += OnAnimationComplete;

        // Resources/Juewa/motion フォルダ内の全ての AnimationClip をロード
        var _animationClips = Resources.LoadAll<AnimationClip>("Juewa/motions");
        foreach (AnimationClip ac in _animationClips)
        {
            animationClips.Add(ac.name, ac);
        }
        _motionController.PlayAnimation(animationClips["CH_Juewa@Live2D_Standby1"], isLoop: true);

    }
    private void Awake()
    {
        // シングルトンインスタンスの設定
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
