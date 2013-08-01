using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AttackGame
{
    //Taken from chasecam example
    class Camera
    {
        #region Chased object properties (set externally each frame)

        /// <summary>
        /// Position of avatar.
        /// </summary>
        public Vector3 AvatarPosition
        {
            get { return avatarPosition; }
            set { avatarPosition = value; }
        }
        private Vector3 avatarPosition;

        /// <summary>
        /// Direction the chased object is facing.
        /// </summary>
        public Vector3 AvatarDirection
        {
            get { return avatarDirection; }
            set { avatarDirection = value; }
        }
        private Vector3 avatarDirection;

        /// <summary>
        /// Avatar object's Up vector.
        /// </summary>
        public Vector3 Up
        {
            get { return up; }
            set { up = value; }
        }
        private Vector3 up = Vector3.Up;

        #endregion

        #region Desired camera positioning (set when creating camera or changing view)

        /// <summary>
        /// Desired camera position in the chased object's coordinate system.
        /// </summary>
        public Vector3 DesiredPositionOffset
        {
            get { return desiredPositionOffset; }
            set { desiredPositionOffset = value; }
        }
        private Vector3 desiredPositionOffset = new Vector3(0, 2.0f, 2.0f);

        /// <summary>
        /// Desired camera position in world space.
        /// </summary>
        public Vector3 DesiredPosition
        {
            get
            {
                // Ensure correct value even if update has not been called this frame
                UpdateWorldPositions();

                return desiredPosition;
            }
        }
        private Vector3 desiredPosition;

        /// <summary>
        /// Look at point in the avatar object's coordinate system.
        /// </summary>
        public Vector3 LookAtOffset
        {
            get { return lookAtOffset; }
            set { lookAtOffset = value; }
        }
        private Vector3 lookAtOffset = new Vector3(0, 2.8f, 0);

        /// <summary>
        /// Look at point in world space.
        /// </summary>
        public Vector3 LookAt
        {
            get
            {
                // Ensure correct value even if update has not been called this frame
                UpdateWorldPositions();

                return lookAt;
            }
        }
        private Vector3 lookAt;

        #endregion

        #region Current camera position

        /// <summary>
        /// Position of camera in world space.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
        }
        private Vector3 position;

        #endregion

        #region Perspective properties

        /// <summary>
        /// Perspective aspect ratio. Default value should be overriden by application.
        /// </summary>
        public float AspectRatio
        {
            get { return aspectRatio; }
            set { aspectRatio = value; }
        }
        private float aspectRatio = 4.0f / 3.0f;

        /// <summary>
        /// Perspective field of view.
        /// </summary>
        public float FieldOfView
        {
            get { return fieldOfView; }
            set { fieldOfView = value; }
        }
        private float fieldOfView = MathHelper.ToRadians(45.0f);

        /// <summary>
        /// Distance to the near clipping plane.
        /// </summary>
        public float NearPlaneDistance
        {
            get { return nearPlaneDistance; }
            set { nearPlaneDistance = value; }
        }
        private float nearPlaneDistance = 1.0f;

        /// <summary>
        /// Distance to the far clipping plane.
        /// </summary>
        public float FarPlaneDistance
        {
            get { return farPlaneDistance; }
            set { farPlaneDistance = value; }
        }
        private float farPlaneDistance = 1000.0f;

        #endregion

        #region Matrix properties

        /// <summary>
        /// View transform matrix.
        /// </summary>
        public Matrix ViewMatrix
        {
            get { return view; }
        }
        private Matrix view;

        /// <summary>
        /// Projecton transform matrix.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get { return projection; }
        }
        private Matrix projection;

        #endregion

        #region Methods

        /// <summary>
        /// Rebuilds object space values in world space. Invoke before publicly
        /// returning or privately accessing world space values.
        /// </summary>
        private void UpdateWorldPositions()
        {
            // Construct a matrix to transform from object space to worldspace
            Matrix transform = Matrix.Identity;
            transform.Forward = AvatarDirection;
            transform.Up = Up;
            transform.Right = Vector3.Cross(Up, AvatarDirection);

            // Calculate desired camera properties in world space
            desiredPosition = AvatarPosition +
                Vector3.TransformNormal(DesiredPositionOffset, transform);
            lookAt = AvatarPosition +
                Vector3.TransformNormal(LookAtOffset, transform);
        }

        /// <summary>
        /// Rebuilds camera's view and projection matricies.
        /// </summary>
        private void UpdateMatrices()
        {
            view = Matrix.CreateLookAt(this.Position, this.LookAt, this.Up);
            projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView,
                AspectRatio, NearPlaneDistance, FarPlaneDistance);
        }

        public void updateCameraPosition(Vector3 position, Vector3 direction, Vector3 up)
        {
            this.AvatarPosition = position;
            this.AvatarDirection = direction;
            this.Up = up;
        }

        public void Update()
        {
            UpdateWorldPositions();

            position = desiredPosition;

            UpdateMatrices();
        }

        #endregion
    }
}
