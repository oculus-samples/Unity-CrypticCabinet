// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using CrypticCabinet.OVR;
using Meta.Utilities;
using Oculus.Interaction;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     A visualizer to help with the object placement during scene setup.
    ///     Note: Rigid body necessary to detect trigger events on enter and exit
    /// </summary>
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class ObjectPlacementVisualiser : MonoBehaviour
    {
        [SerializeField] private bool m_userInteractableObject;
        [SerializeField] private float m_radius;
        [SerializeField] private bool m_placeAgainstWall;
        [SerializeField] private bool m_onTheWall;
        [SerializeField] private bool m_flipFaceDirection;
        [SerializeField] private bool m_forceToGroundLevel;
        [SerializeField] private float m_wallObjectPlacementHeight;
        [SerializeField] private float m_wallObjectWidth;
        [SerializeField] private float m_wallObjectVerticalSize;
        [SerializeField] private ObjectPlacementManager.LoadableSceneObjects m_objectType;
        [SerializeField] private GrabbableFixed m_grabbable;
        [SerializeField] private LayerMask m_userWarningLayers;
        [SerializeField] private LayerMask m_physicsEnvironmentLayers;
        [SerializeField] private float m_placementEaseTime = 3;
        [SerializeField] private float m_distFromEdgeLeft = 0;
        [SerializeField] private float m_distFromEdgeRight = 0;

        public float GetRadius => m_radius;
        public float GetWallObjectHeight => m_wallObjectPlacementHeight;
        public float GetWallObjectWidth => m_wallObjectWidth;
        public float GetWallObjectVerticalSize => m_wallObjectVerticalSize;
        public ObjectPlacementManager.LoadableSceneObjects GetObjectType => m_objectType;
        public bool GetFlipFaceDir => m_flipFaceDirection;
        public Vector2 GetDistanceFromEdge => new(m_distFromEdgeLeft, m_distFromEdgeRight);

        public bool IsOnTheWall => m_onTheWall;

        private GameObject m_validationGO;
        private bool m_hasGeometry;
        private Renderer m_validationPrimitiveRenderer;
        public Vector3 GetObjectDimensions
        {
            get
            {
                if (m_boxCollider == null)
                {
                    m_boxCollider = GetComponent<BoxCollider>();
                }
                return m_boxCollider.size;
            }
        }

        private ObjectPlacementManager m_objectPlacementManager;
        [AutoSet][SerializeField] private BoxCollider m_boxCollider;
        private readonly Collider[] m_overlapResults = new Collider[10];
        private bool m_shouldEasePosition;

        private void Awake()
        {
            Debug.Assert(m_boxCollider != null, "Unity has returned a null box collider.", this);
            if (m_placeAgainstWall)
            {
                m_onTheWall = true;
            }
            SetupVisual();
        }

        private IEnumerator Start()
        {
            if (m_grabbable != null)
            {
                m_grabbable.WhenPointerEventRaised += pointerEvent =>
                {
                    if (pointerEvent.Type == PointerEventType.Unselect)
                    {
                        UpdateLocation();
                    }
                };
            }
            else
            {
                Debug.LogError("Missing Grabbable on object placement visualiser", this);
            }

            m_shouldEasePosition = true;
            yield return new WaitForSeconds(m_placementEaseTime);
            m_shouldEasePosition = false;
            UpdateLocation();
        }

        public void Setup(ObjectPlacementManager objectPlacementManager, Vector3 mainPosition, Vector3 wallPos,
            Quaternion wallRotation)
        {
            var pos = m_placeAgainstWall ? wallPos : mainPosition;
            pos.y = m_forceToGroundLevel ? 0 : pos.y;

            Setup(objectPlacementManager, pos);

            transform.rotation = wallRotation;
        }

        public void Setup(ObjectPlacementManager objectPlacementManager, Vector3 position)
        {
            m_objectPlacementManager = objectPlacementManager;

            transform.position = position;
        }

        public void Setup(ObjectPlacementManager objectPlacementManager, Vector3 position, Quaternion rotation)
        {
            m_objectPlacementManager = objectPlacementManager;

            var thisTransform = transform;
            thisTransform.position = position;
            thisTransform.rotation = rotation;
        }

        public void UpdateLocation()
        {
            var thisTransform = transform;
            var position = thisTransform.position;
            var wallPosition = position + (m_placeAgainstWall ? (Vector3.up * m_wallObjectPlacementHeight) : Vector3.zero);

            var rotation = thisTransform.rotation;

            m_objectPlacementManager.UpdatePlacedObject(m_objectType, position, wallPosition, rotation, rotation);
        }

        private void SetupVisual()
        {
            if (!m_hasGeometry)
            {
                var validationPrefab = ObjectPlacementValidator.Instance.GetValidationVisualCubePrefab;
                m_validationGO = Instantiate(validationPrefab, transform);
                var boxColliderBounds = m_boxCollider.bounds;
                m_validationGO.transform.localPosition = m_boxCollider.center;
                m_validationGO.transform.localScale = boxColliderBounds.extents * 2f;
                m_validationPrimitiveRenderer = m_validationGO.GetComponent<Renderer>();
                if (m_validationPrimitiveRenderer == null)
                {
                    Debug.LogError("ObjectValidatorVisualizer: unable to get instantiated primitive renderer");
                    return;
                }

                m_validationPrimitiveRenderer.material.color = GetCorrectColor();
                m_hasGeometry = true;
            }
        }

        private void Update()
        {
            if (m_shouldEasePosition)
            {
                m_validationPrimitiveRenderer.material.color = GetCorrectColor();
                return;
            }

            Transform thisTransform;
            var hitCount = Physics.OverlapBoxNonAlloc(
                (thisTransform = transform).localToWorldMatrix.MultiplyPoint(m_boxCollider.center),
                m_boxCollider.size * (0.95f * 0.5f), m_overlapResults, thisTransform.rotation, m_userWarningLayers);

            // Will always detect it's self so count for hitting others must be higher than 1.
            m_validationPrimitiveRenderer.material.color = hitCount > 1 ?
                ObjectPlacementValidator.Instance.GetPlacementColorIncorrect : GetCorrectColor();
        }

        private void FixedUpdate()
        {
            if (!m_shouldEasePosition)
            {
                return;
            }

            Transform thisTransform;
            var hitCount = Physics.OverlapBoxNonAlloc((thisTransform = transform).localToWorldMatrix.MultiplyPoint(m_boxCollider.center),
                m_boxCollider.size * 0.51f, m_overlapResults, thisTransform.rotation, m_userWarningLayers | m_physicsEnvironmentLayers);

            for (var i = 0; i < hitCount; i++)
            {
                var other = m_overlapResults[i];
                var otherTransform = other.transform;
                if (otherTransform == thisTransform)
                {
                    continue;
                }

                if (IsOnTheWall)
                {
                    if (other.TryGetComponent<ObjectPlacementVisualiser>(out var otherViz) &&
                        otherViz.IsOnTheWall)
                    {
                        var axis = transform.right;
                        var vec = otherTransform.position - transform.position;
                        var dot = Vector3.Dot(vec, axis);
                        var dist = Mathf.Abs(dot);
                        var diff = dist - ((BoxCollider)other).size.x * 0.51f - m_boxCollider.size.x * 0.51f;
                        if (diff < 0)
                        {
                            if (dot < 0)
                            {
                                axis = -axis;
                            }

                            thisTransform.position += axis * (diff * (Mathf.Abs(diff) < 0.01f ? 1 : 0.1f));
                        }
                    }
                    continue;
                }

                if (!Physics.ComputePenetration(m_boxCollider, thisTransform.position, thisTransform.rotation,
                        other, otherTransform.position, otherTransform.rotation, out var dir, out var distance))
                {
                    continue;
                }

                if (Physics.Raycast(thisTransform.position, dir, distance * 1.1f, m_physicsEnvironmentLayers))
                {
                    distance = 0;
                }

                var collideWithWorld = (other.gameObject.layer & m_physicsEnvironmentLayers) > 0;
                thisTransform.position += dir * (distance * (collideWithWorld ? 1 : 0.1f));
            }
        }

        private Color GetCorrectColor()
        {
            return m_userInteractableObject ?
                ObjectPlacementValidator.Instance.GetPlacementColorCorrectAccessible :
                ObjectPlacementValidator.Instance.GetPlacementColorCorrectViewable;
        }
    }
}