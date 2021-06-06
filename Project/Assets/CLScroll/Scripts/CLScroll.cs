using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �����X�N���[��
/// </summary>
public class CLScroll : MonoBehaviour
{
    /// <summary>
    /// �X�N���[���
    /// </summary>
    public enum LimitType
    {
        ParentRect, // �e�T�C�Y�𒴂���Ƃ��X�N���[��
        WordCount   // �w�蕶�����𒴂���Ƃ��X�N���[��
    }

    /// <summary>
    /// �X�N���[�����
    /// </summary>
    public enum ScrollState
    {
        None,           // �X�N���[������
        StartWait,      // �X�N���[���J�n�҂�
        Scroll,         // �X�N���[����
        EndWait,        // �X�N���[���I���҂�
        ScrollFinish    // �X�N���[���I��
    }

    /// <summary>
    /// �X�N���[�����@
    /// </summary>
    public enum SpeedType
    {
        Speed,  // ���x�w��
        Time    // ���Ԏw��
    }

    /// <summary>
    /// �ȗ����
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
    [SerializeField] private string abbreviationLastText = "�c";
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
    /// ��ԍX�V
    /// </summary>
    /// <param name="state"></param>
    /// <param name="deltaTime"></param>
    /// <param name="isAutoChange"></param>
    public void UpdateState(ScrollState state, float deltaTime, bool isAutoChange = true)
    {
        // �e�T�C�Y���X�V���ꂽ�Ƃ��}�X�N�T�C�Y�X�V
        CheckRectUpdate();

        // ��Ԑؑ�
        if (State != state)
        {
            if (State != ScrollState.None && state != ScrollState.None)
            {
                ChangeScrollState(state);
            }
        }

        if (State == ScrollState.None || State == ScrollState.ScrollFinish)
        {
            // �X�N���[������
            return;
        }
        else if (State == ScrollState.StartWait)
        {
            // �X�N���[���J�n�҂�
            scrollWaitTime_ += deltaTime;
            if (scrollWaitTime_ > scrollStartWaitTime) { scrollWaitTime_ = scrollStartWaitTime; }
            if (IsPossibleNextState() && isAutoChange) { ChangeScrollState(ScrollState.Scroll); }
        }
        else if (State == ScrollState.Scroll)
        {
            // �X�N���[��
            Vector2 anchoredPosition = textArea.rectTransform.anchoredPosition;
            if (speedType == SpeedType.Speed) { anchoredPosition.x -= deltaTime * scrollSpeed; }
            else if(speedType == SpeedType.Time) { anchoredPosition.x -= deltaTime * (Mathf.Abs(finishAnchorPointX_) / scrollTime); }
            anchoredPosition.x = Mathf.Max(finishAnchorPointX_, anchoredPosition.x);
            textArea.rectTransform.anchoredPosition = anchoredPosition;
            if (IsPossibleNextState() && isAutoChange) { ChangeScrollState(ScrollState.EndWait); }
        }
        else if (State == ScrollState.EndWait)
        {
            // �X�N���[���I���҂�
            scrollWaitTime_ += deltaTime;
            if (scrollWaitTime_ > scrollEndWaitTime) { scrollWaitTime_ = scrollEndWaitTime; }
            if (IsPossibleNextState() && isAutoChange && isLoop) { ChangeScrollState(ScrollState.StartWait); }
        }
    }

    /// <summary>
    /// �e�L�X�g�ݒ�
    /// </summary>
    /// <param name="text"></param>
    public void SetText(string text)
    {
        // �e�L�X�g�ݒ�
        textArea.text = text;

        // ��A�N�e�B�u��ԂōX�V���ꂽ�ꍇ�A�A�N�e�B�u���ɏ������J�n����
        if (!this.gameObject.activeInHierarchy || maskArea.rect.size.x <= 0.0f)
        {
            isResetting_ = true;
            return;
        }

        // �X�N���[���̗L����Ԑؑ�
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

        // �ȗ���񏉊���
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
            // �e�L�X�g�̈ʒu�����[�ɂ���
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

            // �e�L�X�g�G���A�ݒ�
            textArea.rectTransform.pivot = new Vector2(0.0f, 0.5f);
            textArea.rectTransform.anchorMin = Vector2.zero;
            textArea.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            textArea.rectTransform.sizeDelta = new Vector2(textArea.preferredWidth, textArea.rectTransform.sizeDelta.y);
            textArea.rectTransform.offsetMin = new Vector2(textArea.rectTransform.offsetMin.x, 0.0f);
            textArea.rectTransform.offsetMax = new Vector2(textArea.rectTransform.offsetMax.x, 0.0f);

            // �ȗ��ݒ�
            if (IsAbridgement())
            {
                SetAbbreviationText();
            }

            // �X�N���[���J�n�҂��ݒ�
            ChangeScrollState(ScrollState.StartWait);
        }
        else
        {
            // �e�L�X�g�G���A�ݒ�
            textArea.rectTransform.pivot = new Vector2(0.0f, 0.5f);
            textArea.rectTransform.anchorMin = Vector2.zero;
            textArea.rectTransform.anchorMax = Vector2.one;
            textArea.rectTransform.offsetMin = Vector2.zero;
            textArea.rectTransform.offsetMax = Vector2.zero;
            if (State != ScrollState.None) { textArea.alignment = textAnchor_; }

            // �X�N���[�����Ȃ��ݒ�
            ChangeScrollState(ScrollState.None);
        }

        prevMaskWidth_ = maskArea.rect.size.x;
    }

    /// <summary>
    /// ���̑J�ڂ��\�ł��邩
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
    /// ���[�v�ݒ�
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
    /// �i�s�󋵃��Z�b�g
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
    /// �čX�V�̕K�v�����邩�`�F�b�N����
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
    /// �X�N���[����Ԑݒ�
    /// </summary>
    /// <param name="state"></param>
    private void ChangeScrollState(ScrollState state)
    {
        State = state;

        // �X�N���[���҂����ԏ�����
        scrollWaitTime_ = 0.0f;

        // �����ȗ�
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

        // �����ʒu��߂�
        if (State != ScrollState.EndWait && State != ScrollState.ScrollFinish)
        {
            Vector2 anchoredPosition = textArea.rectTransform.anchoredPosition;
            anchoredPosition.x = 0.0f;
            textArea.rectTransform.anchoredPosition = anchoredPosition;
        }

        // �X�N���[���I���ʒu�ݒ�
        if (State == ScrollState.Scroll)
        {
            finishAnchorPointX_ = maskArea.rect.size.x - textArea.preferredWidth;
        }
    }

    /// <summary>
    /// �ȗ����L����
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
    /// �}�X�N�T�C�Y�X�V
    /// </summary>
    private void UpdateMaskSize()
    {
        // �ȗ��\���o�Ȃ��Ƃ��͂��̂܂�
        if (!IsAbridgement()) { return; }

        // �T�C�Y���ς���Ă��Ȃ��ꍇ�͂��̂܂�
        if (textArea.preferredWidth == maskArea.rect.size.x) { return; }

        // �e�T�C�Y��̏ꍇ�̓}�X�N�T�C�Y�Œ�(�ɒ[�ɃX�y�[�X���J�����Ƃ����邽��)
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
    /// �ȗ��e�L�X�g��ݒ肷��
    /// </summary>
    private void SetAbbreviationText()
    {
        // �������ێ�
        abbreviationInfo_.originalText = textArea.text;
        abbreviationInfo_.originalMaskOffsetLeft = maskArea.offsetMin.x;
        abbreviationInfo_.originalMaskOffsetRight = maskArea.offsetMax.x;

        if (limitType == LimitType.ParentRect)
        {
            // �ȗ����擾
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

            // �ȗ����ݒ�
            textArea.text = resultText;
        }
        else if (limitType == LimitType.WordCount)
        {
            // �������������̕�����𔲂��o��
            string abbreviationtext = textArea.text.Substring(0, characterLimit);
            textArea.text = abbreviationtext + abbreviationLastText;
        }

        // �ȗ����ݒ�
        abbreviationInfo_.abbreviationText = textArea.text;
        abbreviationInfo_.isInitialized = true;

        // �}�X�N���X�V
        UpdateMaskSize();
    }

    /// <summary>
    /// �C���X�y�N�^�[�ύX���m
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
