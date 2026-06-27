using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Giữ một world-space UI (hoặc object) luôn quay mặt phẳng về phía camera, để không bị
    /// nghiêng theo góc camera xéo. Dùng cho GoalCounterCanvas / LevelTextCanvas khi camera tilt.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class FaceCameraFlat : MonoBehaviour
    {
        private Camera _cam;

        private void Start()  => Apply();
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (_cam == null) _cam = ResolveCamera();
            if (_cam == null) return;
            // Khớp hướng với camera → mặt UI vuông góc với hướng nhìn → nhìn chính diện, hết nghiêng.
            transform.rotation = _cam.transform.rotation;
        }

        // Camera.main có thể trỏ nhầm sang camera scene khác (vd scene Main). Ưu tiên camera cùng
        // scene với UI này (camera đang render board), fallback về Camera.main.
        private Camera ResolveCamera()
        {
            var scene = gameObject.scene;
            foreach (var c in Camera.allCameras)
                if (c.gameObject.scene == scene) return c;
            return Camera.main;
        }
    }
}
