using UnityEngine;

namespace RoverMars.UI
{
    public sealed class StartScreenAnimation : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private RectTransform _title;
        [SerializeField] private RectTransform _start;
        [SerializeField] private UnityEngine.UI.Button _startButton;

        [Header("Title rise")]
        [SerializeField, Min(0f)] private float _titleDownwardOffset = 120f;
        [SerializeField, Min(0.01f)] private float _titleDuration = 1.2f;
        [SerializeField] private AnimationCurve _titleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("START pop-up")]
        [SerializeField, Range(0.01f, 1f)] private float _startInitialScale = 0.1f;
        [SerializeField, Min(0.01f)] private float _startDuration = 0.35f;
        [SerializeField] private AnimationCurve _startCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector2 _titleFinalPosition;
        private Vector3 _startFinalScale;
        private float _elapsed;
        private bool _initialized;

        public bool IsComplete { get; private set; }

        private void Awake()
        {
            if (_title == null || _start == null || _startButton == null)
            {
                Debug.LogError("StartScreenAnimation requires title, START rect and button references.", this);
                enabled = false;
                return;
            }
            Initialize();
            ApplyElapsedTime(0f);
        }

        private void Update()
        {
            if (IsComplete)
                return;
            _elapsed += Time.unscaledDeltaTime;
            ApplyElapsedTime(_elapsed);
        }

        private void Initialize()
        {
            if (_initialized)
                return;
            _titleFinalPosition = _title.anchoredPosition;
            _startFinalScale = _start.localScale;
            _initialized = true;
        }

        // Also allows deterministic inspection of the sequence without playing the scene.
        public void ApplyElapsedTime(float elapsed)
        {
            Initialize();
            float titleDuration = Mathf.Max(0.01f, _titleDuration);
            float startDuration = Mathf.Max(0.01f, _startDuration);
            bool titleFinished = elapsed >= titleDuration;
            IsComplete = elapsed >= titleDuration + startDuration;
            float titleProgress = Mathf.Clamp01(elapsed / titleDuration);
            float titleEase = Mathf.Clamp01(_titleCurve.Evaluate(titleProgress));
            _title.anchoredPosition = titleFinished
                ? _titleFinalPosition
                : _titleFinalPosition + Vector2.down * _titleDownwardOffset * (1f - titleEase);

            _start.gameObject.SetActive(titleFinished);
            float startProgress = Mathf.Clamp01((elapsed - titleDuration) / startDuration);
            float startEase = Mathf.Clamp01(_startCurve.Evaluate(startProgress));
            _start.localScale = IsComplete
                ? _startFinalScale
                : _startFinalScale * Mathf.Lerp(_startInitialScale, 1f, startEase);
            _startButton.interactable = IsComplete;
            _startButton.targetGraphic.raycastTarget = IsComplete;
        }
    }
}
