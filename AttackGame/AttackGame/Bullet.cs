using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace AttackGame
{
    class Bullet : MovingGameObject
    {
        #region Fields: damage, parent, range, timeAlive

        /// <summary>
        /// The damage this projectile does
        /// </summary>
        private int yield;
        public int Yield
        {
            get { return yield; }
            set { yield = value; }
        }

        private static Model bulletModel;
        public override Model Model
        {
            get { return bulletModel; }
            set { bulletModel = value; }
        }

        private static BoundingSphere bulletBoundingSphere;
        public override BoundingSphere BoundingSphere
        {
            get { return bulletBoundingSphere; }
            set { bulletBoundingSphere = value; }
        }

        private MovingGameObject parent; //used for collision, nothing will collide with the ship that shot it.

        private float range;

        private TimeSpan timeAlive;

        private static SoundEffect hitEffect;
        public SoundEffect HitEffect
        {
            set { hitEffect = value; }
        }

        private static SoundEffect shootEffect;
        public SoundEffect ShootEffect
        {
            set { shootEffect = value; }
        }
        #endregion

        #region Initialisation

        public Bullet(String modelname, AttackGame game, MovingGameObject parent, float range)
            : base(modelname, game)
        {
            //Speed is set in the method to fire the bullet so that the player can have a modifiable bullet speed while
            //enemy bullets all travel at the same speed.
            this.parent = parent;
            this.range = range;
            if (shootEffect != null && Game.PlaySounds)
            {
                shootEffect.Play();
            }
        }

        #endregion

        public override void Update(GameTime gametime)
        {
            if (IsActive)
            {
                timeAlive += gametime.ElapsedGameTime;
                if ((timeAlive.TotalSeconds * Speed) > range)
                {
                    this.destroy();
                }

                //Checking for collision with the floor, ceiling, boundaries
                if (Position.Y <= 0.0f || Position.Y >= AttackGame.BoundaryCeiling
                    || Position.X <= AttackGame.BoundaryWall * -1 || Position.X >= AttackGame.BoundaryWall
                    || Position.Z <= AttackGame.BoundaryWall * -1 || Position.Z >= AttackGame.BoundaryWall)
                {
                    this.destroy();
                }


                //if colliding with enemies: destoy them
                List<MovingGameObject> collidingWith = Game.listCollides(this, Game.UpdateableObjects);
                collidingWith.Remove(parent);

                for(int i = 0; i < collidingWith.Count; i++)
                {
                    MovingGameObject thing = collidingWith[i];
                    if (thing.Model == this.Model)
                    {
                        collidingWith.Remove(thing);
                    }
                }

                if(collidingWith.Count > 0)
                {
                    collidingWith[0].damage(yield);
                    if (Game.PlaySounds)
                    {
                        hitEffect.Play();
                    }
                    this.destroy();
                }

                base.Update(gametime);
            }
        }

        public override void damage(float amount)
        {
            this.destroy();
        }

        public override void DrawModel(Matrix projectionMatrix, Matrix viewMatrix)
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
                        effect.World = transforms[mesh.ParentBone.Index] * World;

                        //Custom lighting tutorial used: http://rbwhitaker.wikidot.com/basic-effect-lighting
                        effect.LightingEnabled = false; // turn off the lighting subsystem for this level;
                        effect.AmbientLightColor = new Vector3(1.0f, 1.0f, 1.0f);

                        // Use the matrices provided by the chase camera
                        effect.View = viewMatrix;
                        effect.Projection = projectionMatrix;
                    }
                    mesh.Draw();
                }
            }

        }
    }
}
