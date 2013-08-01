using System;

namespace AttackGame
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (AttackGame game = new AttackGame())
            {
                game.Run();
            }
        }
    }
#endif
}

