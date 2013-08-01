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
    /// <summary>
    /// Moving game objects are handled by this class. The class itself handles updating the position of objects.
    /// </summary>
    public abstract class MovingGameObject : GameObject
    {
        #region Fields: speeds: foward, left/right, up/down

        /// <summary>
        /// The speed the object is moving at.
        /// </summary>
        private float speed;
        public float Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        /// <summary>
        /// The speed the object is strafing left or right at
        /// </summary>
        private float strafeSpeed;
        public float StrafeSpeed
        {
            get { return strafeSpeed; }
            set { strafeSpeed = value; }
        }

        /// <summary>
        /// The speed the object is ascending or descending at
        /// </summary>
        private float ascSpeed;
        public float AscSpeed
        {
            get { return ascSpeed; }
            set { ascSpeed = value; }
        }

        #endregion

        #region Constructor
        public MovingGameObject(String modelname, AttackGame game)
            :base(modelname, game)
        {
            Game.addUpdatingObject(this);
            speed = 0;
            strafeSpeed = 0;
            ascSpeed = 0;
        }

        #endregion

        public virtual void Update(GameTime gametime)
        {
            if (IsActive)
            {
                Vector3 velocity = Direction * Speed;
                Vector3 strafeVelocity = Right * StrafeSpeed;
                Vector3 ascVelocity = Up * AscSpeed;

                float elapsed = (float)gametime.ElapsedGameTime.TotalSeconds;

                Position += velocity * elapsed;
                Position += strafeVelocity * elapsed;
                Position += ascVelocity * elapsed;

                InstanceBoundingSphere.Center = Position;

                boundaries();

                base.updateWorld();
            }
        }

        public void boundaries()
        {
            Position.Y = Math.Max(Position.Y, 0.0f);
            Position.Y = Math.Min(Position.Y, AttackGame.BoundaryCeiling);

            Position.X = Math.Max(Position.X, -AttackGame.BoundaryWall);
            Position.X = Math.Min(Position.X, AttackGame.BoundaryWall);

            Position.Z = Math.Max(Position.Z, -AttackGame.BoundaryWall);
            Position.Z = Math.Min(Position.Z, AttackGame.BoundaryWall);
        }

        /// <summary>
        /// Fires a bullet
        /// </summary>
        /// <param name="speed">The speed the bullet will travel at</param>
        /// <param name="damage">The damage the bullet will do to anything it hits</param>
        /// <param name="parent">The parent of the bullet, the object that fired it</param>
        public void fireBullet(float speed, int damage, float range)
        {
            Bullet bullet = new Bullet("Models/cube10uR", Game, this, range);
            bullet.Speed = speed;
            bullet.Position = this.Position;
            bullet.Up = this.Up;
            bullet.Right = this.Right;
            bullet.Direction = this.Direction;
            bullet.Yield = damage;
            bullet.updateWorld();
        }

        /// <summary>
        /// Destorys the object, removing all references to it.
        /// </summary>
        public override void destroy()
        {
            base.destroy();
            Game.removeUpdatingObject(this);
        }

        public abstract void damage(float amount);
    }
}
