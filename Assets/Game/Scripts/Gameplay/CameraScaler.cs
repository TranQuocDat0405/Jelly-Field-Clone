using UnityEngine;
using Game.Manager;

namespace Game.Gameplay
{
    /// <summary>
    /// Tự điều chỉnh orthographicSize của camera GAMEPLAY (camera nghiêng -15°) để toàn bộ board +
    /// launcher luôn nằm gọn trong màn hình trên MỌI tỉ lệ điện thoại, kể cả board lớn nhất (5×6).
    ///
    /// Camera nghiêng nên KHÔNG dùng công thức phẳng halfWidth/aspect. Thay vào đó chiếu các điểm cần
    /// thấy (4 góc board + đáy launcher) qua <see cref="Camera.WorldToViewportPoint"/> ở orthoSize tham
    /// chiếu, rồi tính hệ số phóng để chúng nằm trong vùng viewport mục tiêu. Với camera orthographic,
    /// độ lệch viewport quanh tâm (0.5,0.5) tỉ lệ NGHỊCH với orthoSize → newOrtho = refOrtho*max(1,maxRatio).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraScaler : MonoBehaviour
    {
        [Header("Reference (màn hình thiết kế 1080×1920)")]
        [SerializeField] private float _refOrthoSize = 5.4f;

        [Header("Vùng viewport mục tiêu cho board (chừa lề + dải HUD phía trên)")]
        [SerializeField] private float _targetLeft   = 0.05f;
        [SerializeField] private float _targetRight  = 0.95f;
        [SerializeField] private float _targetTop    = 0.80f; // chừa ~20% trên cho HUD Level/Count
        [SerializeField] private float _targetBottom = 0.05f;

        [Header("Đáy cần thấy (launcher slot ~ y=-3.8, block ~0.6)")]
        [SerializeField] private float _bottomWorldY = -4.45f;
        [SerializeField] private float _launcherHalfX = 1.8f;

        [Header("Board (fallback nếu không lấy được từ level)")]
        [SerializeField] private int   _fallbackWidth  = 5;
        [SerializeField] private int   _fallbackHeight = 6;
        [SerializeField] private float _slotStep       = 1.0f;

        private Camera _cam;

        private void Awake() => _cam = GetComponent<Camera>();

        private void Start() => Fit();

        private void LateUpdate()
        {
            // Fit mỗi frame để luôn đúng sau khi board/level/độ phân giải đổi (6 phép chiếu — rất nhẹ;
            // Fit() đã có guard Approximately nên không gán orthoSize thừa).
            Fit();
        }

        private void Fit()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null || !_cam.orthographic) return;

            // 1. Board center + kích thước
            Vector3 boardCenter = new Vector3(0f, 0.5f, 0f);
            int w = _fallbackWidth, h = _fallbackHeight;

            var grid = FindObjectOfType<JellyGrid>();
            if (grid != null)
            {
                boardCenter = grid.transform.position;
                w = grid.width; h = grid.height;
            }
            if (LevelManager.IsSingletonAlive && LevelManager.I.CurrentLevel != null)
            {
                w = LevelManager.I.CurrentLevel.gridWidth;
                h = LevelManager.I.CurrentLevel.gridHeight;
            }

            float halfW = w * _slotStep * 0.5f;
            float halfH = h * _slotStep * 0.5f;

            // 2. Tập điểm phải nằm trong vùng mục tiêu
            var pts = new Vector3[]
            {
                boardCenter + new Vector3(-halfW,  halfH, 0f),
                boardCenter + new Vector3( halfW,  halfH, 0f),
                boardCenter + new Vector3(-halfW, -halfH, 0f),
                boardCenter + new Vector3( halfW, -halfH, 0f),
                new Vector3(-_launcherHalfX, _bottomWorldY, 0f),
                new Vector3( _launcherHalfX, _bottomWorldY, 0f),
            };

            // 3. Tính hệ số phóng ở orthoSize tham chiếu (chiếu qua camera nghiêng cho chính xác)
            float prevOrtho = _cam.orthographicSize;
            _cam.orthographicSize = _refOrthoSize;

            float halfTargetR = Mathf.Max(0.01f, _targetRight  - 0.5f);
            float halfTargetL = Mathf.Max(0.01f, 0.5f - _targetLeft);
            float halfTargetT = Mathf.Max(0.01f, _targetTop    - 0.5f);
            float halfTargetB = Mathf.Max(0.01f, 0.5f - _targetBottom);

            float f = 1f;
            foreach (var p in pts)
            {
                Vector3 vp = _cam.WorldToViewportPoint(p);
                float rx = vp.x >= 0.5f ? (vp.x - 0.5f) / halfTargetR : (0.5f - vp.x) / halfTargetL;
                float ry = vp.y >= 0.5f ? (vp.y - 0.5f) / halfTargetT : (0.5f - vp.y) / halfTargetB;
                if (rx > f) f = rx;
                if (ry > f) f = ry;
            }

            // 4. Chỉ phóng to để vừa; máy rộng giữ chuẩn
            float target = _refOrthoSize * Mathf.Max(1f, f);
            _cam.orthographicSize = Mathf.Approximately(target, prevOrtho) ? prevOrtho : target;
        }
    }
}
