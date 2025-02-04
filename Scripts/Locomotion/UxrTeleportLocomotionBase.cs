﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrTeleportLocomotionBase.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using UltimateXR.Avatar;
using UltimateXR.CameraUtils;
using UltimateXR.Core;
using UltimateXR.Core.Caching;
using UltimateXR.Devices;
using UltimateXR.Extensions.Unity;
using UltimateXR.Manipulation;
using UnityEngine;

namespace UltimateXR.Locomotion
{
    /// <summary>
    ///     Base component for teleport locomotion.
    /// </summary>
    public abstract class UxrTeleportLocomotionBase : UxrLocomotion, IUxrPrecacheable
    {
        #region Inspector Properties/Serialized Fields

        // General parameters

        [SerializeField] private UxrHandSide          _controllerHand           = UxrHandSide.Left;
        [SerializeField] private bool                 _useControllerForward     = true;
        [SerializeField] private float                _shakeFilter              = 0.4f;
        [SerializeField] private UxrTranslationType   _translationType          = UxrTranslationType.Fade;
        [SerializeField] private Color                _fadeTranslationColor     = Color.black;
        [SerializeField] private float                _fadeTranslationSeconds   = UxrConstants.TeleportTranslationSeconds;
        [SerializeField] private float                _smoothTranslationSeconds = UxrConstants.TeleportTranslationSeconds;
        [SerializeField] private bool                 _allowJoystickBackStep    = true;
        [SerializeField] private float                _backStepDistance         = 2.0f;
        [SerializeField] private UxrRotationType      _rotationType             = UxrRotationType.Immediate;
        [SerializeField] private float                _rotationStepDegrees      = 45.0f;
        [SerializeField] private Color                _fadeRotationColor        = Color.black;
        [SerializeField] private float                _fadeRotationSeconds      = UxrConstants.TeleportRotationSeconds;
        [SerializeField] private float                _smoothRotationSeconds    = UxrConstants.TeleportRotationSeconds;
        [SerializeField] private UxrReorientationType _reorientationType        = UxrReorientationType.AllowUserJoystickRedirect;

        // Target

        [SerializeField] private UxrTeleportTarget _target;
        [SerializeField] private float             _targetPlacementAboveHit = 0.01f;
        [SerializeField] private bool              _showTargetAlsoWhenInvalid;
        [SerializeField] private Color             _validMaterialColorTargets   = Color.white;
        [SerializeField] private Color             _invalidMaterialColorTargets = Color.red;

        // Constraints

        [SerializeField] private QueryTriggerInteraction _triggerCollidersInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private float                   _maxAllowedDistance          = 20.0f;
        [SerializeField] private float                   _maxAllowedHeightDifference  = 10.0f;
        [SerializeField] private float                   _maxAllowedSlopeDegrees      = 30.0f;
        [SerializeField] private float                   _destinationValidationRadius = 0.25f;
        [SerializeField] private LayerMask               _validTargetLayers           = ~0;
        [SerializeField] private LayerMask               _blockingTargetLayers        = ~0;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets the hand used to control the teleport component.
        /// </summary>
        public UxrHandSide HandSide => _controllerHand;

        /// <summary>
        ///     Gets or sets the teleport translation type.
        /// </summary>
        public UxrTranslationType TranslationType
        {
            get => _translationType;
            set => _translationType = value;
        }

        /// <summary>
        ///     Gets or sets the fade color when using <see cref="UxrTranslationType.Fade" /> translation teleporting.
        /// </summary>
        public Color FadeTranslationColor
        {
            get => _fadeTranslationColor;
            set => _fadeTranslationColor = value;
        }

        /// <summary>
        ///     Gets or sets the transition duration in seconds for the <see cref="UxrTranslationType.Fade" /> translation type.
        /// </summary>
        public float FadeTranslationSeconds
        {
            get => _fadeTranslationSeconds;
            set => _fadeTranslationSeconds = value;
        }

        /// <summary>
        ///     Gets or sets the transition duration in seconds for the <see cref="UxrTranslationType.Smooth" /> translation type.
        /// </summary>
        public float SmoothTranslationSeconds
        {
            get => _smoothTranslationSeconds;
            set => _smoothTranslationSeconds = value;
        }

        /// <summary>
        ///     Gets or sets the teleport rotation type.
        /// </summary>
        public UxrRotationType RotationType
        {
            get => _rotationType;
            set => _rotationType = value;
        }

