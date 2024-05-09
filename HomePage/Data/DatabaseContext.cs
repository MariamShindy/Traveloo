using Microsoft.EntityFrameworkCore;

namespace HomePage.Data
{
	public class DatabaseContext :DbContext 
	{
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Data Source=Data/homePage.db");
		}
		public DbSet<Image> ImageItems { get; set; }

	}
}
