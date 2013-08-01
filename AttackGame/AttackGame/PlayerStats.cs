using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttackGame
{
    public class PlayerStats
    {
        public List<Stat> statList;

        //Engines
        private Stat turnRate;
        public Stat TurnRate { get { return turnRate; } }

        private Stat engineForce;
        public Stat EngineForce { get { return engineForce; } }
        //Bullets
        private Stat shotDelay;
        public Stat ShotDelay { get { return shotDelay; } }

        private Stat shotRange;
        public Stat ShotRange { get { return shotRange; } }

        private Stat shotDmg;
        public Stat ShotDmg { get { return shotDmg; } }

        private Stat shotSpeed;
        public Stat ShotSpeed { get { return shotSpeed; } }

        //Shields & health
        private Stat maxShield;
        public Stat MaxShield { get { return maxShield; } }

        private Stat shieldRegenDelay;
        public Stat ShieldRegenDelay { get { return shieldRegenDelay; } }

        private Stat shieldRegenSpeed;
        public Stat ShieldRegenSpeed { get { return shieldRegenSpeed; } }

        private Stat hull;
        public Stat Hull { get { return hull; } }

        public PlayerStats()
        {
            turnRate = new Stat(1.0f, 0.1f, 10, "Turning Rate", 25.0f);
            engineForce = new Stat(1000.0f, 100.0f, 10, "Engine Force", 50.0f);

            shotDelay = new Stat(400.0f, -30.0f, 10, "Delay Between Shots", 50.0f);
            shotRange = new Stat(500.0f, 100, 5, "Range of Shots", 75.0f);
            shotDmg = new Stat(1.0f, 1.0f, 3, "Shot Damage", 150.0f);
            shotSpeed = new Stat(1000.0f, 200.0f, 10, "Speed of Each Shot", 50.0f);

            maxShield = new Stat(50.0f, 10.0f, 10, "Shield Capacity", 100.0f);
            shieldRegenDelay = new Stat(6.0f, -0.3f, 10, "Shield Regen Delay", 150.0f); //How many seconds to wait after a hit before regenning shields
            shieldRegenSpeed = new Stat(25.0f, 15.0f, 10, "Shield Regen Speed", 125.0f); //How much shield to regen per second
            hull = new Stat(50.0f, 10.0f, 10, "Hull Strength", 75.0f);

            statList = new List<Stat>();
            statList.Add(turnRate);
            statList.Add(engineForce);
            statList.Add(shotDelay);
            statList.Add(shotRange);
            statList.Add(shotSpeed);
            statList.Add(shotDmg);
            statList.Add(maxShield);
            statList.Add(shieldRegenDelay);
            statList.Add(shieldRegenSpeed);
            statList.Add(hull);
        }
    }
}