        /// <summary>
        ///     Gets or sets the amount of degrees rotated around the avatar axis when the user presses the left or right joystick
        ///     buttons.
        /// </summary>
        public float RotationStepDegrees
        {
            get => _rotationStepDegrees;
            set => _rotationStepDegrees = value;
        }

        /// <summary>
        ///     Gets or sets the fade color when using <see cref="UxrRotationType.Fade" /> translation teleporting.
        /// </summary>
        public Color FadeRotationColor
        {
            get => _fadeRotationColor;
            set => _fadeRotationColor = value;
        }

        /// <summary>
        ///     Gets or sets the transition duration in seconds for the <see cref="UxrRotationType.Fade" /> rotation type.
        /// </summary>
        public float FadeRotationSeconds
        {
            get => _fadeRotationSeconds;
            set => _fadeRotationSeconds = value;
        }

        /// <summary>
        ///     Gets or sets the transition duration in seconds for the <see cref="UxrRotationType.Smooth" /> rotation type.
        /// </summary>
        public float SmoothRotationSeconds
        {
            get => _smoothRotationSeconds;
            set => _smoothRotationSeconds = value;
        }

        /// <summary>
        ///     Gets or sets how the teleport target direction is set.
        /// </summary>
        public UxrReorientationType ReorientationType
        {
            get => _reorientationType;
            set => _reorientationType = value;
        }

        /// <summary>
        ///     Gets or sets the target object.
        /// </summary>
        public UxrTeleportTarget Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        ///     Gets or sets the distance above the ground the target is positioned.
        /// </summary>
        public float TargetPlacementAboveHit
        {
            get => _targetPlacementAboveHit;
            set => _targetPlacementAboveHit = value;
        }

        /// <summary>
        ///     Gets or sets whether the target should also be visible when the teleport destination is not valid.
        /// </summary>
        public bool ShowTargetAlsoWhenInvalid
        {
            get => _showTargetAlsoWhenInvalid;
            set => _showTargetAlsoWhenInvalid = value;
        }

        /// <summary>
        ///     When <see cref="ShowTargetAlsoWhenInvalid" /> is true, sets the teleport target color used when the destination is
        ///     valid.
        /// </summary>
        public Color ValidMaterialColorTargets
        {
            get => _validMaterialColorTargets;
            set => _validMaterialColorTargets = value;
        }

        /// <summary>
        ///     When <see cref="ShowTargetAlsoWhenInvalid" /> is true, sets the teleport target color used when the destination is
        ///     invalid.
        /// </summary>
        public Color InvalidMaterialColorTargets
        {
            get => _invalidMaterialColorTargets;
            set => _invalidMaterialColorTargets = value;
        }

        /// <summary>
        ///     Gets or sets the behaviour for raycasts against trigger volumes.
        /// </summary>
        public QueryTriggerInteraction TriggerCollidersInteraction
        {
            get => _triggerCollidersInteraction;
            set => _triggerCollidersInteraction = value;
        }

        /// <summary>
        ///     Gets or sets the maximum teleport distance.
        /// </summary>
        public float MaxAllowedDistance
        {
            get => _maxAllowedDistance;
            set => _maxAllowedDistance = value;
        }

        /// <summary>
        ///     Gets or sets the maximum height difference allowed from the current position to a destination.
        /// </summary>
        public float MaxAllowedHeightDifference
        {
            get => _maxAllowedHeightDifference;
            set => _maxAllowedHeightDifference = value;
        }

        /// <summary>
        ///     Gets or sets the maximum slop for a destination to be considered valid.
        /// </summary>
        public float MaxAllowedSlopeDegrees
        {
            get => _maxAllowedSlopeDegrees;
            set => _maxAllowedSlopeDegrees = value;
        }

        /// <summary>
        ///     Gets or sets the radius of a cylinder used when validating if a teleport destination is valid.
        /// </summary>
        public float DestinationValidationRadius
        {
            get => _destinationValidationRadius;
            set => _destinationValidationRadius = value;
        }

        /// <summary>
        ///     Gets or sets the layers over which teleportation is allowed.
        /// </summary>
        public LayerMask ValidTargetLayers
        {
            get => _validTargetLayers;
            set => _validTargetLayers = value;
        }

        /// <summary>
        ///     Gets or sets the layers which should be considered when ray-casting looking for either valid or invalid
        ///     teleportation surfaces.
        /// </summary>
        public LayerMask BlockingTargetLayers
        {
            get => _blockingTargetLayers;
            set => _blockingTargetLayers = value;
        }

        #endregion

        #region Implicit IUxrPrecacheable

