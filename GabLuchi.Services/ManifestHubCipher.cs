using System.Globalization;
using System.Text;

namespace GabLuchi.Services;

/// <summary>
/// Request-id cipher for the ManifestHub manifest API. That API refuses plain application ids
/// ("Direct numeric IDs not allowed") and only serves tokens produced by this scheme:
///
///   2246340  ->  G79954751
///
/// The token is a digit substitution whose permutation table is derived from a fixed key, prefixed
/// with a length character (A = 1 digit ... G = 7 digits, so every current AppID yields a leading
/// "G") and a checksum digit. It is not a secret — the public generator page runs this exact
/// algorithm in the browser and the server simply decodes the token back into the AppID — so
/// reproducing it here lets us build download links without driving a web page.
/// </summary>
public static class ManifestHubCipher
{
	private const string SecretKey = "N4F1S_FU4D_OWN_SYSTEM_2025";

	private static readonly char[] SubTable = GenerateSubstitutionTable(SecretKey);

	/// <summary>
	/// Encodes an AppID into the token form the ManifestHub API expects. Returns an empty string
	/// when <paramref name="appid"/> is not a positive integer, which callers treat as a source miss.
	/// </summary>
	public static string Encode(string? appid)
	{
		if (!long.TryParse(appid?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out long id) || id <= 0)
		{
			return string.Empty;
		}
		string digits = id.ToString(CultureInfo.InvariantCulture);
		int sum = 0;
		foreach (char c in digits)
		{
			sum += c - '0';
		}
		StringBuilder token = new StringBuilder(digits.Length + 2);
		token.Append((char)(65 + digits.Length - 1));
		token.Append((char)('0' + (sum * 7) % 10));
		foreach (char c in digits)
		{
			token.Append(SubTable[c - '0']);
		}
		return token.ToString();
	}

	/// <summary>
	/// Builds the digit permutation the way the upstream JavaScript does: Fisher-Yates driven by an
	/// LCG whose multiply-add runs in IEEE double precision. That product (up to ~2.4e18) leaves the
	/// exact-integer range of a double and is rounded to the representable neighbour before the
	/// 32-bit mask, so the rounding has to be reproduced here or the table drifts and every token
	/// comes out wrong.
	/// </summary>
	private static char[] GenerateSubstitutionTable(string key)
	{
		char[] table = "0123456789".ToCharArray();
		long seed = 0;
		foreach (char c in key)
		{
			seed = (seed * 31 + c) & 0xFFFF;
		}
		for (int i = table.Length - 1; i > 0; i--)
		{
			double step = seed * 1103515245.0 + 12345.0;
			long truncated = (long)step;
			seed = (int)((truncated % 4294967296L) & 0x7FFFFFFF);
			int j = (int)(seed % (i + 1));
			(table[i], table[j]) = (table[j], table[i]);
		}
		return table;
	}
}
