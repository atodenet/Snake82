using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Atode
{
	/// Refer: https://gist.github.com/libertylocked/10cfb58ebd26af285b13
	public static class Primitive
	{
		private static Texture2D pixel;

		private static void CreateThePixel(SpriteBatch spriteBatch)
		{
			pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
			pixel.SetData(new[] { Color.White });
		}

		/// <summary>
		/// Draws a filled rectangle
		/// </summary>
		/// <param name="spriteBatch">The destination drawing surface</param>
		/// <param name="rect">The rectangle to draw</param>
		/// <param name="color">The color to draw the rectangle in</param>
		public static void FillRectangle(this SpriteBatch spriteBatch, Rectangle rect, Color color)
		{
			if (pixel == null)
			{
				CreateThePixel(spriteBatch);
			}

			// Simply use the function already there
			spriteBatch.Draw(pixel, rect, color);
		}
	}
}