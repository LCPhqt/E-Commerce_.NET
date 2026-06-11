using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Auto-update product images to local files
    var localMap = new Dictionary<int, string>
    {
        { 1, "/images/iphone15.jpg" },
        { 2, "/images/macbookair.jpg" },
        { 3, "/images/airpods.jpg" },
        { 4, "/images/galaxy.jpg" },
        { 5, "/images/dellxps.jpg" },
        { 6, "/images/logitech.jpg" }
    };
    var products = db.Products.Where(p => p.Id >= 1 && p.Id <= 6).ToList();
    foreach (var p in products)
    {
        if (localMap.TryGetValue(p.Id, out var url) && p.HinhAnhUrl != url)
        {
            p.HinhAnhUrl = url;
        }
    }
    db.SaveChanges();
}

app.Run();
