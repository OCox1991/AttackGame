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
    public class Craft : MovingGameObject
    {
        #region Fields: rotation rate, mass, thrustforce, dragfactor, various velocities, keyboard states, Model and boundingbox

        /// <summary>
        /// Full speed at which ship can rotate; measured in radians per second.
        /// </summary>
        private float RotationRate;

        /// <summary>
        /// Mass of ship.
        /// </summary>
        private const float Mass = 1.0f;

        /// <summary>
        /// Maximum force that can be applied along the ship's direction.
        /// </summary>
        private float ThrustForce;

        /// <summary>
        /// Velocity scalar to approximate drag.
        /// </summary>
        private const float DragFactor = 0.97f;

        /// <summary>
        /// Current ship velocity.
        /// </summary>
        public Vector3 Velocity;

        ///Current ship strafe velocity
        public Vector3 StrafeVelocity;

        //Current ship ascend velocity
        public Vector3 AscendVelocity;

        //Input
        private KeyboardState currentState;
        private KeyboardState prevState;

        //TimeKeeping
        private TimeSpan timeSinceShot;
        private int shotDelayMilliSecs;

        //Ship model
        private static Model shipModel;
        public override Model Model
        {
            get { return shipModel; }
            set { shipModel = value; }
        }

        private static BoundingSphere shipSphere;
        public override BoundingSphere BoundingSphere
        {
            get { return shipSphere; }
            set { shipSphere = value; }
        }

        private float maxShield;
        private float currentShield;
        public float CurrentShield
        {
            get { return currentShield; }
        }

        private Vector3 lightAmount;

        private float shieldRegen;
        private float shieldRegenDelay;

        private float maxHull;
        private float hull;
        public float Hull
        {
            get { return hull; }
        }

        private TimeSpan timeSinceDamage;

        #endregion

        #region Initialization

        public Craft(String ModelName, AttackGame game)
            :base(ModelName, game)
        {
            Velocity = Vector3.Zero;
            StrafeVelocity = Vector3.Zero;
            
            if (Game.PlayerStats != null)
            {
                currentShield = Game.PlayerStats.MaxShield.getValue();
            }

            timeSinceDamage = new TimeSpan(0, 0, 1);

            InstanceBoundingSphere = new BoundingSphere();
        }

        #endregion

        public override void Update(GameTime gameTime)
        {
            #region updating Stats

            float nextMaxHull = Game.PlayerStats.Hull.getValue();
            if (nextMaxHull != maxHull)
            {
                hull = nextMaxHull;
                maxHull = nextMaxHull;
            }

            RotationRate = Game.PlayerStats.TurnRate.getValue();
            ThrustForce = Game.PlayerStats.EngineForce.getValue();
            maxShield = Game.PlayerStats.MaxShield.getValue();
            shieldRegen = Game.PlayerStats.ShieldRegenSpeed.getValue();
            shieldRegenDelay = Game.PlayerStats.ShieldRegenDelay.getValue();

            #endregion

            InstanceBoundingSphere.Center = Position;
            InstanceBoundingSphere.Radius = shipSphere.Radius;

            prevState = currentState;
            currentState = Keyboard.GetState();

            if (timeSinceDamage.TotalMilliseconds > 100)
            {
                lightAmount.X = 0.1f - ((currentShield/maxShield) * 0.1f);
                lightAmount.Y = 0.1f - ((currentShield/maxShield) * 0.1f);
                lightAmount.Z = 0.1f + ((currentShield / maxShield) * 0.9f); //so if at max shield lightamount = 0.0,0.0,1.0, and if at min 0.1,0.1,0.1
            }
            else
            {
                if (currentShield > 0) //Shield hit effect
                {
                    lightAmount.X = 1.0f;
                    lightAmount.Y = 1.0f;
                    lightAmount.Z = 1.0f;
                }
                else //hull hit effect
                {
                    lightAmount.X = 1.0f;
                    lightAmount.Y = 0.2f;
                    lightAmount.Z = 0.2f;
                }
            }

            checkOtherInput(gameTime);

            manageShield(gameTime);

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Determine rotation amount from input
            Vector2 rotationAmount = calculateRotationAmount();

            // Scale rotation amount to radians per second
            rotationAmount = rotationAmount * RotationRate * elapsed;

            // Correct the X axis steering when the ship is upside down
            if (Up.Y < 0)
                rotationAmount.X = -rotationAmount.X;


            // Create rotation matrix from rotation amount 
            Matrix rotationMatrix =
                Matrix.CreateFromAxisAngle(Right, rotationAmount.Y) *
                Matrix.CreateRotationY(rotationAmount.X);

            // Rotate orientation vectors
            Direction = Vector3.TransformNormal(Direction, rotationMatrix);
            Up = Vector3.TransformNormal(Up, rotationMatrix);

            // Re-normalize orientation vectors
            // Without this, the matrix transformations may introduce small rounding
            // errors which add up over time and could destabilize the ship.
            Direction.Normalize();
            Up.Normalize();

            // Re-calculate Right
            Right = Vector3.Cross(Direction, Up);

            // The same instability may cause the 3 orientation vectors may
            // also diverge. Either the Up or Direction vector needs to be
            // re-computed with a cross product to ensure orthagonality
            Up = Vector3.Cross(Right, Direction);


            // Determine thrust amount from input //Change these to be a vector3 that can be normalised.
            float thrustAmount = calculateThrustAmount();

            float strafeThrustAmt = calculateStrafeAmount();

            float ascAmount = calculateAscAmount();

            //Ascending descending or strafing should affect the other thrusts.

            float totalThrust = Math.Abs(thrustAmount) + Math.Abs(strafeThrustAmt) + Math.Abs(ascAmount); 

            if (totalThrust > 0)
            {
                totalThrust *= totalThrust; //makes the difference between moving in one and moving in multiple directions more noticable.
                thrustAmount /= totalThrust;
                strafeThrustAmt /= totalThrust;
                ascAmount /= totalThrust;
            }

            if (thrustAmount < 0)
            {
                thrustAmount /= 4;
            }

            // Calculate force from thrust amount
            Vector3 force = Direction * thrustAmount * ThrustForce; //How this works: Direction will be XYZ with nums

            //Calculate force for strafeing
            Vector3 strafeForce = Right * strafeThrustAmt * ThrustForce;

            Vector3 ascForce = Up * ascAmount * ThrustForce;

            // Apply acceleration
            Vector3 acceleration = force / Mass;
            Velocity += acceleration * elapsed;

            //Apply strafe acceleration
            Vector3 strafeAccel = strafeForce / Mass;
            StrafeVelocity += strafeAccel * elapsed;

            Vector3 ascAccel = ascForce / Mass;
            AscendVelocity += ascAccel * elapsed;

            // Apply psuedo drag to all movement
            Velocity *= DragFactor;
            StrafeVelocity *= DragFactor;
            AscendVelocity *= DragFactor;

            //HERE IS WHERE FUTURE POS CHECKING WOULD BE DONE
            //if future pos collides() with any gameObject
            //Pos = currentpos, velocity = 0 OR velocity = velocity * -0.5
            //take some damage
            //then go on rebuilding

            Vector3 FuturePos = Position + Velocity * elapsed;
            FuturePos += StrafeVelocity * elapsed;
            FuturePos += AscendVelocity * elapsed;

            #region Position checking
            if (FuturePos.Y < 0.0f)
            {
                //Keep from going through the floor
                FuturePos.Y = 0.0f;

                //Apply pseudo rebound & drag to all velocities.
                Velocity.Y *= -1.0f;
                Velocity.X *= 0.25f;
                Velocity.Z *= 0.25f;

                StrafeVelocity.Y *= -1.0f;
                StrafeVelocity.X *= 0.25f;
                StrafeVelocity.Z *= 0.25f;

                AscendVelocity.Y *= -1.0f;
                AscendVelocity.X *= 0.25f;
                AscendVelocity.Z *= 0.25f;

                //Take damage
                damage(1);
            }

            FuturePos.X = Math.Max(FuturePos.X - 5, (AttackGame.BoundaryWall) * -1);
            FuturePos.X = Math.Min(FuturePos.X + 5, AttackGame.BoundaryWall);

            FuturePos.Z = Math.Max(FuturePos.Z - 5, (AttackGame.BoundaryWall) * -1);
            FuturePos.Z = Math.Min(FuturePos.Z + 5, AttackGame.BoundaryWall);

            FuturePos.Y = Math.Min(FuturePos.Y, AttackGame.BoundaryCeiling);
            #endregion

            // Apply velocities
            Position = FuturePos;

            base.updateWorld();
        }

        #region Dealing with input

        public float calculateThrustAmount()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            float thrustAmount = 0;
            if (keyboardState.IsKeyDown(Keys.LeftShift))
                thrustAmount = 1.0f;

            if (keyboardState.IsKeyDown(Keys.LeftControl))
                thrustAmount = -1.0f;

            return thrustAmount;
        }

        public Vector2 calculateRotationAmount()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            Vector2 rotationAmount = Vector2.Zero;
            if (keyboardState.IsKeyDown(Keys.Left))
                rotationAmount.X = 1.0f;
            if (keyboardState.IsKeyDown(Keys.Right))
                rotationAmount.X = -1.0f;
            if (keyboardState.IsKeyDown(Keys.Down))
                rotationAmount.Y = -1.0f;
            if (keyboardState.IsKeyDown(Keys.Up))
                rotationAmount.Y = 1.0f;
            return rotationAmount;
        }

        public float calculateStrafeAmount()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            float strafeThrustAmt = 0;
            if (keyboardState.IsKeyDown(Keys.D))
                strafeThrustAmt = 2.0f;
            if (keyboardState.IsKeyDown(Keys.A))
                strafeThrustAmt = -2.0f;
            return strafeThrustAmt;
        }

        public float calculateAscAmount()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            float ascthrustAmount = 0;
            if (keyboardState.IsKeyDown(Keys.W))
                ascthrustAmount = 2.0f;

            if (keyboardState.IsKeyDown(Keys.S))
                ascthrustAmount = -2.0f;

            return ascthrustAmount;
        }

        public void checkOtherInput(GameTime gametime)
        {
            shotDelayMilliSecs = (int)Game.PlayerStats.ShotDelay.getValue();
            if(prevState.IsKeyUp(Keys.Space) && currentState.IsKeyDown(Keys.Space))
            {
                float shotDmg = Game.PlayerStats.ShotDmg.getValue();
                float shotRange = Game.PlayerStats.ShotRange.getValue();
                float shotSpeed = Game.PlayerStats.ShotSpeed.getValue();

                fireBullet(shotSpeed, (int)shotDmg, shotRange);
                timeSinceShot = new TimeSpan(0, 0, 0);
            }
            else if (prevState.IsKeyDown(Keys.Space) && currentState.IsKeyDown(Keys.Space))
            {
                timeSinceShot += gametime.ElapsedGameTime;
                if (timeSinceShot.Milliseconds > shotDelayMilliSecs)
                {
                    float shotDmg = Game.PlayerStats.ShotDmg.getValue();
                    float shotRange = Game.PlayerStats.ShotRange.getValue();
                    float shotSpeed = Game.PlayerStats.ShotSpeed.getValue();

                    fireBullet(shotSpeed, (int)shotDmg, shotRange);
                    timeSinceShot = new TimeSpan(0, 0, 0);
                }
            }
        }

        #endregion

        public override void damage(float damageAmt)
        {
            float diffMult = 1.0f;
            switch (Game.Difficulty)
            {
                case GameDifficulty.easy:
                    diffMult = 0.5f;
                    break;
                case GameDifficulty.hard:
                    diffMult = 2.0f;
                    break;
            }

            damageAmt *= diffMult;

            if(currentShield >= damageAmt)
            {
                currentShield -= damageAmt;
            }
            else
            {
                damageAmt -= currentShield;
                currentShield = 0;
                hull -= damageAmt;

                //add some lighting change here

                if (hull <= 0)
                {
                    destroy();
                    Game.loseGame();
                }
            }
            timeSinceDamage = new TimeSpan(0, 0, 0);
        }

        public void manageShield(GameTime gametime)
        {
            if (timeSinceDamage.TotalSeconds >= shieldRegenDelay)
            {
                if (currentShield < maxShield)
                {
                    float nextShield = currentShield + (shieldRegen * (float)gametime.ElapsedGameTime.TotalSeconds);
                    if (nextShield > maxShield)
                    {
                        nextShield = maxShield;
                    }
                    currentShield = nextShield;
                }
            }
            else
            {
                timeSinceDamage += gametime.ElapsedGameTime;
            }
        }

        /// <summary>
        /// Simple model drawing method.
        /// </summary>        
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

                        //Remove fog
                        effect.FogEnabled = false;

                        //Used to give a forcefield effect on crafts taken from http://rbwhitaker.wikidot.com/basic-effect-lighting
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
    }
}
