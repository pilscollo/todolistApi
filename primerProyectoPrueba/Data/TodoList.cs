using System;
using Microsoft.EntityFrameworkCore;
using primerProyectoPrueba.modelos;

namespace primerProyectoPrueba.Data
{
    
    public class TodoList : DbContext
    {
        public DbSet<Todo> todolist { get; set; }
        public DbSet<User> users { get; set; }
        public TodoList(DbContextOptions<TodoList> options) : base(options)
        {
           
        }
    }
}

