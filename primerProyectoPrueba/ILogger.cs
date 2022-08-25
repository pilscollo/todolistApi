using System;
namespace primerProyectoPrueba
{
    public interface ILogger<out TCategoryName> : ILogger
    {
    }
}

