using System;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace primerProyectoPrueba.middleware
{
    public class AutorizartionCustomMiddleWare
    {
        private  RequestDelegate _next;

        public AutorizartionCustomMiddleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            

            try
            {
                var claim = context.User.FindFirst("Rol");

                var CONT = context.GetEndpoint().ToString().Equals("HTTP: POST /loggin");



                if (claim != null)
                {
                    if (claim.ToString().Equals("Rol: ADMIN"))
                    {
                        Console.WriteLine("entro");
                        await _next(context);

                    }
                }
                else if (CONT)
                {
                    Console.WriteLine(CONT);
                    await _next(context);


                }
                

            }
            catch(Exception e)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                Console.WriteLine(e);
            }
            

        }
    }

    public static class AuthoorizationMiddlewareExtensions
    {
        public static IApplicationBuilder UseMyCustomMiddleware(
       this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AutorizartionCustomMiddleWare>();
        }
    }
}

