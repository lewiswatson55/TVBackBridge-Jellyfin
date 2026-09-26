using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.VidaaBack;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;

    public bool DebugMode { get; set; }

    public int BackKeyCode { get; set; } = 8;

    public string BackKeyName { get; set; } = "Backspace";

    public string UserAgentKeywords { get; set; } = "vidaa, hisense, toshiba";
}

public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "TV Back Bridge";

    public override string Description => "Maps a TV remote Back key to Jellyfin Web navigation. Tested with a Toshiba VIDAA remote; supports configurable key mapping and an on-screen key viewer.";

    public override Guid Id => Guid.Parse("50a91f83-3527-4dc6-839a-66575393aaed");

    public static Plugin? Instance { get; private set; }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }
}
