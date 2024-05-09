using System.ComponentModel.DataAnnotations;

namespace HomePage
{
	public class Image
	{
		[Key]
		public int ImageId { get; set; }
		public string Title { get; set; }
		public string ImagePath { get; set; } 
	}
}
