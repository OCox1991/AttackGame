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
    class Terrain : GameObject
    {
        private static Model terrainModel;
        public override Model Model
        {
            get { return terrainModel; }
            set { terrainModel = value; }
        }

        public Terrain(String modelName, AttackGame game)
            : base(modelName, game)
        {
        }
    }
}