        /// <inheritdoc />
        public IEnumerable<GameObject> PrecachedInstances
        {
            get
            {
                if (Target != null)
                {
                    yield return Target.gameObject;
                }
            }
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the component. Should also be called in child classes.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Look for other avatar teleports

            _otherAvatarTeleports = new List<UxrTeleportLocomotionBase>();

            UxrTeleportLocomotionBase[] allAvatarTeleports = Avatar.GetComponentsInChildren<UxrTeleportLocomotionBase>();

            foreach (UxrTeleportLocomotionBase teleport in allAvatarTeleports.Where(teleport => teleport != this))
            {
                _otherAvatarTeleports.Add(teleport);
            }

            // If the teleport target is a prefab, instantiate. Otherwise just reference the object in the scene

            if (_target.gameObject.IsPrefab())
            {
                _teleportTarget = Instantiate(_target, Avatar.transform);
            }
            else
            {
                _teleportTarget = _target;

                if (_teleportTarget != null)
                {
                    _teleportTarget.transform.parent = Avatar.transform;
                }
            }

            // Set initial state

            if (_teleportTarget != null)
            {
                _teleportTarget.transform.rotation = Avatar.transform.rotation;
                TeleportPosition                   = Avatar.transform.position;
                TeleportDirection                  = Avatar.ProjectedCameraForward;
            }

            _layerMaskRaycast.value = BlockingTargetLayers.value | ValidTargetLayers.value;
        }

        /// <summary>
        ///     Resets the component and subscribes to events.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            UxrManager.AvatarMoved += UxrManager_AvatarMoved;

            EnableTeleportObjects(false, false);

            _isBackStepAvailable = true;
            _isValidTeleport     = false;
            IsTeleporting        = false;
            ControllerStart      = RawControllerStart;
            ControllerForward    = RawControllerForward;
        }

        /// <summary>
        ///     Clear some states and unsubscribes from events.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrManager.AvatarMoved -= UxrManager_AvatarMoved;

            NotifyTeleportSpawnCollider(null);
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called when the avatar moved.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event parameters</param>
        private void UxrManager_AvatarMoved(object sender, UxrAvatarMoveEventArgs e)
        {
            if (e.Avatar == Avatar)
            {
                ControllerStart   = RawControllerStart;
                ControllerForward = RawControllerForward;
            }
        }

        #endregion

        #region Protected Overrides UxrLocomotion

