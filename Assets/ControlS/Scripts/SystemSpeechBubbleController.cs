using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ScriptData;

public class SystemSpeechBubbleController : SpeechBubbleController
{
    /// <summary>
    /// 메시지를 띄울 위치
    /// </summary>
    [SerializeField] private Vector3 pos;
    [SerializeField] protected RawImage _ownerImage;

    public override void Init()
    {
        base.Init();

        _speaker = EObject.System;
        transform.localPosition = pos;
        _ownerImage ??= Owner.GetComponent<RawImage>();

        _changeExpression -= OnChangeExpression;
        _changeExpression += OnChangeExpression;

        //ScriptManager.Instance.Play("Test");
    }

    void OnChangeExpression(EExpression expression)
    {
        _ownerImage.texture = Resources.Load<Texture>($"Arts/Expression/{expression.ToString()}");
    }
}
