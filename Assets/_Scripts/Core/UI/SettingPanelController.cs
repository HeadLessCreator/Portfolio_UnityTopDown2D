using UnityEngine;
using UnityEngine.UI;

public class SettingPanelController : MonoBehaviour
{
    [Header("Sound Sliders")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Sound Toggle Buttons")]
    [SerializeField] private Button soundOnButton;
    [SerializeField] private Button soundOffButton;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    private float previousBgmVolume = 1f;
    private float previousSfxVolume = 1f;

    private bool isBinding;

    private void OnEnable()
    {
        RefreshUI();
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void RefreshUI()
    {
        isBinding = true;

        if (SoundManager.Instance != null)
        {
            float bgmVolume = SoundManager.Instance.BGMVolume;
            float sfxVolume = SoundManager.Instance.EffectVolume;

            if (bgmVolume > 0f)
            {
                previousBgmVolume = bgmVolume;
            }

            if (sfxVolume > 0f)
            {
                previousSfxVolume = sfxVolume;
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(bgmVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
            }

            bool isSoundOn = bgmVolume > 0f || sfxVolume > 0f;

            // 패널이 처음 열릴 때만 현재 사운드 상태와 버튼 비주얼을 동기화.
            RefreshSoundToggleVisual(isSoundOn);
        }

        isBinding = false;
    }

    private void BindEvents()
    {
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (soundOnButton != null)
        {
            soundOnButton.onClick.RemoveListener(OnSoundOffButtonClicked);
            soundOnButton.onClick.AddListener(OnSoundOffButtonClicked);
        }

        if (soundOffButton != null)
        {
            soundOffButton.onClick.RemoveListener(OnSoundOnButtonClicked);
            soundOffButton.onClick.AddListener(OnSoundOnButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSettings);
            closeButton.onClick.AddListener(CloseSettings);
        }
    }

    private void UnbindEvents()
    {
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        if (soundOnButton != null)
        {
            soundOnButton.onClick.RemoveListener(OnSoundOffButtonClicked);
        }

        if (soundOffButton != null)
        {
            soundOffButton.onClick.RemoveListener(OnSoundOnButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSettings);
        }
    }

    // OFF 상태 버튼을 눌러서 사운드를 다시 켤 때 호출.
    public void OnSoundOnButtonClicked()
    {
        float bgmVolume = previousBgmVolume > 0f ? previousBgmVolume : 1f;
        float sfxVolume = previousSfxVolume > 0f ? previousSfxVolume : 1f;

        SoundManager.Instance?.SetBGMVolume(bgmVolume);
        SoundManager.Instance?.SetEffectVolume(sfxVolume);

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(bgmVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
        }

        // 버튼 비주얼 전환은 버튼 자체 OnClick의 SetActive 설정이 담당.
    }

    // ON 상태 버튼을 눌러서 사운드를 끌 때 호출.
    public void OnSoundOffButtonClicked()
    {
        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.BGMVolume > 0f)
            {
                previousBgmVolume = SoundManager.Instance.BGMVolume;
            }

            if (SoundManager.Instance.EffectVolume > 0f)
            {
                previousSfxVolume = SoundManager.Instance.EffectVolume;
            }
        }

        SoundManager.Instance?.SetBGMVolume(0f);
        SoundManager.Instance?.SetEffectVolume(0f);

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(0f);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(0f);
        }

        // 버튼 비주얼 전환은 버튼 자체 OnClick의 SetActive 설정이 담당.
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (isBinding)
        {
            return;
        }

        SoundManager.Instance?.SetBGMVolume(value);

        if (value > 0f)
        {
            previousBgmVolume = value;
        }

        RefreshSoundToggleVisualByCurrentVolume();
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (isBinding)
        {
            return;
        }

        SoundManager.Instance?.SetEffectVolume(value);

        if (value > 0f)
        {
            previousSfxVolume = value;
        }

        RefreshSoundToggleVisualByCurrentVolume();
    }

    private void RefreshSoundToggleVisualByCurrentVolume()
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        bool isSoundOn = SoundManager.Instance.BGMVolume > 0f ||
                         SoundManager.Instance.EffectVolume > 0f;

        RefreshSoundToggleVisual(isSoundOn);
    }

    private void RefreshSoundToggleVisual(bool isOn)
    {
        if (soundOnButton != null)
        {
            soundOnButton.gameObject.SetActive(isOn);
        }

        if (soundOffButton != null)
        {
            soundOffButton.gameObject.SetActive(!isOn);
        }
    }

    private void CloseSettings()
    {
        UIManager.Instance?.CloseSettings();
    }
}