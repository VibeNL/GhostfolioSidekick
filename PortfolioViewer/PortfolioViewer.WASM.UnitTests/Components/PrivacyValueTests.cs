using AwesomeAssertions;
using Bunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Components;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Components
{
    public class PrivacyValueTests : BunitContext
    {
        public PrivacyValueTests()
        {
            Services.AddSingleton<IPrivacyModeService, PrivacyModeService>();
        }

        [Fact]
        public void PrivacyValue_WhenPrivacyModeIsOff_RendersRealValue()
        {
            // Arrange — privacy mode is off by default

            // Act
            var cut = Render<PrivacyValue>(p => p.Add(c => c.Value, "€ 1,234.56"));

            // Assert
            cut.Markup.Should().Contain("€ 1,234.56");
            cut.Markup.Should().NotContain("••••••");
        }

        [Fact]
        public void PrivacyValue_WhenPrivacyModeIsOn_RendersMaskedPlaceholder()
        {
            // Arrange — turn on privacy mode before rendering
            Services.GetRequiredService<IPrivacyModeService>().Toggle();

            // Act
            var cut = Render<PrivacyValue>(p => p.Add(c => c.Value, "€ 1,234.56"));

            // Assert
            cut.Markup.Should().Contain("••••••");
            cut.Markup.Should().NotContain("€ 1,234.56");
        }

        [Fact]
        public void PrivacyValue_WhenPrivacyModeIsOn_MaskedSpanHasPrivacyMaskedCssClass()
        {
            // Arrange
            Services.GetRequiredService<IPrivacyModeService>().Toggle();

            // Act
            var cut = Render<PrivacyValue>(p => p.Add(c => c.Value, "$ 500.00"));

            // Assert — span must carry the CSS class so it can be styled
            cut.Find("span.privacy-masked").Should().NotBeNull();
        }

        [Fact]
        public void PrivacyValue_TogglingPrivacyModeOn_TriggersRerender()
        {
            // Arrange
            var privacyService = Services.GetRequiredService<IPrivacyModeService>();
            var cut = Render<PrivacyValue>(p => p.Add(c => c.Value, "€ 1,234.56"));
            cut.Markup.Should().Contain("€ 1,234.56");

            // Act
            privacyService.Toggle();

            // Assert
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("••••••"),
                timeout: TimeSpan.FromSeconds(2));
            cut.Markup.Should().NotContain("€ 1,234.56");
        }

        [Fact]
        public void PrivacyValue_TogglingPrivacyModeOff_TriggersRerender()
        {
            // Arrange — start with privacy mode on
            var privacyService = Services.GetRequiredService<IPrivacyModeService>();
            privacyService.Toggle();
            var cut = Render<PrivacyValue>(p => p.Add(c => c.Value, "€ 1,234.56"));
            cut.Markup.Should().Contain("••••••");

            // Act
            privacyService.Toggle();

            // Assert
            cut.WaitForAssertion(() => cut.Markup.Should().Contain("€ 1,234.56"),
                timeout: TimeSpan.FromSeconds(2));
            cut.Markup.Should().NotContain("••••••");
        }

        [Fact]
        public void PrivacyValue_MultipleToggles_AlwaysMatchesCurrentState()
        {
            // Arrange
            var privacyService = Services.GetRequiredService<IPrivacyModeService>();
            var cut = Render<PrivacyValue>(p => p.Add(c => c.Value, "£ 999.00"));

            for (var i = 0; i < 3; i++)
            {
                // Toggle on
                privacyService.Toggle();
                cut.WaitForAssertion(() => cut.Markup.Should().Contain("••••••"),
                    timeout: TimeSpan.FromSeconds(2));

                // Toggle off
                privacyService.Toggle();
                cut.WaitForAssertion(() => cut.Markup.Should().Contain("£ 999.00"),
                    timeout: TimeSpan.FromSeconds(2));
            }
        }

        [Fact]
        public void PrivacyValue_AfterDispose_DoesNotThrowWhenServiceToggled()
        {
            // Arrange
            var privacyService = Services.GetRequiredService<IPrivacyModeService>();
            var cut = Render<PrivacyValue>(p => p.Add(c => c.Value, "€ 100.00"));

            // Act — dispose the component, simulating navigation away from the page
            cut.Dispose();

            // Assert — toggling the service must not throw (handler must be unsubscribed)
            var act = () => privacyService.Toggle();
            act.Should().NotThrow();
        }
    }
}
