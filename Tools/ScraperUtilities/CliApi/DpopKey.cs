using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CliApi
{
	/// <summary>
	/// P-256 ECDSA key used to sign DPoP proof JWTs for the Scalable Capital CLI API.
	/// Persisted as a minimal JWK ({"kty":"EC","crv":"P-256","d":...}) so login only happens once.
	/// </summary>
	public class DpopKey : IDisposable
	{
		private readonly ECDsa _key;

		private DpopKey(ECDsa key)
		{
			_key = key;
		}

		public static DpopKey Create() => new(ECDsa.Create(ECCurve.NamedCurves.nistP256));

		public static DpopKey Load(string json)
		{
			using var document = JsonDocument.Parse(json);
			var root = document.RootElement;
			if (root.GetProperty("kty").GetString() != "EC" || root.GetProperty("crv").GetString() != "P-256")
			{
				throw new InvalidOperationException("DPoP key file is not an EC/P-256 JWK.");
			}

			var dValue = root.GetProperty("d").GetString();
			if (string.IsNullOrWhiteSpace(dValue))
			{
				throw new InvalidOperationException("DPoP key file is missing the private key scalar.");
			}

			var scalar = Base64UrlDecode(dValue);
			if (scalar.Length != 32)
			{
				throw new InvalidOperationException($"Invalid DPoP private key scalar length {scalar.Length}; expected 32 bytes.");
			}

			var parameters = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = scalar };
			return new DpopKey(ECDsa.Create(parameters));
		}

		public string ToJson()
		{
			var d = _key.ExportParameters(true).D ?? throw new InvalidOperationException("ECDsa key has no private scalar.");
			return $"{{\"kty\":\"EC\",\"crv\":\"P-256\",\"d\":\"{Base64UrlEncode(d)}\"}}";
		}

		public string BuildProof(string method, Uri targetUri, string? nonce, string? accessToken)
		{
			var q = _key.ExportParameters(false).Q;
			var header = new JsonObject
			{
				["typ"] = "dpop+jwt",
				["alg"] = "ES256",
				["jwk"] = new JsonObject
				{
					["kty"] = "EC",
					["crv"] = "P-256",
					["x"] = Base64UrlEncode(FixedSize32(q.X)),
					["y"] = Base64UrlEncode(FixedSize32(q.Y))
				}
			};

			var claims = new JsonObject
			{
				["htm"] = method,
				["htu"] = targetUri.GetLeftPart(UriPartial.Path),
				["iat"] = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				["jti"] = Base64UrlEncode(RandomNumberGenerator.GetBytes(16))
			};

			if (!string.IsNullOrWhiteSpace(nonce))
			{
				claims["nonce"] = nonce;
			}

			if (!string.IsNullOrWhiteSpace(accessToken))
			{
				using var sha256 = SHA256.Create();
				var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(accessToken));
				claims["ath"] = Base64UrlEncode(hash);
			}

			var signingInput = $"{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header))}.{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(claims))}";
			using var ecdsa = ECDsa.Create(_key.ExportParameters(true));
			var signature = ecdsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256);
			return $"{signingInput}.{Base64UrlEncode(signature)}";
		}

		public void Dispose() => _key.Dispose();

		private static byte[] FixedSize32(byte[]? value)
		{
			if (value == null)
			{
				throw new InvalidOperationException("ECDsa public point coordinate is missing.");
			}

			if (value.Length == 32)
			{
				return value;
			}

			var result = new byte[32];
			Array.Copy(value, 0, result, 32 - value.Length, value.Length);
			return result;
		}

		public static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

		private static byte[] Base64UrlDecode(string value)
		{
			var base64 = value.Replace('-', '+').Replace('_', '/');
			return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
		}
	}
}
