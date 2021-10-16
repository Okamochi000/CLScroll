using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 文字スクロール
/// </summary>
public class CLScroll : MonoBehaviour
{
    /// <summary>
    /// スクロール基準
    /// </summary>
    public enum LimitType
    {
        ParentRect, // 親サイズを超えるときスクロール
        WordCount   // 指定文字数を超えるときスクロール
    }

    /// <summary>
    /// スクロール状態
    /// </summary>
    public enum ScrollState
    {
        None,           // スクロール無し
        StartWait,      // スクロール開始待ち
        Scroll,         // スクロール中
        EndWait,        // スクロール終了待ち
        ScrollFinish    // スクロール終了
    }

    /// <summary>
    /// スクロール方法
    /// </summary>
    public enum SpeedType
    {
        Speed,  // 速度指定
        Time    // 時間指定
    }

    /// <summary>
    /// 省略情報
    /// </summary>
    private struct AbbreviationInfo
    {
        public bool isInitialized;
        public string originalText;
        public string abbreviationText;
        public float originalMaskOffsetLeft;
        public float originalMaskOffsetRight;
    }

    public ScrollState State { get; private set; } = ScrollState.None;
    public bool IsAutoUpdate { get; set; } = true;

    [SerializeField] private Text textArea = null;
    [SerializeField] private RectTransform maskArea = null;
    [SerializeField] private LimitType limitType = LimitType.ParentRect;
    [SerializeField] private SpeedType speedType = SpeedType.Speed;
    [SerializeField] private bool isLoop = true;
    [SerializeField] private string abbreviationLastText = "…";
    [SerializeField] [Min(1)] private int characterLimit = 8;
    [SerializeField] [Min(1)] private float scrollSpeed = 25.0f;
    [SerializeField] [Min(0.1f)] private float scrollTime = 2.0f;
    [SerializeField] [Min(0)] private float scrollStartWaitTime = 3.0f;
    [SerializeField] [Min(0)] private float scrollEndWaitTime = 1.0f;

    private float finishAnchorPointX_ = 0.0f;
    private float scrollWaitTime_ = 0.0f;
    private float prevMaskWidth_ = 0.0f;
    private bool isResetting_ = true;
    private TextAnchor textAnchor_ = TextAnchor.MiddleLeft;
    private AbbreviationInfo abbreviationInfo_ = new AbbreviationInfo();

    // Update is called once per frame
    void Update()
    {
        if (isResetting_)
        {
            isResetting_ = false;
            SetText(textArea.text);
        }

        if (IsAutoUpdate)
        {
            UpdateState(State, Time.deltaTime, true);
        }
    }

    void OnEnable()
    {
        if (isResetting_)
        {
            isResetting_ = false;
            SetText(textArea.text);
        }
    }

    /// <summary>
    /// 状態更新
    /// </summary>
    /// <param name="state"></param>
    /// <param name="deltaTime"></param>
    /// <param name="isAutoChange"></param>
    public void UpdateState(ScrollState state, float deltaTime, bool isAutoChange = true)
    {
        // 親サイズが更新されたときマスクサイズ更新
        CheckRectUpdate();

        // 状態切替
        if (State != state)
        {
            if (State != ScrollState.None && state != ScrollState.None)
            {
                ChangeScrollState(state);
            }
        }

        if (State == ScrollState.None || State == ScrollState.ScrollFinish)
        {
            // スクロール無し
            return;
        }
        else if (State == ScrollState.StartWait)
        {
            // スクロール開始待ち
            scrollWaitTime_ += deltaTime;
            if (scrollWaitTime_ > scrollStartWaitTime) { scrollWaitTime_ = scrollStartWaitTime; }
            if (IsPossibleNextState() && isAutoChange) { ChangeScrollState(ScrollState.Scroll); }
        }
        else if (State == ScrollState.Scroll)
        {
            // スクロール
            Vector2 anchoredPosition = textArea.rectTransform.anchoredPosition;
            if (speedType == SpeedType.Speed) { anchoredPosition.x -= deltaTime * scrollSpeed; }
            else if(speedType == SpeedType.Time) { anchoredPosition.x -= deltaTime * (Mathf.Abs(finishAnchorPointX_) / scrollTime); }
            anchoredPosition.x = Mathf.Max(finishAnchorPointX_, anchoredPosition.x);
            textArea.rectTransform.anchoredPosition = anchoredPosition;
            if (IsPossibleNextState() && isAutoChange) { ChangeScrollState(ScrollState.EndWait); }
        }
        else if (State == ScrollState.EndWait)
        {
            // スクロール終了待ち
            scrollWaitTime_ += deltaTime;
            if (scrollWaitTime_ > scrollEndWaitTime) { scrollWaitTime_ = scrollEndWaitTime; }
            if (IsPossibleNextState() && isAutoChange && isLoop) { ChangeScrollState(ScrollState.StartWait); }
        }
    }

