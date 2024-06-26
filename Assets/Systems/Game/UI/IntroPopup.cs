using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Kuroneko.UIDelivery;
using Kuroneko.UtilityDelivery;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class IntroPopup : Popup
{
    public RectTransform tapestry;
    public TMP_Text text;
    [TextArea]
    public string[] introText = Array.Empty<String>();

    private int _index = 0;

    private PlayerControls _playerControls;
    private bool _inputReady = false;
    
    protected override void InitPopup()
    {
        _playerControls = new PlayerControls();
        _playerControls.Game.Shoot.performed += Next;
        _playerControls.Enable();
        text.DOFade(0f, 0f);
        tapestry.DOAnchorPos(new Vector2(800, 0), 0f);
    }

    private void Start()
    {
        tapestry.DOAnchorPos(new Vector2(0, 0), 30f);
        First();
    }

    private void Next(InputAction.CallbackContext callbackContext)
    {
        if (_inputReady)
        {
            Next();
        }
    }

    private void First()
    {
        ShowText(introText[0], this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void Next()
    {
        _index++;
        if (_index >= introText.Length)
        {
            NextScene();
        }
        else
        {
            NextText(introText[_index], this.GetCancellationTokenOnDestroy()).Forget();
        }
    }
    
    private async UniTask ShowText(string data, CancellationToken token)
    {
        _inputReady = false;
        
        //Fade in
        text.SetText(data);
        text.DOFade(1f, 1f).SetEase(Ease.InSine);
        await UniTask.WaitForSeconds(1f, cancellationToken:token);
        
        ServiceLocator.Instance.Get<IAudioService>().Play("INTRO_1");
        
        _inputReady = true;
    }

    private async UniTask NextText(string data, CancellationToken token)
    {
        _inputReady = false;
        
        //Fade out
        text.DOFade(0f, 1f).SetEase(Ease.OutSine);
        await UniTask.WaitForSeconds(1f, cancellationToken:token);
        
        text.SetText(data);
        
        ServiceLocator.Instance.Get<IAudioService>().Play($"INTRO_{_index+1}");
        //Fade in
        text.DOFade(1f, 1f).SetEase(Ease.InSine);
        await UniTask.WaitForSeconds(1f, cancellationToken:token);
        
        _inputReady = true;
    }

    private void NextScene()
    {
        SceneManager.LoadScene(sceneBuildIndex: 1);
        _playerControls.Dispose();
    }

    private void OnDestroy()
    {
        _playerControls.Dispose();
    }
}
