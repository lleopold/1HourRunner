using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Game
{
    /// <summary>
    /// Class for storing game stats that consist of player stats, at the beginning of the game + stats that are aqured during the game
    /// </summary>
    internal class GameStats
    {
        public float _playerHealth;
        public float _precisionMin;
        public float _precisionMax;
        public float _precisionStartingAim;
        public float _aimingSpeed;
        public float _playerSpeed;
        public float _reloadSpeed;
    }
}
