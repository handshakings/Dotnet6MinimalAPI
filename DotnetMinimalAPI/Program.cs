using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//connection with sql
string conString = builder.Configuration.GetSection("ConnectionString").Value;
builder.Services.AddDbContext<UserDb>(options => options.UseSqlServer(conString));

//dependency injection of repositories
builder.Services.AddTransient<UserDb>();
builder.Services.AddTransient<UserRepository>();

//reading configuration and environment variables
var fromConfiguration = builder.Configuration["HelloKey"] ?? "Hello";

//reading environment variables. Basically there r 3 envirionments: Development, Staging, Production
var environmentFromLaunchingSetting = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "No Environment";

var app = builder.Build();

//setting url. It will override launchsetting.json
//app.Urls.Add("https://loclhost:3000/user");
//app.Urls.Add("https://loclhost:3001");
//app.Urls.Add("https://loclhost:3002");

app.MapGet("/user", async ([FromServices] UserRepository userRepo) => await userRepo.GetAllAsync());
app.MapGet("/user/{id}", async ([FromServices] UserRepository userRepo, int id) =>
{
    var user = await userRepo.GetById(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});
app.MapPost("/user", async ([FromServices] UserRepository userRepo, User user) =>
{
    await userRepo.Create(user);
    return Results.Created(@"/user/{user.Id}", user); 
});
app.MapPut("/user/{id}", async ([FromServices] UserRepository userRepo, User user, int id) =>
 {
     var userUpdated = await userRepo.Update(user, id);
     if(userUpdated is null)
     {
         return Results.NotFound();
     }
     return Results.Created(@"/user/{user.Id}", user.Id);
 });
app.MapDelete("/user/{id}", ([FromServices] UserRepository userRepo, int id) => userRepo.Delete(id));

app.Run();
  


#nullable disable
class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Country { get; set; }
}
class UserDb : DbContext
{
    public UserDb(DbContextOptions<UserDb> options):base(options){}
    public DbSet<User> Users { get; set; }
}
class UserRepository
{
    private readonly UserDb db;
    public UserRepository(UserDb db)
    {
        this.db = db;
    }
    public async Task<List<User>> GetAllAsync() => await db.Users.ToListAsync();

    public async Task<User> GetById(int id) => await db.Users.FindAsync(id);

    public async Task Create(User user)
    {
        if (user is null)
        {
            return;
        }
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }

    public async Task<User> Update(User userToUpdate, int id)
    {
        var user = await db.Users.FindAsync(id);
        if(user is null)
        {
            return null;
        }
        user.UserName = userToUpdate.UserName;
        user.Country = userToUpdate.Country;
        await db.SaveChangesAsync();
        return user;
    }
    public void Delete(int id)
    {
        var user =  db.Users.Find(id);
        if(user is null) { return; }
        db.Users.Remove(user);
        db.SaveChangesAsync();
    }
}