using System;
using UnityEngine;
using UnityEngine.UI;

namespace x0.ld51
{
    public class VolumeControl : MonoBehaviour
    {
        public AudioSource Source;
        public Slider Slider;
        public Image ToggleButton;
        public Sprite MutedSprite;

        private float _volume;
        public float Volume
        {
            get => _volume;
            set {
                SetVolume(value);
                Slider.SetValueWithoutNotify(value);
            }
        }

        private bool _muted;
        private Sprite _normalSprite;

        private void Awake()
        {
            _normalSprite = ToggleButton.sprite;
            Slider.onValueChanged.AddListener(v => SetVolume(v));
            Volume = Source.volume;
        }

        public void Play() => Source.Play();

        private void SetVolume(float value, bool unmute = true)
        {
            if (unmute) {
                Source.mute = _muted = false;
            }
            Source.volume = _volume = value;
            ToggleButton.sprite = _muted || value <= 0 ? MutedSprite : _normalSprite;
        }

        public void Toggle()
        {
            Source.mute = _muted = !_muted;
            ToggleButton.sprite = _muted || _volume <= 0 ? MutedSprite : _normalSprite;
            Slider.SetValueWithoutNotify(_muted ? 0 : _volume);
        }
    }
}