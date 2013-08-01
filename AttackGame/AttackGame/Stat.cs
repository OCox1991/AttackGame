using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttackGame
{
    public class Stat
    {
        /// <summary>
        /// The starting value of the stat
        /// </summary>
        private float baseVal;

        /// <summary>
        /// The increment value of the stat
        /// </summary>
        private float increment;

        /// <summary>
        /// The maximum level of the stat
        /// </summary>
        private int maxLevel;
        public int MaxLevel
        {
            get { return maxLevel; }
        }

        /// <summary>
        /// The current level of the stat
        /// </summary>
        private int currentLevel;
        public int CurrentLevel
        {
            get { return currentLevel;}
        }

        /// <summary>
        /// The name of the stat
        /// </summary>
        private String name;
        public String Name
        {
            get { return name; }
        }

        /// <summary>
        /// The cost of getting from level 1 to level 2 of the stat
        /// </summary>
        private float baseCost;

        public Stat(float baseVal, float increment, int maxLevel, String name, float baseCost)
        {
            this.baseVal = baseVal;
            this.increment = increment;
            this.maxLevel = maxLevel;
            this.name = name;
            this.baseCost = baseCost;
            this.currentLevel = 1;
        }

        public float getNextLevelCost()
        {
            if (currentLevel == maxLevel)
            {
                baseCost = 0;
            }
            return baseCost * currentLevel;
        }

        public float getValue()
        {
            return baseVal + (increment * (currentLevel - 1));
     
        }

        public void upgradeStat()
        {
            currentLevel++;
        }
    }
}
