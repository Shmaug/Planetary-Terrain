using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTerrain {
    class Program {
        [STAThread]
        static void Main(string[] args) {
            using (Game game = new BetterTerrain.Game()) {
                game.Run();
            }
        }
    }
}
