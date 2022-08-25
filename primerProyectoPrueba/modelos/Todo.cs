using System;
using primerProyectoPrueba.Data;

namespace primerProyectoPrueba.modelos
{
    public class Todo
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public bool completo { get; set; }
        
        public User User { get; set; }
        public int UserId { get; set; }

    }
}

