using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ZombieShooter;

// Small built-in bitmap alphabet keeps the prototype independent of installed fonts/assets.
internal static class PixelText
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:-./+ ";
    private static readonly string[] Glyphs = (
        "010101111101101 110101110101110 011100100100011 110101101101110 111100110100111 " +
        "111100110100100 011100101101011 101101111101101 111010010010111 001001001101010 " +
        "101101110101101 100100100100111 101111111101101 101111111111101 010101101101010 " +
        "110101110100100 010101101111011 110101110101101 011100010001110 111010010010010 " +
        "101101101101111 101101101101010 101101111111101 101101010101101 101101010010010 111001010100111 " +
        "111101101101111 010110010010111 110001010100111 110001010001110 101101111001001 " +
        "111100110001110 011100111101111 111001010010010 111101111101111 111101111001110 " +
        "000010000010000 000000111000000 000000000000010 001001010100100 000010111010000 000000000000000").Split(' ');

    public static void Draw(SpriteBatch batch, Texture2D pixel, string text, Vector2 position, Color color, int scale = 3)
    {
        foreach (char character in text.ToUpperInvariant())
        {
            int index = Alphabet.IndexOf(character);
            if (index >= 0)
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 3; x++)
                        if (Glyphs[index][y * 3 + x] == '1')
                            batch.Draw(pixel, new Rectangle((int)position.X + x * scale, (int)position.Y + y * scale, scale, scale), color);
            position.X += 4 * scale;
        }
    }
}