    /// <summary>
    /// テキスト設定
    /// </summary>
    /// <param name="text"></param>
    public void SetText(string text)
    {
        // テキスト設定
        textArea.text = text;

        // 非アクティブ状態で更新された場合、アクティブ時に処理を開始する
        if (!this.gameObject.activeInHierarchy || maskArea.rect.size.x <= 0.0f)
        {
            isResetting_ = true;
            return;
        }

        // スクロールの有効状態切替
        bool isScrollEnable = false;
        if (limitType == LimitType.ParentRect)
        {
            float width = maskArea.rect.size.x;
            if (textArea.preferredWidth > width) { isScrollEnable = true; }
        }
        else if (limitType == LimitType.WordCount)
        {
            if (text.Length > characterLimit) { isScrollEnable = true; }
        }

        // 省略情報初期化
        if (abbreviationInfo_.isInitialized)
        {
            maskArea.anchorMin = Vector2.zero;
            maskArea.anchorMax = Vector2.one;
            abbreviationInfo_.isInitialized = false;
            Vector2 offsetMin = maskArea.offsetMin;
            offsetMin.x = abbreviationInfo_.originalMaskOffsetLeft;
            maskArea.offsetMin = offsetMin;
            Vector2 offsetMax = maskArea.offsetMax;
            offsetMax.x = abbreviationInfo_.originalMaskOffsetRight;
            maskArea.offsetMax = offsetMax;
            abbreviationInfo_.originalMaskOffsetLeft = 0.0f;
            abbreviationInfo_.originalMaskOffsetRight = 0.0f;
        }
        abbreviationInfo_.originalText = "";
        abbreviationInfo_.abbreviationText = "";

        if (isScrollEnable)
        {
            // テキストの位置を左端にする
            if (State == ScrollState.None)
            {
                textAnchor_ = textArea.alignment;
                switch (textAnchor_)
                {
                    case TextAnchor.UpperCenter:
                    case TextAnchor.UpperRight:
                        textArea.alignment = TextAnchor.UpperLeft;
                        break;
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.MiddleRight:
                        textArea.alignment = TextAnchor.MiddleLeft;
                        break;
                    case TextAnchor.LowerCenter:
                    case TextAnchor.LowerRight:
                        textArea.alignment = TextAnchor.LowerLeft;
                        break;
                    default: break;
                }
            }

            // テキストエリア設定
            textArea.rectTransform.pivot = new Vector2(0.0f, 0.5f);
            textArea.rectTransform.anchorMin = Vector2.zero;
            textArea.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            textArea.rectTransform.sizeDelta = new Vector2(textArea.preferredWidth, textArea.rectTransform.sizeDelta.y);
            textArea.rectTransform.offsetMin = new Vector2(textArea.rectTransform.offsetMin.x, 0.0f);
            textArea.rectTransform.offsetMax = new Vector2(textArea.rectTransform.offsetMax.x, 0.0f);

            // 省略設定
            if (IsAbridgement())
            {
                SetAbbreviationText();
            }

            // スクロール開始待ち設定
            ChangeScrollState(ScrollState.StartWait);
        }
        else
        {
            // テキストエリア設定
            textArea.rectTransform.pivot = new Vector2(0.0f, 0.5f);
            textArea.rectTransform.anchorMin = Vector2.zero;
            textArea.rectTransform.anchorMax = Vector2.one;
            textArea.rectTransform.offsetMin = Vector2.zero;
            textArea.rectTransform.offsetMax = Vector2.zero;
            if (State != ScrollState.None) { textArea.alignment = textAnchor_; }

            // スクロールしない設定
            ChangeScrollState(ScrollState.None);
        }

        prevMaskWidth_ = maskArea.rect.size.x;
    }

