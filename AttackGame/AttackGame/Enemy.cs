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
    /// The base class for all enemy ships, any stats that all enemies share are dealt with here.
    /// </summary>
    public abstract class Enemy : MovingGameObject
    {
        protected float pointWorth;

        protected float collideDamage;

        protected float hitsLeft;

        private TimeSpan collisionTime;
        private Enemy collidingEnemy;

        private Craft avatar;
        public Craft Avatar
        {
            get { return avatar; }
        }

        public Enemy(String modelname, AttackGame game, Craft avatar)
            : base(modelname, game)
        {
            pointWorth = 100.0f;
            Game.addEnemy(this);
            this.avatar = avatar;
            switch (Game.Difficulty)
            {
                case GameDifficulty.easy:
                    hitsLeft = 1.0f;
                    break;

                case GameDifficulty.medium:
                    hitsLeft = 3.0f;
                    break;

                case GameDifficulty.hard:
                    hitsLeft = 5.0f;
                    break;
            }
            collideDamage = hitsLeft * 10;
            collisionTime = new TimeSpan(0, 0, 10);
        }

        //Taken from 
        /// <summary>
        /// Finds the direction the object would have to look in to be looking at a given object's position.
        /// </summary>
        /// <param name="lookat">The position of the object to be looked at</param>
        /// <returns></returns>
        public Matrix LookAt(Vector3 lookat)
        {
            Matrix rotation = new Matrix();

            rotation.Forward = Vector3.Normalize(lookat - Position);
            rotation.Right = Vector3.Normalize(Vector3.Cross(rotation.Forward, Vector3.Up));
            rotation.Up = Vector3.Normalize(Vector3.Cross(rotation.Right, rotation.Forward));

            return rotation;

        }

        /// <summary>
        /// A simple method that checks enemy collision
        /// </summary>
        /// <param name="gametime">The gametime, used only to call base.update</param>
        public override void Update(GameTime gametime)
        {
            if (IsActive)
            {
                List<Enemy> enemyCollides = Game.listCollides(this, Game.Enemies);
                if (Game.collides(this, Game.Avatar))
                {
                    pointWorth = 0;
                    this.destroy();
                    Game.Avatar.damage(collideDamage);
                }
                else if (enemyCollides.Count > 0)
                {
                    collisionTime = new TimeSpan(0,0,0);
                    collidingEnemy = enemyCollides[0];
                }

                if (collisionTime.TotalSeconds < 0.1f)
                {
                    collisionTime += gametime.ElapsedGameTime;
                    bounce(collidingEnemy, gametime);
                }

                base.updateWorld();
                base.Update(gametime);
            }
        }

        public void bounce(Enemy e, GameTime gameTime)
        {
            Position = Vector3.Add(Position, LookAt(e.Position).Forward * -60.0f * (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        public override void damage(float amount)
        {
            hitsLeft -= amount;
            if (hitsLeft <= 0)
            {
                this.destroy();
            }
        }

        public override void destroy()
        {
            Game.enemyDestroyed(this);
            Game.addPoints(pointWorth);
            base.destroy();
        }
    }

    /// <summary>
    /// A mine is inactive past a certain range, once the player gets within range of it it moves towards them with speed that
    /// depends on the distance between the player and the mine. The fog effect is used to colour inactive so that they fade to
    /// red as they become more active.
    /// </summary>
    class Mine : Enemy
    {
        private Vector3 lightAmount;

        private static Model mineModel;
        public override Model Model
        {
            get { return mineModel; }
            set { mineModel = value; }
        }

        private static BoundingSphere mineSphere;
        public override BoundingSphere BoundingSphere
        {
            get { return mineSphere; }
            set { mineSphere = value; }
        }

        public Mine(String modelname, AttackGame game, Craft avatar)
            : base(modelname, game, avatar)
        {
            lightAmount = new Vector3(0.0f, 0.0f, 0.0f);
            pointWorth = 50.0f;
            switch (Game.Difficulty)
            {
                case GameDifficulty.easy:
                    hitsLeft = 1.0f;
                    collideDamage = 20;
                    break;

                case GameDifficulty.medium:
                    hitsLeft = 1.0f;
                    collideDamage = 40;
                    break;

                case GameDifficulty.hard:
                    hitsLeft = 2.0f;
                    collideDamage = 50;
                    break;
            }

            InstanceBoundingSphere.Radius *= 3;
        }

        /// <summary>
        /// An override of update to make the mines act in the desired way
        /// </summary>
        public override void Update(GameTime gametime)
        {
            if (IsActive)
            {
                Matrix rotation = LookAt(Avatar.Position);
                Direction = rotation.Forward;
                Right = rotation.Right;
                Up = rotation.Up;

                

                float dist = Vector3.Distance(Avatar.Position, this.Position);

                if (dist < 500.0f)
                {
                    lightAmount.X = ((500.0f - dist) / 500.0f);
                    Speed = 250.0f - (float)Math.Pow((double)(dist / 10.0f), 2.0)/10;
                }
                else
                {
                    lightAmount.X = 0.0f;
                }
                base.Update(gametime);
            }
        }

        /// <summary>
        /// An override of the DrawModel method in GameObject that includes lighting effects so the
        /// mines will glow red as they get near
        /// </summary>
        public override void DrawModel(Matrix projectionMatrix, Matrix viewMatrix)
        {
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.World = transforms[mesh.ParentBone.Index] * World;

                    //Custom lighting code taken from http://rbwhitaker.wikidot.com/basic-effect-lighting
                    effect.LightingEnabled = true; // turn on the lighting subsystem for the craft;
                    effect.AmbientLightColor = lightAmount;

                    // Use the matrices provided by the chase camera
                    effect.View = viewMatrix;
                    effect.Projection = projectionMatrix;
                }
                mesh.Draw();
            }
        }
    }

    /// <summary>
    /// An orbiter gets to a certain range of the player, pauses, shoots, pauses again then repositions.
    /// Orbiters attempt to stay on the same horizontal plane as the avatar ship.
    /// </summary>
    class Orbiter : Enemy
    {
        #region Fields: State, idealRange, timespans, model and boundingbox
        enum State { start, pausedPreFire, pausedPostFire, firing, chooseDirection, moving, outOfRange }
        private State state;

        float idealRange;

        TimeSpan pauseTime;
        TimeSpan pause2Time;
        TimeSpan moveTime;
        TimeSpan elapsed;

        private static Model orbiterModel;
        public override Model Model
        {
            get { return orbiterModel; }
            set { orbiterModel = value; }
        }

        private static BoundingSphere orbiterSphere;
        public override BoundingSphere BoundingSphere
        {
            get { return orbiterSphere; }
            set { orbiterSphere = value; }
        }

        #endregion

        public Orbiter(String modelname, AttackGame game, Craft avatar)
            : base(modelname, game, avatar)
        {
            pointWorth = 150.0f;
            StrafeSpeed = 0;
            AscSpeed = 0;
            state = State.start;
            idealRange = 250.0f;
            pauseTime = new TimeSpan(0, 0, 2);
            pause2Time = new TimeSpan(0, 0, 1);
            moveTime = new TimeSpan(0, 0, 4);
        }

        /// <summary>
        /// An override of damage to take into account the Orbiter's special properties, that is, that it takes more damage
        /// if hit in its movement phase.
        /// </summary>
        public override void damage(float amount)
        {
            if (state == State.moving)
            {
                amount *= 1.5f;
            }
            base.damage(amount);
        }

        /// <summary>
        /// An override of update to make the orbiter act in the desired way. The orbiter cycles through states
        /// start -> pausedPreFire -> firing -> pausedPostFire -> chooseDirection -> moving -> start
        /// </summary>
        public override void Update(GameTime gametime)
        {
            if (IsActive)
            {
                Matrix rotation = LookAt(Avatar.Position);
                Direction = rotation.Forward;
                Right = rotation.Right;
                Up = rotation.Up;

                Vector2 XZPos = new Vector2(Position.X, Position.Z);
                Vector2 avatarXZPos = new Vector2(Avatar.Position.X, Avatar.Position.Z);

                float dist = Vector2.Distance(XZPos, avatarXZPos);//uses X and Z co ords to find the distance between it and the target.
                float timeElapsed = (float)gametime.ElapsedGameTime.TotalSeconds; //Need to divide by this to avoid overshooting when navigating to the ideal range.

                if (dist != idealRange)
                {
                    Speed = (dist - idealRange) / timeElapsed;
                    if (Speed > 250.0f)
                    {
                        Speed = 250.0f;
                    }
                    else if (Speed < -250.0f)
                    {
                        Speed = -250.0f;
                    }
                }
                if (!(idealRange - 25 < dist && idealRange + 25 > dist))
                {
                    state = State.outOfRange;
                }

                //Dealing with vertical movement
                if (Position.Y != Avatar.Position.Y)
                {
                    AscSpeed = Avatar.Position.Y - Position.Y;
                    if (AscSpeed > 1.0f / timeElapsed)
                    {
                        AscSpeed = 1.0f / timeElapsed;
                    }
                    else if (AscSpeed < -1.0f / timeElapsed)
                    {
                        AscSpeed = -1.0f / timeElapsed;
                    }
                }

                switch (state)
                {
                    case State.start:
                        elapsed = new TimeSpan(0, 0, 0);
                        state = State.pausedPreFire;
                        break;

                    case State.outOfRange:
                        if (idealRange - 25 < dist && idealRange + 25 > dist)
                        {
                            //if within these limits
                            state = State.pausedPreFire;
                        }
                        break;

                    case State.pausedPreFire:
                        elapsed += gametime.ElapsedGameTime;
                        if (elapsed > pauseTime)
                        {
                            state = State.firing;
                        }
                        AscSpeed = 0;
                        StrafeSpeed = 0;
                        break;

                    case State.firing:
                        elapsed = new TimeSpan(0, 0, 0);
                        fireBullet(500.0f, 20, 300.0f);
                        state = State.pausedPostFire;
                        break;

                    case State.pausedPostFire:
                        elapsed += gametime.ElapsedGameTime;
                        if (elapsed > pause2Time)
                        {
                            state = State.chooseDirection;
                        }
                        break;

                    case State.chooseDirection:
                        elapsed = new TimeSpan(0, 0, 0);
                        Random r = new Random();
                        StrafeSpeed = (float)r.Next(-300, 300); //Change this
                        state = State.moving;
                        break;

                    case State.moving:
                        elapsed += gametime.ElapsedGameTime;
                        if (elapsed > moveTime)
                        {
                            state = State.start;
                        }
                        break;
                }
            }

            base.Update(gametime);
        }
    }
}
