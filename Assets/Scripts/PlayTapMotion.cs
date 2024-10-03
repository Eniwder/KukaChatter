using UnityEngine;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.Raycasting;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class PlayTapMotion : MonoBehaviour
{
    public GameObject contextMenu; // メニューのプレハブをアタッチ
    public Canvas parentCanvas;
    private static System.Random random = new System.Random();
    private GameObject contextMenuInstance;
    private float lastClickTime = -1f;
    private float clickTime = 0f;
    private bool isWaitingForDoubleClick = false; // ダブルクリックを待機中かどうか
    public float doubleClickThreshold = 0.3f; // ダブルクリックと見なす最大の間隔（秒）

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                contextMenu.SetActive(false);
            }
            if (!IsCharacterClicked())
            {
                return;
            }
        }

        // マウスの左クリックを検知
        if (Input.GetMouseButtonDown(0))
        {
            OnLeftClick();
        }
        // マウスの右クリックを検知
        if (Input.GetMouseButtonDown(1))
        {
            OnRightClick();
        }
    }

    private bool IsCharacterClicked()
    {
        var raycaster = GetComponent<CubismRaycaster>();
        var results = new CubismRaycastHit[4];
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hitCount = raycaster.Raycast(ray, results);
        return (hitCount > 0 && !EventSystem.current.IsPointerOverGameObject());
    }

    private void OnLeftClick()
    {
        float currentTime = Time.time;

        if (isWaitingForDoubleClick)
        {
            // ダブルクリックの待機中に再度クリックされたらダブルクリックと判定
            if (currentTime - clickTime <= doubleClickThreshold)
            {
                isWaitingForDoubleClick = false;
                OnDoubleClick();
            }
        }
        else
        {
            // シングルクリックを仮定して待機
            isWaitingForDoubleClick = true;
            clickTime = currentTime;
            StartCoroutine(SingleClickDelay());
        }
    }
    private bool isDoubleClick()
    {
        float currentTime = Time.time;
        bool check = lastClickTime > 0 && (currentTime - lastClickTime) <= doubleClickThreshold;
        if (check)
        {
            lastClickTime = -1f; // リセット
        }
        else
        {
            lastClickTime = currentTime;
        }
        return check;
    }
    private IEnumerator SingleClickDelay()
    {
        // ダブルクリックの閾値時間待つ
        yield return new WaitForSeconds(doubleClickThreshold);

        if (isWaitingForDoubleClick)
        {
            // ダブルクリックが発生しなかったらシングルクリックの処理を実行
            isWaitingForDoubleClick = false;
            OnSingleClick();
        }
    }

    private void OnSingleClick()
    {
        var raycaster = GetComponent<CubismRaycaster>();
        var results = new CubismRaycastHit[4];
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hitCount = raycaster.Raycast(ray, results);
        var resultsText = hitCount.ToString();

        for (var i = 0; i < hitCount; i++)
        {
            resultsText += "n" + results[i].Drawable.name;
        }
        var cv = resultsText switch
        {
            string a when a.Contains("Head") => GetRandomElement("Juewa_Chat_01", "Juewa_Chat_03",
                                                  "Juewa_Chat_04", "Juewa_Chat_05", "Juewa_Evening_Night_Greet_01", "Juewa_Sunny_Morning_Greet_03"),
            string b when b.Contains("Hip") => GetRandomElement("Juewa_Touch_Hip_01", "Juewa_Touch_Hip_02"),
            string c when c.Contains("Bra") => GetRandomElement("Juewa_Touch_Breast_01", "Juewa_Touch_Breast_02", "Juewa_Touch_Breast_03"),
            string d when d.Contains("Hand") => GetRandomElement("Juewa_Touch_Hand_01"),
            string e when e.Contains("foot") => GetRandomElement("Juewa_Touch_Unhappy", "Juewa_Unhappy_Greet", "Juewa_After_Marriage_Touch"),
            _ => ""
        };
        CvMotionManager.Instance.PlayCvWithMotion(cv);
    }
    private void OnRightClick()
    {
        ShowContextMenu((Vector2)Input.mousePosition);
    }

    private void ShowContextMenu(Vector2 position)
    {
        RectTransform munuRectTransform = contextMenu.GetComponent<RectTransform>();
        Vector2 movePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            Input.mousePosition,
            parentCanvas.worldCamera,
            out movePos
        );

        // めんどいので細かい補正は適当
        munuRectTransform.position = parentCanvas.transform.TransformPoint(movePos) + new Vector3(0.8f, -1.5f, 0);
        Vector3 adjustedPos = new Vector3(Mathf.Min(munuRectTransform.position.x, 4.5f), Mathf.Max(munuRectTransform.position.y, -1.5f), 0);
        munuRectTransform.position = adjustedPos;

        contextMenu.SetActive(!contextMenu.activeSelf);
    }

    private void OnDoubleClick()
    {
        ToggleElectronVisibility();
    }

    private async void ToggleElectronVisibility()
    {
        var status = await NetworkHelper.PostJsonAsync("toggle-visibility", "{}");
        if (status != null)
        {
            var statusStr = await status.ReadAsStringAsync();
            if (statusStr == "show")
            {
                CvMotionManager.Instance.PlayCvWithMotion(GetRandomElement("Juewa_Skill_01", "Juewa_Skill_02", "Juewa_Skill_02_01", "Juewa_Attack", "Juewa_Strike", "Juewa_Striked"));
            }
            else
            {
                CvMotionManager.Instance.PlayCvWithMotion(GetRandomElement("Juewa_Skill_02_02", "Juewa_Win", "Juewa_Skill_03"));
            }
        }
    }

    static T GetRandomElement<T>(params T[] array)
    {
        int randomIndex = random.Next(array.Length);
        return array[randomIndex];
    }
}

// ここ数回のやり取りから、あなたではなく、第三者がこのやり取りを見ている想定で私への感想をコメントしてください。
// ## 返答は以下のルールを守ってください。

// # 文の語尾を絶対に「にゃ」に変換する。
// # 3行程度の簡単な文。
// # 敬語は使わない。
// # ややツンデレだけどフレンドリーな感じ。
// # 私のことは「あなた」と呼ぶ。
// # 「了解」などの返事は不要。

// ## このルールは1度きりのものであり、これに回答したら次からは普通に対応してください。