    /// <summary>
    /// 次の遷移が可能であるか
    /// </summary>
    /// <returns></returns>
    public bool IsPossibleNextState()
    {
        switch (State)
        {
            case ScrollState.StartWait:
                if (scrollWaitTime_ >= scrollStartWaitTime) { return true; }
                else { return false; }
            case ScrollState.Scroll:
                if (textArea.rectTransform.anchoredPosition.x <= finishAnchorPointX_) { return true; }
                else { return false; }
            case ScrollState.EndWait:
                if (scrollWaitTime_ >= scrollEndWaitTime) { return true; }
                else { return false; }
            default: break;
        }

        return false;
    }

    /// <summary>
    /// ループ設定
    /// </summary>
    public void SetLoop(bool loop)
    {
        if (isLoop != loop)
        {
            isLoop = loop;
            if (isLoop && State == ScrollState.ScrollFinish)
            {
                ChangeScrollState(ScrollState.StartWait);
            }
        }
    }

    /// <summary>
    /// 進行状況リセット
    /// </summary>
    public void ResetTime()
    {
        scrollWaitTime_ = 0.0f;
        if (State == ScrollState.Scroll)
        {
            Vector2 anchoredPosition = textArea.rectTransform.anchoredPosition;
            anchoredPosition.x = 0.0f;
            textArea.rectTransform.anchoredPosition = anchoredPosition;
        }
    }

    /// <summary>
    /// 再更新の必要があるかチェックする
    /// </summary>
    private void CheckRectUpdate()
    {
        if (prevMaskWidth_ == maskArea.rect.size.x) { return; }

        if (abbreviationInfo_.isInitialized)
        {
            maskArea.anchorMin = Vector2.zero;
            maskArea.anchorMax = Vector2.one;
            abbreviationInfo_.isInitialized = false;
            Vector2 offsetMin = maskArea.offsetMin;
            offsetMin.x = abbreviationInfo_.originalMaskOffsetLeft;
            maskArea.offsetMin = offsetMin;
            Vector2 offsetMax = maskArea.offsetMax;
            offsetMax.x = abbreviationInfo_.originalMaskOffsetRight;
            maskArea.offsetMax = offsetMax;
        }

        if (maskArea.rect.size.x <= 0.0f) { return; }

        if (State == ScrollState.None)
        {
            SetText(textArea.text);
        }
        else
        {
            if (IsAbridgement())
            {
                ScrollState prevState = State;
                float prevWaitTime = scrollWaitTime_;
                SetText(abbreviationInfo_.originalText);
                UpdateState(prevState, prevWaitTime, false);
            }
        }
    }

    /// <summary>
    /// スクロール状態設定
    /// </summary>
    /// <param name="state"></param>
    private void ChangeScrollState(ScrollState state)
    {
        State = state;

        // スクロール待ち時間初期化
        scrollWaitTime_ = 0.0f;

        // 文字省略
        if (IsAbridgement() && abbreviationInfo_.isInitialized)
        {
            if (State == ScrollState.StartWait)
            {
                textArea.text = abbreviationInfo_.abbreviationText;
            }
            else if (State == ScrollState.Scroll)
            {
                textArea.text = abbreviationInfo_.originalText;
            }
        }

        // 文字位置を戻す
        if (State != ScrollState.EndWait && State != ScrollState.ScrollFinish)
        {
            Vector2 anchoredPosition = textArea.rectTransform.anchoredPosition;
            anchoredPosition.x = 0.0f;
            textArea.rectTransform.anchoredPosition = anchoredPosition;
        }

        // スクロール終了位置設定
        if (State == ScrollState.Scroll)
        {
            finishAnchorPointX_ = maskArea.rect.size.x - textArea.preferredWidth;
        }
    }

