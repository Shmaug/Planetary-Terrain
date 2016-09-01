using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planetary_Terrain {
    class Program {
        [STAThread]
        static void Main(string[] args) {
            using (Game game = new Planetary_Terrain.Game()) {
                game.Run();
            }
        }
    }
}
