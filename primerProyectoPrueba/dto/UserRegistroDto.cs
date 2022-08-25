using System;
using primerProyectoPrueba.modelos;

namespace primerProyectoPrueba.dto
{
    public class UserRegistroDto
    {
        public string nombre { get; set; } = string.Empty;
        public string apellido { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
    }
}