    /// <summary>
    /// 省略が有効か
    /// </summary>
    /// <returns></returns>
    private bool IsAbridgement()
    {
        if (limitType == LimitType.ParentRect)
        {
            if (abbreviationLastText == "")
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// マスクサイズ更新
    /// </summary>
    private void UpdateMaskSize()
    {
        // 省略表示出ないときはそのまま
        if (!IsAbridgement()) { return; }

        // サイズが変わっていない場合はそのまま
        if (textArea.preferredWidth == maskArea.rect.size.x) { return; }

        // 親サイズ基準の場合はマスクサイズ固定(極端にスペースが開くことがあるため)
        if (limitType == LimitType.ParentRect) { return; }

        Vector2 maskOffsetMin = maskArea.offsetMin;
        Vector2 maskOffsetMax = maskArea.offsetMax;
        float diffWidth = (maskArea.rect.size.x - textArea.preferredWidth);
        switch (textAnchor_)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.MiddleLeft:
            case TextAnchor.LowerLeft:
                maskOffsetMax.x -= diffWidth;
                break;
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                maskOffsetMin.x += (diffWidth / 2.0f);
                maskOffsetMax.x -= (diffWidth / 2.0f);
                break;
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                maskOffsetMin.x += diffWidth;
                break;
            default: break;
        }

        float updateSize = maskOffsetMin.x - maskOffsetMax.x;
        float maxSize = abbreviationInfo_.originalMaskOffsetLeft - abbreviationInfo_.originalMaskOffsetRight;
        if (updateSize > maxSize)
        {
            maskArea.offsetMin = maskOffsetMin;
            maskArea.offsetMax = maskOffsetMax;
        }

        prevMaskWidth_ = maskArea.rect.size.x;
    }

    /// <summary>
    /// 省略テキストを設定する
    /// </summary>
    private void SetAbbreviationText()
    {
        // 初期文保持
        abbreviationInfo_.originalText = textArea.text;
        abbreviationInfo_.originalMaskOffsetLeft = maskArea.offsetMin.x;
        abbreviationInfo_.originalMaskOffsetRight = maskArea.offsetMax.x;

        if (limitType == LimitType.ParentRect)
        {
            // 省略文取得
            TextGenerator generator = textArea.cachedTextGenerator;
            TextGenerationSettings settings = textArea.GetGenerationSettings(textArea.rectTransform.rect.size);
            generator.Populate(textArea.text, settings);
            string resultText = abbreviationInfo_.originalText;
            float maxWidth = maskArea.rect.size.x;
            int charaLength = (int)(maxWidth / (textArea.preferredWidth / (float)abbreviationInfo_.originalText.Length)) - abbreviationLastText.Length;
            if (charaLength >= 0 && charaLength < abbreviationInfo_.originalText.Length)
            {
                resultText = abbreviationInfo_.originalText.Substring(0, charaLength) + abbreviationLastText;
                settings = textArea.GetGenerationSettings(Vector2.zero);
                float preferredWidth = (textArea.cachedTextGeneratorForLayout.GetPreferredWidth(resultText, settings) / textArea.pixelsPerUnit);
                if (preferredWidth > maxWidth)
                {
                    while (preferredWidth > maxWidth)
                    {
                        charaLength--;
                        resultText = abbreviationInfo_.originalText.Substring(0, charaLength) + abbreviationLastText;
                        preferredWidth = (textArea.cachedTextGeneratorForLayout.GetPreferredWidth(resultText, settings) / textArea.pixelsPerUnit);
                    }
                }
                else if (preferredWidth < maxWidth)
                {
                    while (preferredWidth < maxWidth)
                    {
                        charaLength++;
                        resultText = abbreviationInfo_.originalText.Substring(0, charaLength) + abbreviationLastText;
                        preferredWidth = (textArea.cachedTextGeneratorForLayout.GetPreferredWidth(resultText, settings) / textArea.pixelsPerUnit);
                    }
                    charaLength--;
                    resultText = abbreviationInfo_.originalText.Substring(0, charaLength) + abbreviationLastText;
                }
            }
            else
            {
                resultText = abbreviationInfo_.originalText + abbreviationLastText;
            }

            // 省略文設定
            textArea.text = resultText;
        }
        else if (limitType == LimitType.WordCount)
        {
            // 制限文字数分の文字列を抜き出す
            string abbreviationtext = textArea.text.Substring(0, characterLimit);
            textArea.text = abbreviationtext + abbreviationLastText;
        }

        // 省略情報設定
        abbreviationInfo_.abbreviationText = textArea.text;
        abbreviationInfo_.isInitialized = true;

        // マスク情報更新
        UpdateMaskSize();
    }

    /// <summary>
    /// インスペクター変更検知
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying) { return; }
        if (textArea == null || maskArea == null) { return; }
        if (!this.gameObject.activeInHierarchy) { return; }

        if(!isResetting_)
        {
            if (abbreviationInfo_.isInitialized) { textArea.text = abbreviationInfo_.originalText; }
            SetText(textArea.text);
        }
    }
}
