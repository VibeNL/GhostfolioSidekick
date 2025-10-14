using AwesomeAssertions;
using System.Net;
using System.Reflection;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Controllers
{
	/// <summary>
	/// Tests for internal helper classes in ProxyController
	/// Uses reflection to test internal classes that are not publicly accessible
	/// </summary>
	public class ProxyControllerInternalHelpersTests
	{
		private readonly Type _ipNetworkType;
		private readonly Type _urlValidationResultType;

		public ProxyControllerInternalHelpersTests()
		{
			var assembly = Assembly.GetAssembly(typeof(GhostfolioSidekick.PortfolioViewer.ApiService.Controllers.ProxyController));
			_ipNetworkType = assembly!.GetType("GhostfolioSidekick.PortfolioViewer.ApiService.Controllers.IPNetwork")!;
			_urlValidationResultType = assembly.GetType("GhostfolioSidekick.PortfolioViewer.ApiService.Controllers.UrlValidationResult")!;
		}

		#region IPNetwork Tests

		[Theory]
		[InlineData("192.168.1.0/24", "192.168.1.1", true)]
		[InlineData("192.168.1.0/24", "192.168.1.255", true)]
		[InlineData("192.168.1.0/24", "192.168.2.1", false)]
		[InlineData("10.0.0.0/8", "10.255.255.255", true)]
		[InlineData("10.0.0.0/8", "11.0.0.1", false)]
		[InlineData("172.16.0.0/12", "172.31.255.255", true)]
		[InlineData("172.16.0.0/12", "172.32.0.1", false)]
		[InlineData("127.0.0.0/8", "127.0.0.1", true)]
		[InlineData("127.0.0.0/8", "127.255.255.255", true)]
		[InlineData("127.0.0.0/8", "128.0.0.1", false)]
		public void IPNetwork_Contains_IPv4_ReturnsExpectedResult(string cidr, string testIp, bool expectedResult)
		{
			// Arrange
			var parseMethod = _ipNetworkType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
			var containsMethod = _ipNetworkType.GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance);
			var ipNetwork = parseMethod!.Invoke(null, new object[] { cidr });
			var testAddress = IPAddress.Parse(testIp);

			// Act
			var result = containsMethod!.Invoke(ipNetwork, new object[] { testAddress });

			// Assert
			result.Should().Be(expectedResult);
		}

		[Theory]
		[InlineData("::1/128", "::1", true)]
		[InlineData("::1/128", "::2", false)]
		[InlineData("fc00::/7", "fc00::1", true)]
		[InlineData("fc00::/7", "fdff::1", true)]
		[InlineData("fc00::/7", "fe00::1", false)]
		[InlineData("fe80::/10", "fe80::1", true)]
		[InlineData("fe80::/10", "febf::1", true)]
		[InlineData("fe80::/10", "fec0::1", false)]
		public void IPNetwork_Contains_IPv6_ReturnsExpectedResult(string cidr, string testIp, bool expectedResult)
		{
			// Arrange
			var parseMethod = _ipNetworkType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
			var containsMethod = _ipNetworkType.GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance);
			var ipNetwork = parseMethod!.Invoke(null, new object[] { cidr });
			var testAddress = IPAddress.Parse(testIp);

			// Act
			var result = containsMethod!.Invoke(ipNetwork, new object[] { testAddress });

			// Assert
			result.Should().Be(expectedResult);
		}

		[Fact]
		public void IPNetwork_Contains_DifferentAddressFamilies_ReturnsFalse()
		{
			// Arrange
			var parseMethod = _ipNetworkType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
			var containsMethod = _ipNetworkType.GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance);
			var ipv4Network = parseMethod!.Invoke(null, new object[] { "192.168.1.0/24" });
			var ipv6Address = IPAddress.Parse("::1");

			// Act
			var result = containsMethod!.Invoke(ipv4Network, new object[] { ipv6Address });

			// Assert
			result.Should().Be(false);
		}

		[Theory]
		[InlineData("192.168.1.0/0")]   // All IPv4
		[InlineData("192.168.1.0/32")]  // Single IP
		[InlineData("10.0.0.0/16")]     // Class A subnet
		[InlineData("172.16.0.0/20")]   // Class B subnet
		public void IPNetwork_Parse_ValidCIDR_CreatesNetwork(string cidr)
		{
			// Arrange
			var parseMethod = _ipNetworkType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);

			// Act
			var result = parseMethod!.Invoke(null, new object[] { cidr });

			// Assert
			result.Should().NotBeNull();
			result!.GetType().Should().Be(_ipNetworkType);
		}

		[Theory]
		[InlineData("192.168.1.0")]     // Missing prefix length
		[InlineData("192.168.1.0/")]    // Empty prefix length
		[InlineData("invalid/24")]      // Invalid IP address
		[InlineData("")]                // Empty string
		[InlineData("/24")]             // Missing IP address
		public void IPNetwork_Parse_InvalidCIDR_ThrowsException(string invalidCidr)
		{
			// Arrange
			var parseMethod = _ipNetworkType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);

			// Act & Assert
			var exception = Assert.Throws<TargetInvocationException>(() => 
				parseMethod!.Invoke(null, new object[] { invalidCidr }));
			
			exception.InnerException.Should().NotBeNull();
		}

		[Theory]
		[InlineData("192.168.1.0/24")]
		[InlineData("10.0.0.0/8")]
		[InlineData("172.16.0.0/12")]
		[InlineData("127.0.0.0/8")]
		[InlineData("224.0.0.0/4")]
		public void IPNetwork_Parse_IPv4_HandlesDifferentPrefixLengths(string cidr)
		{
			// Arrange
			var parseMethod = _ipNetworkType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
			var containsMethod = _ipNetworkType.GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance);
			var ipNetwork = parseMethod!.Invoke(null, new object[] { cidr });

			// Act - Test that the network contains the network address itself
			var networkAddress = IPAddress.Parse(cidr.Split('/')[0]);
			var result = containsMethod!.Invoke(ipNetwork, new object[] { networkAddress });

			// Assert
			result.Should().Be(true);
		}

		[Theory]
		[InlineData("::1/128")]
		[InlineData("fc00::/7")]
		[InlineData("fe80::/10")]
		[InlineData("2001:db8::/32")]
		public void IPNetwork_Parse_IPv6_HandlesDifferentPrefixLengths(string cidr)
		{
			// Arrange
			var parseMethod = _ipNetworkType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
			var containsMethod = _ipNetworkType.GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance);
			var ipNetwork = parseMethod!.Invoke(null, new object[] { cidr });

			// Act - Test that the network contains the network address itself
			var networkAddress = IPAddress.Parse(cidr.Split('/')[0]);
			var result = containsMethod!.Invoke(ipNetwork, new object[] { networkAddress });

			// Assert
			result.Should().Be(true);
		}

		#endregion

		#region UrlValidationResult Tests

		[Fact]
		public void UrlValidationResult_DefaultConstructor_SetsDefaultValues()
		{
			// Arrange & Act
			var result = Activator.CreateInstance(_urlValidationResultType);

			// Assert
			result.Should().NotBeNull();
			
			var isValidProperty = _urlValidationResultType.GetProperty("IsValid");
			var errorMessageProperty = _urlValidationResultType.GetProperty("ErrorMessage");
			var validatedUrlProperty = _urlValidationResultType.GetProperty("ValidatedUrl");

			isValidProperty!.GetValue(result).Should().Be(false);
			errorMessageProperty!.GetValue(result).Should().Be(string.Empty);
			validatedUrlProperty!.GetValue(result).Should().BeNull();
		}

		[Fact]
		public void UrlValidationResult_Properties_CanBeSetAndRetrieved()
		{
			// Arrange
			var result = Activator.CreateInstance(_urlValidationResultType);
			var testUri = new Uri("https://example.com");
			const string testErrorMessage = "Test error message";

			var isValidProperty = _urlValidationResultType.GetProperty("IsValid");
			var errorMessageProperty = _urlValidationResultType.GetProperty("ErrorMessage");
			var validatedUrlProperty = _urlValidationResultType.GetProperty("ValidatedUrl");

			// Act
			isValidProperty!.SetValue(result, true);
			errorMessageProperty!.SetValue(result, testErrorMessage);
			validatedUrlProperty!.SetValue(result, testUri);

			// Assert
			isValidProperty.GetValue(result).Should().Be(true);
			errorMessageProperty.GetValue(result).Should().Be(testErrorMessage);
			validatedUrlProperty.GetValue(result).Should().Be(testUri);
		}

		#endregion

		#region Static Fields Tests

		[Fact]
		public void ProxyController_AllowedSchemes_ContainsExpectedSchemes()
		{
			// Arrange
			var proxyControllerType = typeof(GhostfolioSidekick.PortfolioViewer.ApiService.Controllers.ProxyController);
			var allowedSchemesField = proxyControllerType.GetField("AllowedSchemes", BindingFlags.NonPublic | BindingFlags.Static);

			// Act
			var allowedSchemes = (HashSet<string>)allowedSchemesField!.GetValue(null)!;

			// Assert
			allowedSchemes.Should().NotBeNull();
			allowedSchemes.Should().Contain("http");
			allowedSchemes.Should().Contain("https");
			allowedSchemes.Should().HaveCount(2);
		}

		[Fact]
		public void ProxyController_BlockedNetworks_ContainsExpectedNetworks()
		{
			// Arrange
			var proxyControllerType = typeof(GhostfolioSidekick.PortfolioViewer.ApiService.Controllers.ProxyController);
			var blockedNetworksField = proxyControllerType.GetField("BlockedNetworks", BindingFlags.NonPublic | BindingFlags.Static);

			// Act
			var blockedNetworks = (System.Collections.IList)blockedNetworksField!.GetValue(null)!;

			// Assert
			blockedNetworks.Should().NotBeNull();
			blockedNetworks.Count.Should().Be(9); // Based on the networks defined in the controller

			// We can't easily test the actual network objects without more complex reflection,
			// but we can verify the count and that it's not empty
		}

		#endregion

		#region Helper Method Tests for Text Processing

		[Fact]
		public void ProxyController_WhitespaceRegex_MatchesMultipleSpaces()
		{
			// Arrange
			var proxyControllerType = typeof(GhostfolioSidekick.PortfolioViewer.ApiService.Controllers.ProxyController);
			var whitespaceRegexMethod = proxyControllerType.GetMethod("WhitespaceRegex", BindingFlags.NonPublic | BindingFlags.Static);

			// Act
			var regex = whitespaceRegexMethod!.Invoke(null, null) as System.Text.RegularExpressions.Regex;

			// Assert
			regex.Should().NotBeNull();
			regex!.IsMatch("  ").Should().BeTrue();
			regex.IsMatch("\t").Should().BeTrue();
			regex.IsMatch("\n").Should().BeTrue();
			regex.IsMatch("   \t  \n  ").Should().BeTrue();
			regex.IsMatch("a").Should().BeFalse();
		}

		[Fact]
		public void ProxyController_NewlineRegex_MatchesMultipleNewlines()
		{
			// Arrange
			var proxyControllerType = typeof(GhostfolioSidekick.PortfolioViewer.ApiService.Controllers.ProxyController);
			var newlineRegexMethod = proxyControllerType.GetMethod("NewlineRegex", BindingFlags.NonPublic | BindingFlags.Static);

			// Act
			var regex = newlineRegexMethod!.Invoke(null, null) as System.Text.RegularExpressions.Regex;

			// Assert
			regex.Should().NotBeNull();
			regex!.IsMatch("\n\n").Should().BeTrue();
			regex.IsMatch("\n  \n").Should().BeTrue();
			regex.IsMatch("\n\t\n").Should().BeTrue();
			regex.IsMatch("\n").Should().BeFalse();
			regex.IsMatch("text").Should().BeFalse();
		}

		#endregion
	}
}