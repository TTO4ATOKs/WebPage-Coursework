using WebPage_Coursework.Controllers;
using WebPage_Coursework.Repository;
using WebPage_Coursework.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddScoped<CartRepository>();

builder.Services.AddAuthentication("Cookies") 
    .AddCookie(options =>
    {
        options.Cookie.Name = "MyAppCookie"; 
        options.LoginPath = "/Account/Login"; 
        options.LogoutPath = "/Account/Logout";
    });

builder.Services.AddScoped<OrderRepository>();

builder.Services.AddScoped<OrderController>();

builder.Services.AddSingleton(new ProductRepository(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AdminRepository>(provider =>
    new AdminRepository(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Добавление MVC
builder.Services.AddControllersWithViews();

// Настройка сессий
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true; 
    options.Cookie.IsEssential = true; 
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();