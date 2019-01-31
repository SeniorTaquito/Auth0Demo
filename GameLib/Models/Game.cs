using System;
using System.Collections.Generic;
using System.Text;

namespace GameLib.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public double Cost { get; set; }        
    }
}
