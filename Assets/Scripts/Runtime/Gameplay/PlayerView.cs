using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Player entity: a body geometry plus a face geometry under a model pivot. The
    /// model pivot rotates with facing so the face stays on the front of the body.
    /// Placeholder primitives; stylized art arrives later.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private Transform face;

        public Transform Model => model;
        public Transform Face => face;

        public static PlayerView Create(Transform parent, Vector3 worldPosition, Direction facing)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;

            var view = go.AddComponent<PlayerView>();

            var modelGo = new GameObject("Model");
            modelGo.transform.SetParent(go.transform, false);
            view.model = modelGo.transform;

            const float bodyScale = 0.55f;
            PrimitiveFactory.Create(
                "Body",
                modelGo.transform,
                new Vector3(0f, bodyScale * 0.5f, 0f),
                new Vector3(bodyScale, bodyScale, bodyScale),
                Palette.PlayerBody,
                PrimitiveFactory.GetCube());

            GameObject faceGo = PrimitiveFactory.Create(
                "Face",
                modelGo.transform,
                new Vector3(0f, bodyScale * 0.62f, bodyScale * 0.42f),
                new Vector3(bodyScale * 0.45f, bodyScale * 0.35f, bodyScale * 0.2f),
                Palette.PlayerFace,
                PrimitiveFactory.GetCube());

            view.face = faceGo.transform;
            view.SetFacing(facing);
            return view;
        }

        public void SetFacing(Direction facing)
        {
            if (model != null)
                model.localRotation = Quaternion.Euler(0f, BoardGeometry.ToYRotation(facing), 0f);
        }

        public void SetFacingAngle(float yRotation)
        {
            if (model != null)
                model.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }
}
