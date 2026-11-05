using UnityEngine;
using UnityEngine.UI;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public sealed class UIMainPanel : MonoBehaviour
    {
        [SerializeField] private Image playPauseImage;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Image musicImage;
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;

        public void TogglePlaybackSprite()
        {
            if (playPauseImage == null || playSprite == null || pauseSprite == null) return;

            playPauseImage.sprite = playPauseImage.sprite == playSprite ? pauseSprite : playSprite;
        }

        public void ToggleMusicSprite()
        {
            if (musicImage == null || musicOnSprite == null || musicOffSprite == null) return;

            musicImage.sprite = musicImage.sprite == musicOnSprite ? musicOffSprite : musicOnSprite;
        }
    }
}
