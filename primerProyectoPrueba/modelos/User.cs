using System;
using primerProyectoPrueba.modelos;

namespace primerProyectoPrueba.Data
{
    public class User
    {
        public int Id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string apellido { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string rol { get; set; } = string.Empty;
        public List<Todo> Todos { get; set; }

        public User()
        {
            Todos = new List<Todo>();
        }
    }

}

