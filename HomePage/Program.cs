using Dapper;
using HomePage;
using Microsoft.Data.Sqlite;
using System.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IDbConnection>(_ =>
{
	var connection = new SqliteConnection("Data Source=Data/homePage.db");
	connection.Open();
	connection.Execute(@"CREATE TABLE IF NOT EXISTS ImageItems (
                            ImageId INTEGER PRIMARY KEY AUTOINCREMENT,
                            Title TEXT NOT NULL,
                            ImagePath TEXT NOT NULL
                        )");
	return connection;
}
);
builder.Services.AddAntiforgery	();
var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();

app.MapPost("/upload", async (HttpContext context ,IFormFile image,[FromForm] string title, [FromServices] IAntiforgery antiforgery, IDbConnection db) =>
{
	var tokens = antiforgery.GetAndStoreTokens(context);
	var requestToken = tokens.RequestToken;

	if (string.IsNullOrEmpty(requestToken))
	{
		return Results.BadRequest("Anti-forgery token is missing.");
	}

	if (image == null || image.Length == 0)
	{
		return Results.BadRequest("No image uploaded.");
	}

	var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
	var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
	if (!allowedExtensions.Contains(fileExtension))
	{
		return Results.BadRequest("Invalid file extension. Allowed extensions are .jpg, .jpeg, .png, and .gif.");
	}

	var imagePath = Path.Combine("wwwroot", "imagesUploaded", image.FileName);

	using (var stream = new FileStream(imagePath, FileMode.Create))
	{
		await image.CopyToAsync(stream);
	}

	var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ImageItems");

	// If the count is already 3, delete the oldest image
	if (count >= 3)
	{
		var oldestImage = await db.QueryFirstOrDefaultAsync<Image>("SELECT * FROM ImageItems ORDER BY ImageId LIMIT 1");
		File.Delete(oldestImage.ImagePath);
		await db.ExecuteAsync("DELETE FROM ImageItems WHERE ImageId = @ImageId", new { ImageId = oldestImage.ImageId });
	}

	var newImage = new Image { Title = title, ImagePath = imagePath };
	await db.ExecuteAsync("INSERT INTO ImageItems (Title, ImagePath) VALUES (@Title, @ImagePath)", newImage);

	return Results.Redirect("/");
}).DisableAntiforgery();

app.MapGet("/", async context =>
{
	try
	{
		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

			var images = await db.QueryAsync<Image>("SELECT * FROM ImageItems ORDER BY ImageId DESC LIMIT 3");

			var imagePath1 = images.ElementAtOrDefault(0)?.ImagePath;
			var imagePath2 = images.ElementAtOrDefault(1)?.ImagePath;
			var imagePath3 = images.ElementAtOrDefault(2)?.ImagePath;

			var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
			Console.WriteLine($"HTML file path: {htmlPath}");

			var htmlContent = await File.ReadAllTextAsync(htmlPath);

			var updatedHtml = htmlContent
				.Replace("{{IMAGE_1_SRC}}", imagePath1 != null ? ($"imagesUploaded/{Path.GetFileName(imagePath1)}") : "")
				.Replace("{{IMAGE_2_SRC}}", imagePath2 != null ? ($"imagesUploaded/{Path.GetFileName(imagePath2)}") : "")
				.Replace("{{IMAGE_3_SRC}}", imagePath3 != null ? ($"imagesUploaded/{Path.GetFileName(imagePath3)}") : "");

			await context.Response.WriteAsync(updatedHtml);
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"An error occurred: {ex.Message}");
		await context.Response.WriteAsync($"An error occurred: {ex.Message}");
	}
});

app.Run();



