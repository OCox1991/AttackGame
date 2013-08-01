using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;


namespace AttackGame
{
    public class GameObject
    {
        #region Fields: Position, Direction, Up, Right, Model, ModelName, World matrix, IsActive, Game, instance and class bounding spheres

        /// <summary>
        /// Location of Object in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Direction Object is facing.
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// Object's up vector.
        /// </summary>
        public Vector3 Up;

        /// <summary>
        /// The Object's right vector
        /// </summary>
        private Vector3 right;
        public Vector3 Right
        {
            get { return right; }
            set { right = value; }
        }

        /// <summary>
        /// Location of the model as a String
        /// </summary>
        public String modelName;

        /// <summary>
        /// The world matrix of the object
        /// </summary>
        private Matrix world;
        public Matrix World
        {
            get { return world; }
            set { world = value; }
        }
        
        private bool isActive;
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        private AttackGame game;
        public AttackGame Game
        {
            get { return game; }
        }

        private static Model gameObjModel;
        public virtual Model Model
        {
            get { return gameObjModel; }
            set { gameObjModel = value; }
        }

        private static BoundingSphere gameObjSphere;
        public virtual BoundingSphere BoundingSphere
        {
            get { return gameObjSphere; }
            set { gameObjSphere = value; }
        }

        public BoundingSphere InstanceBoundingSphere;

        #endregion

        public GameObject(String modelName, AttackGame game)
        {
            //setting up model
            this.modelName = modelName;

            //storing the game that this belongs to so objects can be automatically added to the drawing stuff
            this.game = game;

            //position and orientation in world
            Position = Vector3.Down;
            Direction = Vector3.Forward;
            Up = Vector3.Up;
            right = Vector3.Right;
            updateWorld();

            InstanceBoundingSphere = new BoundingSphere();
            InstanceBoundingSphere.Radius = BoundingSphere.Radius;
            InstanceBoundingSphere.Center = Position;

            isActive = true;
            Game.addObject(this);
        }

        public void updateWorld()
        {
            world = Matrix.Identity;
            world.Forward = Direction;
            world.Up = Up;
            world.Right = right;
            world.Translation = Position;
        }

        /// <summary>
        /// Simple model drawing method.
        /// </summary>        
        public virtual void DrawModel(Matrix projectionMatrix, Matrix viewMatrix)
        {
            if (IsActive)
            {
                Matrix[] transforms = new Matrix[Model.Bones.Count];
                Model.CopyAbsoluteBoneTransformsTo(transforms);

                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.EnableDefaultLighting();
                        effect.World = transforms[mesh.ParentBone.Index] * world;
                        // Use the matrices provided by the chase camera
                        effect.View = viewMatrix;
                        effect.Projection = projectionMatrix;
                    }
                    mesh.Draw();
                }
            }
        }

        /// <summary>
        /// Destroys the object, removing all references to it.
        /// </summary>
        public virtual void destroy()
        {
            isActive = false;
            Game.removeObject(this);
        }
    }
}
