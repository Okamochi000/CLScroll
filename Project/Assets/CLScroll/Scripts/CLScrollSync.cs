using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������CLScroll�𓯊�
/// </summary>
public class CLScrollSync : MonoBehaviour
{
    public CLScroll.ScrollState State { get; private set; }

    private List<CLScroll> scrollList_ = new List<CLScroll>();
    private float playTime_ = 0.0f;

    // Update is called once per frame
    void Update()
    {
        if (State == CLScroll.ScrollState.None)
        {
            return;
        }

        // ���X�g���̃I�u�W�F�N�g�����݂��邩
        scrollList_.Remove(null);
        if (scrollList_.Count == 0)
        {
            State = CLScroll.ScrollState.None;
            playTime_ = 0.0f;
            return;
        }

        // �X�N���[���Ώۂ����݂��邩�m�F
        bool isExistScroll = false;
        foreach (CLScroll clScroll in scrollList_)
        {
            if (clScroll.State != CLScroll.ScrollState.None && clScroll.State != CLScroll.ScrollState.ScrollFinish)
            {
                isExistScroll = true;
                break;
            }
        }
        if (!isExistScroll)
        {
            State = CLScroll.ScrollState.StartWait;
            playTime_ = 0.0f;
            return;
        }

        // �X�N���[������
        bool isNextState = true;
        foreach (CLScroll clScroll in scrollList_)
        {
            if (clScroll.State != CLScroll.ScrollState.None && clScroll.State != CLScroll.ScrollState.ScrollFinish)
            {
                if (State != clScroll.State) { clScroll.UpdateState(State, playTime_, false); }
                clScroll.UpdateState(State, Time.deltaTime, false);
                if (!clScroll.IsPossibleNextState()) { isNextState = false; }
            }
        }
        playTime_ += Time.deltaTime;

        // ���̏�ԂɑJ��
        if (isNextState)
        {
            playTime_ = 0.0f;
            foreach (CLScroll clScroll in scrollList_)
            {
                clScroll.UpdateState(State, 0.0f, true);
            }

            switch (State)
            {
                case CLScroll.ScrollState.StartWait:
                    State = CLScroll.ScrollState.Scroll;
                    break;
                case CLScroll.ScrollState.Scroll:
                    State = CLScroll.ScrollState.EndWait;
                    break;
                case CLScroll.ScrollState.EndWait:
                    State = CLScroll.ScrollState.StartWait;
                    break;
                default: break;
            }
        }
    }

    /// <summary>
    /// �X�N���[���ǉ�
    /// </summary>
    /// <param name="clScroll"></param>
    public void AddCLScroll(CLScroll clScroll)
    {
        if (clScroll == null) { return; }
        if (State == CLScroll.ScrollState.None) { State = CLScroll.ScrollState.StartWait; }

        scrollList_.Add(clScroll);
        clScroll.IsAutoUpdate = false;
        clScroll.UpdateState(State, playTime_, false);
    }

    /// <summary>
    /// �����X�N���[����ǉ�
    /// </summary>
    /// <param name="clScrolls"></param>
    public void AddCLScrolls(CLScroll[] clScrolls)
    {
        if (clScrolls == null || clScrolls.Length == 0) { return; }

        foreach (CLScroll clScroll in clScrolls)
        {
            AddCLScroll(clScroll);
        }
    }
}
