using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using primerProyectoPrueba.Data;
using primerProyectoPrueba.modelos;
using Serilog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using primerProyectoPrueba.dto;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using primerProyectoPrueba;
using primerProyectoPrueba.middleware;
using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((cx, lc) =>
{
    lc.WriteTo.Console()
      .WriteTo.File("./logs/microsite-api-logs.txt", rollingInterval: RollingInterval.Day);
});
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
builder.Services.AddDbContext<TodoList>(options => options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});
const int maxRequestLimit = 209715200;
// If using IIS
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = maxRequestLimit;
});
// If using Kestrel
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestLimit;
});
builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = maxRequestLimit;
    x.MultipartBodyLengthLimit = maxRequestLimit;
    x.MultipartHeadersLengthLimit = maxRequestLimit;
});

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy(name: "mycours", o =>
//    {
//        o.WithOrigins("http://127.0.0.1:5500");
//        o.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
//        .AllowAnyHeader().AllowAnyMethod();
//    });
//}
//);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                          
                      });
    
});
builder.Services.AddSwaggerGen(c =>
{
    

    c.AddSecurityDefinition(JwtAuthenticationDefaults.AuthenticationScheme,
    new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = JwtAuthenticationDefaults.HeaderName, // Authorization
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
   
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = JwtAuthenticationDefaults.AuthenticationScheme
            }
        },
        new List<string>()
    }
});
});


var app = builder.Build();
app.UseCors(MyAllowSpecificOrigins);
app.UseRouting();

//Enable Authentication
app.UseAuthentication();

app.UseAuthorization();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseMiddleware<AutorizartionCustomMiddleWare>();


app.MapPost("/loggin",[AllowAnonymous]async([FromForm]UserDto userdto, TodoList db) =>
{
    // confirmar que usuario no existe
    // verificar rol
    // generar token
    try
    {
        var user = await db.users.Where(o => o.username.Equals(userdto.Username) && o.password.Equals(userdto.Password)).FirstOrDefaultAsync();  
        if (user is User)
        {
            var issuer = builder.Configuration["Jwt:Issuer"];
            var audience = builder.Configuration["Jwt:Audience"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Now its ime to define the jwt token which will be responsible of creating our tokens
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            // We get our secret from the appsettings
            var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);

            // we define our token descriptor
            // We need to utilise claims which are properties in our token which gives information about the token
            // which belong to the specific user who it belongs to
            // so it could contain their id, name, email the good part is that these information
            // are generated by our server and identity framework which is valid and trusted
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim("Id", user.Id.ToString()),
                // the JTI is used for our refresh token which we will be convering in the next video
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role,"User")

            }),
                // the life span of the token needs to be shorter and utilise refresh token to keep the user signedin
                // but since this is a demo app we can extend it to fit our current need
                Expires = DateTime.UtcNow.AddMinutes(30),
                Audience = audience,
                Issuer = issuer,
                // here we are adding the encryption alogorithim information which will be used to decrypt our token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);

            var jwtToken = jwtTokenHandler.WriteToken(token);

            return Results.Ok(jwtToken);
        }
       
    }
    catch (Exception e)
    {
        Console.WriteLine(e); 
    }
    return Results.Unauthorized();

});
app.MapPost("/user/registro",[AllowAnonymous] async (UserRegistroDto userdto, TodoList db) => {

    if (db.users.Where(t => t.username.Equals(userdto.username)).FirstOrDefault() is null)
    {
        if (userdto.nombre.Trim() =="" || userdto.apellido.Trim() == "" || userdto.username.Trim() == "" || userdto.password.Trim() == "")
        {
            return Results.NoContent();
        }
        else
        {
            User user = new User();
            user.apellido = userdto.apellido.Trim();
            user.nombre = userdto.nombre.Trim();
            user.password = userdto.password.Trim();
            user.username = userdto.username.Trim();
            user.rol = "usuario";
            db.users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created("/user", user);
        }
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapPost("/todolist",[Authorize(Roles = "User")] async (todoDTO tdto, TodoList db,HttpContext context) =>
{
    try
    {
           var id = context.User.FindFirstValue("Id");
        
            Todo t = new Todo();
            t.completo = tdto.completo;
            t.nombre = tdto.nombre;
            t.UserId = Int32.Parse(id);
            var user = await db.users.Where(c => c.Id == Int32.Parse(id)).FirstOrDefaultAsync();
            if (user is User)
            {
                user.Todos.Append(t);
                t.User = user;
            }


            db.todolist.Add(t);
            await db.SaveChangesAsync();
            Console.WriteLine("funca");
            return Results.Ok();
        
    }
    catch (InvalidCastException e)
    {
        Console.WriteLine(e);
        return Results.NotFound();
    }
   
});


app.MapGet("/todolist", [Authorize(Roles = "User")] async ([FromQuery] int  page, [FromQuery] int pageSize,TodoList db,HttpContext context) => {

    int pageCount = (int)(db.todolist.Count() / pageSize);
    context.Response.Headers.Append("page",pageCount.ToString());
    
    return await db.todolist.OrderBy(o => o.id).Skip((page - 1) *(pageSize)).Take(pageSize).ToListAsync();
});
app.MapGet("/todolist/user/",[Authorize(Roles = "User")] async ([FromQuery] int page, [FromQuery] int pageSize, TodoList db, HttpContext context) => {
    int pageCount = (int)(db.todolist.Count() / pageSize);
    context.Response.Headers.Append("page", pageCount.ToString());
    return await db.todolist.Where(o=>o.UserId==Int32.Parse(context.User.FindFirst("Id").Value)).OrderBy(o => o.id).Skip((page - 1) * (pageSize)).Take(pageSize).ToListAsync();
});
app.MapGet("/user", [Authorize(Roles = "User")] async ( TodoList db) => {

    
    return await db.users.ToListAsync();
});


app.MapGet("/todolist/compelete", [Authorize(Roles = "User")] async (TodoList db) =>
await db.todolist.Where(t => t.completo==true).ToListAsync());

app.MapGet("/todolist/{id}", [Authorize(Roles = "User")] async (int id, TodoList db) =>
    {
        var todo = db.todolist.Find(id);
        if (todo is Todo)
        {
            return Results.Ok(todo);
        }else
        {
            return Results.NotFound();
        }
    }
    );
app.MapPut("/todolist/modificar/{id}", [Authorize(Roles = "User")] async (int id,todoDTO inputTodo, TodoList db)=>
{
    var todo = await db.todolist.FindAsync(id);
    if (todo is null) return Results.NotFound();
    todo.nombre =  inputTodo.nombre;
    todo.completo = inputTodo.completo;


    await db.SaveChangesAsync();
    return Results.NoContent();
});
app.MapPut("/todolist/modificar", [Authorize(Roles = "User")] async (todoDTO inputTodo, TodoList db) =>
{
    
    var todo = await db.todolist.FindAsync(inputTodo.id);
    if (todo is null) return Results.NotFound();
    todo.nombre = inputTodo.nombre;
    todo.completo = inputTodo.completo;

    Console.WriteLine("la tarea es: "+todo.completo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", [Authorize(Roles = "User")] async (int id, TodoList db) =>
{
    if (await db.todolist.FindAsync(id) is Todo todo)
    {
        db.todolist.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
});


app.Run();