        /// <inheritdoc />
        protected override void UpdateLocomotion()
        {
            if (Avatar == null)
            {
                return;
            }

            // Check for back-step and rotations

            if (Avatar.ControllerInput.GetButtonsPressUp(_controllerHand, UxrInputButtons.JoystickDown))
            {
                _isBackStepAvailable = true;
            }

            bool backStepInput = false;

            if (IsAllowedToTeleport)
            {
                backStepInput = _allowJoystickBackStep && CanBackStep && Avatar.ControllerInput.GetButtonsPress(_controllerHand, UxrInputButtons.JoystickDown);

                if (RotationType != UxrRotationType.NotAllowed)
                {
                    if (Avatar.ControllerInput.GetButtonsPressDown(_controllerHand, UxrInputButtons.JoystickLeft) && CanRotate)
                    {
                        Rotate(-RotationStepDegrees);
                        return;
                    }

                    if (Avatar.ControllerInput.GetButtonsPressDown(_controllerHand, UxrInputButtons.JoystickRight) && CanRotate)
                    {
                        Rotate(RotationStepDegrees);
                        return;
                    }
                }
            }

            // Back step?

            if (backStepInput && _isBackStepAvailable && IsAllowedToTeleport)
            {
                Vector3 newPosition = Avatar.CameraFloorPosition - Avatar.ProjectedCameraForward * _backStepDistance;

                if (IsValidTeleport(true, ref newPosition, Avatar.transform.up, out bool _))
                {
                    _isBackStepAvailable = false;
                    _isValidTeleport     = true;
                    TeleportPosition     = newPosition;
                    TeleportDirection    = Avatar.ProjectedCameraForward;
                    TryTeleportUsingCurrentTarget();
                    return;
                }
            }

            if (_shakeFilter > 0.0f && IsTeleporting && IsOtherComponentTeleporting == false)
            {
                float deltaTimeMultiplier = Mathf.Lerp(DeltaTimeMultiplierFilterMin, DeltaTimeMultiplierFilterMax, Mathf.Clamp01(_shakeFilter));

                ControllerStart   = Vector3.Lerp(ControllerStart,   RawControllerStart,   Time.deltaTime * deltaTimeMultiplier);
                ControllerForward = Vector3.Lerp(ControllerForward, RawControllerForward, Time.deltaTime * deltaTimeMultiplier);
            }
            else
            {
                ControllerStart   = RawControllerStart;
                ControllerForward = RawControllerForward;
            }

            // Update locomotion in child classes

            UpdateTeleportLocomotion();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Can be overriden in child classes to execute the additional per-frame teleport locomotion logic.
        /// </summary>
        protected virtual void UpdateTeleportLocomotion()
        {
        }

        /// <summary>
        ///     Cancels the current teleport target. When overriden in child classes the base class should be called too.
        /// </summary>
        protected virtual void CancelTarget()
        {
            EnableTeleportObjects(false, false);
            _isValidTeleport = false;
        }

        /// <summary>
        ///     Checks if the teleport position is valid.
        /// </summary>
        /// <param name="checkBlockingInBetween">
        ///     Should it check for blocking elements in a straight line from the current position to the new position?
        /// </param>
        /// <param name="newPosition">
        ///     Teleport position. If should passed as reference because it may have slight corrections
        /// </param>
        /// <param name="hitNormal">The hit normal that generated the teleport position candidate</param>
        /// <param name="isValidSlope">Returns a boolean telling if the slope at the destination is valid or not</param>
        /// <returns>Boolean telling whether the new position is a valid teleport destination or not</returns>
        protected bool IsValidTeleport(bool checkBlockingInBetween, ref Vector3 newPosition, Vector3 hitNormal, out bool isValidSlope)
        {
            Vector3 eyePosStart = Avatar.CameraFloorPosition;
            Vector3 eyePosEnd   = newPosition;

            isValidSlope = true;

            if (!IsAllowedToTeleport)
            {
                return false;
            }

            if (Mathf.Abs(newPosition.y - Avatar.transform.position.y) > MaxAllowedHeightDifference)
            {
                return false;
            }

            eyePosStart.y = Avatar.CameraPosition.y;
            eyePosEnd.y   = newPosition.y + (Avatar.CameraPosition.y - Avatar.transform.position.y);

            // Check if there is something blocking in a straight line if requested, used in a back step

            if (checkBlockingInBetween)
            {
                Vector3 direction = eyePosEnd - eyePosStart;

                if (Physics.Raycast(eyePosStart, direction.normalized, out RaycastHit _, direction.magnitude, BlockingTargetLayers, TriggerCollidersInteraction))
                {
                    // There is something blocking in between
                    return false;
                }
            }
            else
            {
                // First perform a sphere test on the place where the head would be teleported to see if we can have an early negative.
                if (Physics.CheckSphere(eyePosEnd, HeadRadius, BlockingTargetLayers, TriggerCollidersInteraction))
                {
                    return false;
                }

                if (MaxAllowedHeightDifference > 0.0f && Vector3.Angle(hitNormal, Vector3.up) > MaxAllowedSlopeDegrees)
                {
                    // Check if we are hitting a tall enough wall to see if we can have an early negative. This avoids the filtering
                    // below to allow climbing up the first portion of the wall.
                    // What we do is raycast in an inclined upwards direction to see if the wall is significantly enough above the raycast.

                    Vector3 rayStart = newPosition + hitNormal * 0.1f;
                    Vector3 rayEnd   = newPosition + Vector3.up * MaxAllowedHeightDifference;

                    if (Physics.Raycast(rayStart, (rayEnd - rayStart).normalized, out RaycastHit _, Vector3.Distance(rayStart, rayEnd) + 0.01f, BlockingTargetLayers, TriggerCollidersInteraction))
                    {
                        // We are hitting the base of a tall wall
                        isValidSlope = false;
                        return false;
                    }
                }
            }

            // If not, we want to check also a radius around the destination pos. If a certain number of positions within
            // this radius are valid we consider it also a valid destination. This removes some unwanted false negatives due
            // to small height or slope differences on the ground.

            int positives   = 0;
            int validSlopes = 0;

            for (int i = 0; i < DestinationValidationSubdivisions; ++i)
            {
                float offsetT = 1.0f / DestinationValidationSubdivisions * 0.5f;
                float radians = Mathf.PI * 2.0f * (i * (1.0f / DestinationValidationSubdivisions) + offsetT);

                Vector3 offset = new Vector3(Mathf.Cos(radians), 0.0f, Mathf.Sin(radians));

                if (IsValidDestination(newPosition, eyePosEnd + offset.normalized * DestinationValidationRadius, out bool isValidSlopeSubdivision))
                {
                    positives++;
                }

                validSlopes += isValidSlopeSubdivision ? 1 : 0;
            }

            isValidSlope = validSlopes >= DestinationValidationPositivesNeeded;

            return positives >= DestinationValidationPositivesNeeded && isValidSlope;
        }

        /// <summary>
        ///     Cancels all other current teleport targets. When overriden in child classes the base class should be called too.
        /// </summary>
        protected void CancelOtherTeleportTargets()
        {
            foreach (UxrTeleportLocomotionBase otherTeleport in _otherAvatarTeleports)
            {
                otherTeleport.CancelTarget();
            }
        }

        /// <summary>
        ///     Checks whether the given raycast hits have any that are blocking. A blocking raycast can either be a valid or
        ///     invalid teleport destination depending on many factors. Use <see cref="IsValidDestination" /> to check whether the
        ///     given position is valid.
        /// </summary>
        /// <param name="inputHits">Set of raycast hits to check</param>
        /// <param name="outputHit">Result blocking raycast</param>
        /// <returns>Whether there is a blocking raycast returned in <paramref name="outputHit" /></returns>
        protected bool HasBlockingRaycastHit(RaycastHit[] inputHits, out RaycastHit outputHit)
        {
            bool hasBlockingHit = false;
            outputHit = default;

            if (inputHits.Count() > 1)
            {
                Array.Sort(inputHits, (a, b) => a.distance.CompareTo(b.distance));
            }

            foreach (RaycastHit singleHit in inputHits)
            {
                if (singleHit.collider.GetComponentInParent<UxrAvatar>() != null)
                {
                    // Filter out colliding against part of an avatar
                    continue;
                }

                UxrGrabbableObject grabbableObject = singleHit.collider.GetComponentInParent<UxrGrabbableObject>();

                if (grabbableObject != null && grabbableObject.IsBeingGrabbed)
                {
                    // Filter out colliding against a grabbed object
                    continue;
                }

                outputHit      = singleHit;
                hasBlockingHit = true;
                break;
            }

            return hasBlockingHit;
        }

        /// <summary>
        ///     Notifies a raycast was selected to be a potential destination. Computes whether the destination is valid. If it is,
        ///     sets the appropriate internal state that can later be executed using <see cref="TryTeleportUsingCurrentTarget" />.
        /// </summary>
        /// <param name="hit">Raycast that will be processed as a potential teleport destination</param>
        /// <returns>Whether the destination is a valid teleport location</returns>
        protected bool NotifyDestinationRaycast(RaycastHit hit)
        {
            _isValidTeleport = true;
            
            bool ignoreDestination = hit.collider.GetComponentInParent<UxrIgnoreTeleportDestination>() != null;

            // Check for UxrTeleportSpawnCollider component

            UxrTeleportSpawnCollider teleportSpawnCollider = hit.collider.GetComponent<UxrTeleportSpawnCollider>();

            NotifyTeleportSpawnCollider(teleportSpawnCollider);

            if (teleportSpawnCollider != null && teleportSpawnCollider.enabled && !ignoreDestination)
            {
                Transform spawnPos = teleportSpawnCollider.GetSpawnPos(Avatar, out Vector3 _);

                if (spawnPos != null)
                {
                    TeleportPosition  = spawnPos.position;
                    TeleportDirection = Vector3.ProjectOnPlane(spawnPos.forward, Vector3.up);

                    EnableTeleportObjects(true, true);
                }
            }
            else
            {
                Vector3 teleportPos = hit.point;
                bool    showTarget  = true;

                _isValidTeleport = IsValidTeleport(false, ref teleportPos, hit.normal, out bool validSlope) && !ignoreDestination;

                if (_isValidTeleport && validSlope)
                {
                    // Hit against valid target
                    EnableTeleportObjects(true, _isValidTeleport);
                }
                else
                {
                    // Hit against blocking object or invalid slope
                    _isValidTeleport = false;
                    showTarget       = ShowTargetAlsoWhenInvalid && validSlope;
                    EnableTeleportObjects(showTarget, _isValidTeleport);
                }

                if (showTarget)
                {
                    // Place target

                    TeleportPosition = teleportPos;

                    if (ReorientationType == UxrReorientationType.KeepOrientation)
                    {
                        TeleportDirection = Avatar.ProjectedCameraForward;
                    }
                    else if (ReorientationType == UxrReorientationType.UseTeleportFromToDirection)
                    {
                        TeleportDirection = Vector3.ProjectOnPlane(TeleportPosition - Avatar.CameraPosition, Vector3.up);
                    }
                    else if (ReorientationType == UxrReorientationType.AllowUserJoystickRedirect)
                    {
                        Vector2 joystickValue     = Avatar.ControllerInput.GetInput2D(HandSide, UxrInput2D.Joystick);
                        Vector3 projectedForward  = Vector3.ProjectOnPlane(ControllerForward, Vector3.up).normalized;
                        Vector3 joystickDirection = new Vector3(joystickValue.x, 0.0f, joystickValue.y).normalized;

                        TeleportDirection = Quaternion.LookRotation(projectedForward, Vector3.up) * Quaternion.LookRotation(joystickDirection, Vector3.up) * Vector3.forward;
                    }
                }
            }

            return _isValidTeleport;
        }

        /// <summary>
        ///     Notifies that no raycast were found to be processed as a  potential teleport destination.
        /// </summary>
        protected void NotifyNoDestinationRaycast()
        {
            _isValidTeleport = false;
            EnableTeleportObjects(false, false);
            NotifyTeleportSpawnCollider(null);
        }

        /// <summary>
        ///     Tries to teleport the avatar using the current <see cref="TeleportPosition" /> and <see cref="TeleportDirection" />
        ///     values, only if the current destination is valid and the avatar isn't currently being teleported.
        /// </summary>
        protected void TryTeleportUsingCurrentTarget()
        {
            // Teleport if we can!

            if (_isValidTeleport && !IsTeleporting && !IsOtherComponentTeleporting && IsAllowedToTeleport)
            {
                IsTeleporting = true;

                Transform  avatarTransform = Avatar.transform;
                Vector3    avatarPos       = avatarTransform.position;
                Vector3    avatarUp        = avatarTransform.up;
                Quaternion avatarRot       = avatarTransform.rotation;

                if (TranslationType == UxrTranslationType.Fade)
                {
                    UxrManager.Instance.TeleportFadeColor = FadeTranslationColor;
                }

                UxrManager.Instance.TeleportLocalAvatar(TeleportPosition,
                                                        Quaternion.LookRotation(TeleportDirection),
                                                        _translationType,
                                                        TranslationSeconds,
                                                        () =>
                                                        {
                                                            if (_lastSpawnCollider != null)
                                                            {
                                                                _lastSpawnCollider.RaiseTeleported(new UxrAvatarMoveEventArgs(Avatar, avatarPos, avatarRot, TeleportPosition, Quaternion.LookRotation(TeleportDirection, avatarUp)));
                                                            }
                                                        },
                                                        finished =>
                                                        {
                                                            _isValidTeleport  = false;
                                                            IsTeleporting     = false;
                                                            ControllerStart   = RawControllerStart;
                                                            ControllerForward = RawControllerForward;
                                                        });
            }

            NotifyTeleportSpawnCollider(null);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Checks if a given teleport position is valid. We use the eye position of the teleport destination as
        ///     a reference to be able to raycast to the ground and check for valid layers and slope angle and if there
        ///     is a discrepancy between the raycast and the expected floor height.
        /// </summary>
        /// <param name="teleportPos">The floor level position of the teleport</param>
        /// <param name="newEyePos">The eye position that will be used as reference for the teleport destination</param>
        /// <param name="isValidSlope">Will return if it is a valid slope</param>
        /// <returns>Boolean telling whether newEyePos is a valid teleport position or not</returns>
        private bool IsValidDestination(Vector3 teleportPos, Vector3 newEyePos, out bool isValidSlope)
        {
            isValidSlope = false;

            float eyeHeight = newEyePos.y - teleportPos.y;

            if (Physics.Raycast(newEyePos, -Vector3.up, out RaycastHit hit, eyeHeight * 1.2f, LayerMaskRaycast, TriggerCollidersInteraction))
            {
                float slopeDegrees = Mathf.Abs(Vector3.Angle(hit.normal, Vector3.up));

                isValidSlope = slopeDegrees < MaxAllowedSlopeDegrees;
                bool valid = isValidSlope && (ValidTargetLayers.value & 1 << hit.collider.gameObject.layer) != 0;
                valid = valid && Mathf.Abs(hit.point.y - teleportPos.y) < MaxVerticalHeightDisparity;

                if (hit.collider.GetComponentInParent<UxrIgnoreTeleportDestination>() != null)
                {
                    return false;
                }

                if (valid)
                {
                    // Raycast upwards to see if there is something between the ground and eye level. Since we can be teleported inside a box
                    // at eye level for instance, the previous raycast will not handle that case. We need to raycast from outside as well

                    if (Physics.Raycast(teleportPos, Vector3.up, out hit, eyeHeight, LayerMaskRaycast, TriggerCollidersInteraction))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Notifies a change in the currently targeted <see cref="UxrTeleportSpawnCollider" /> component.
        /// </summary>
        /// <param name="teleportSpawnCollider">New currently targeted component or null if none is selected</param>
        private void NotifyTeleportSpawnCollider(UxrTeleportSpawnCollider teleportSpawnCollider)
        {
            if (teleportSpawnCollider && teleportSpawnCollider.enabled)
            {
                if (_lastSpawnCollider != null && teleportSpawnCollider != _lastSpawnCollider && _lastSpawnCollider.EnableWhenSelected)
                {
                    _lastSpawnCollider.EnableWhenSelected.SetActive(false);
                }
                else if (_lastSpawnCollider != teleportSpawnCollider && teleportSpawnCollider.EnableWhenSelected)
                {
                    if (teleportSpawnCollider.EnableWhenSelected.activeSelf == false)
                    {
                        teleportSpawnCollider.EnableWhenSelected.SetActive(true);
                    }
                }

                _lastSpawnCollider = teleportSpawnCollider;
            }
            else
            {
                if (_lastSpawnCollider != null && _lastSpawnCollider.EnableWhenSelected)
                {
                    _lastSpawnCollider.EnableWhenSelected.SetActive(false);
                }

                _lastSpawnCollider = null;
            }
        }

        /// <summary>
        ///     Enables or disables teleport graphical components.
        /// </summary>
        /// <param name="enableTarget">Whether to enable the teleport target object</param>
        /// <param name="validTeleport">Whether the current teleport destination is valid</param>
        private void EnableTeleportObjects(bool enableTarget, bool validTeleport)
        {
            // Enable / disable

            if (_teleportTarget != null)
            {
                _teleportTarget.gameObject.SetActive(enableTarget);
            }

            // Set materials

            if (ShowTargetAlsoWhenInvalid)
            {
                _teleportTarget.SetMaterialColor(validTeleport ? ValidMaterialColorTargets : InvalidMaterialColorTargets);
            }
        }

        /// <summary>
        ///     Rotates the avatar around its vertical axis, where a positive angle turns it to the right and a negative angle to
        ///     the left.
        /// </summary>
        /// <param name="degrees">Degrees to rotate</param>
        private void Rotate(float degrees)
        {
            if (!IsTeleporting && !IsOtherComponentTeleporting)
            {
                IsTeleporting = true;

                Transform  avatarTransform = Avatar.transform;
                Vector3    avatarPos       = avatarTransform.position;
                Vector3    avatarUp        = avatarTransform.up;
                Quaternion avatarRot       = avatarTransform.rotation;

                if (RotationType == UxrRotationType.Fade)
                {
                    UxrManager.Instance.TeleportFadeColor = FadeRotationColor;
                }

                UxrManager.Instance.RotateLocalAvatar(degrees,
                                                      RotationType,
                                                      RotationSeconds,
                                                      () =>
                                                      {
                                                          if (_lastSpawnCollider != null)
                                                          {
                                                              _lastSpawnCollider.RaiseTeleported(new UxrAvatarMoveEventArgs(Avatar, avatarPos, avatarRot, TeleportPosition, Quaternion.LookRotation(TeleportDirection, avatarUp)));
                                                          }
                                                      },
                                                      finished =>
                                                      {
                                                          IsTeleporting     = false;
                                                          ControllerStart   = RawControllerStart;
                                                          ControllerForward = RawControllerForward;
                                                      });
            }

            NotifyTeleportSpawnCollider(null);
        }

        #endregion

        #region Protected Types & Data

        /// <summary>
        ///     Gets whether the avatar can currently receive input to step backwards.
        /// </summary>
        protected virtual bool CanBackStep => true;

        /// <summary>
        ///     Gets whether the avatar can currently receive input to rotate around.
        /// </summary>
        protected virtual bool CanRotate => true;

        /// <summary>
        ///     Gets whether other teleport component is currently teleporting the avatar.
        /// </summary>
        protected bool IsOtherComponentTeleporting => _otherAvatarTeleports != null && _otherAvatarTeleports.Any(otherTeleport => otherTeleport.IsTeleporting);

        /// <summary>
        ///     Gets whether the component is currently allowed to teleport the avatar.
        /// </summary>
        protected bool IsAllowedToTeleport
        {
            get
            {
                if (IsTeleporting || UxrAvatar.LocalAvatarCamera == null)
                {
                    // Component is currently teleporting
                    return false;
                }

                if (UxrCameraWallFade.IsAvatarPeekingThroughGeometry(Avatar))
                {
                    // Head is currently inside a wall. Avoid teleportation for "cheating".
                    return false;
                }

                Vector3 cameraPos          = UxrAvatar.LocalAvatar.CameraPosition;
                Vector3 cameraToController = ControllerStart - cameraPos;

                return !HasBlockingRaycastHit(Physics.RaycastAll(cameraPos, cameraToController.normalized, cameraToController.magnitude, LayerMaskRaycast, TriggerCollidersInteraction), out RaycastHit hit);
            }
        }

        /// <summary>
        ///     Gets the <see cref="LayerMask" /> used for ray-casting either valid or invalid teleport destinations.
        /// </summary>
        protected LayerMask LayerMaskRaycast => _layerMaskRaycast;

        /// <summary>
        ///     Gets or sets whether the component is currently teleporting the avatar.
        /// </summary>
        protected bool IsTeleporting { get; private set; }

        /// <summary>
        ///     Gets the smoothed source of ray-casting when it starts on the controller.
        /// </summary>
        protected Vector3 ControllerStart { get; private set; }

        /// <summary>
        ///     Gets the smoothed direction of ray-casting when starts on the controller.
        /// </summary>
        protected Vector3 ControllerForward { get; private set; }

        /// <summary>
        ///     Gets or sets the current teleport destination.
        /// </summary>
        protected Vector3 TeleportPosition
        {
            get => _teleportPosition;
            set
            {
                _teleportPosition = value;

                if (_teleportTarget != null)
                {
                    _teleportTarget.transform.position = value + Vector3.up * TargetPlacementAboveHit;
                    _teleportTarget.OrientArrow(Quaternion.LookRotation(TeleportDirection));
                }
            }
        }

        /// <summary>
        ///     Gets or sets the current teleport direction.
        /// </summary>
        protected Vector3 TeleportDirection
        {
            get => _teleportDirection;
            set
            {
                _teleportDirection = value;

                if (_teleportTarget != null)
                {
                    _teleportTarget.OrientArrow(Quaternion.LookRotation(value));
                }
            }
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Gets the raw unprocessed world position on the controller where the ray-casting starts.
        /// </summary>
        private Vector3 RawControllerStart
        {
            get
            {
                if (_useControllerForward && Avatar != null)
                {
                    Transform forwardTransform = Avatar.GetControllerInputForward(_controllerHand);
                    return forwardTransform ? forwardTransform.position : transform.position;
                }

                return transform.position;
            }
        }

        /// <summary>
        ///     Gets the raw unprocessed world direction on the controller used for ray-casting.
        /// </summary>
        private Vector3 RawControllerForward
        {
            get
            {
                if (_useControllerForward && Avatar != null)
                {
                    Transform forwardTransform = Avatar.GetControllerInputForward(_controllerHand);
                    return forwardTransform ? forwardTransform.forward : transform.forward;
                }

                return transform.forward;
            }
        }

        /// <summary>
        ///     Gets the translation transition in seconds depending on <see cref="TranslationType" />.
        /// </summary>
        private float TranslationSeconds
        {
            get
            {
                return _translationType switch
                       {
                                   UxrTranslationType.Fade   => _fadeTranslationSeconds,
                                   UxrTranslationType.Smooth => _smoothTranslationSeconds,
                                   _                         => 0.0f
                       };
            }
        }

        /// <summary>
        ///     Gets the rotation transition in seconds depending on <see cref="RotationType" />.
        /// </summary>
        private float RotationSeconds
        {
            get
            {
                return _rotationType switch
                       {
                                   UxrRotationType.Fade   => _fadeRotationSeconds,
                                   UxrRotationType.Smooth => _smoothRotationSeconds,
                                   _                      => 0.0f
                       };
            }
        }

        private const float HeadRadius                           = 0.2f;
        private const float DeltaTimeMultiplierFilterMin         = 25.0f;
        private const float DeltaTimeMultiplierFilterMax         = 5.0f;
        private const float MaxVerticalHeightDisparity           = 0.2f;
        private const float DestinationValidationSubdivisions    = 8;
        private const float DestinationValidationPositivesNeeded = 5;

        private List<UxrTeleportLocomotionBase> _otherAvatarTeleports;

        private bool                     _isBackStepAvailable;
        private bool                     _isValidTeleport;
        private Vector3                  _teleportPosition;
        private Vector3                  _teleportDirection;
        private LayerMask                _layerMaskRaycast = 0;
        private UxrTeleportTarget        _teleportTarget;
        private UxrTeleportSpawnCollider _lastSpawnCollider;

        #endregion
    }
}