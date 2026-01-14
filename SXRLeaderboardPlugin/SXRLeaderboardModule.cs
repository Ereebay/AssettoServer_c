using AssettoServer.Server.Plugin;
using Autofac;

namespace SXRLeaderboardPlugin;

public class SXRLeaderboardModule : AssettoServerModule<SXRLeaderboardConfiguration>
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SXRLeaderboardService>().AsSelf().SingleInstance();
        builder.RegisterType<SXRLeaderboardPlugin>().AsSelf().As<IAssettoServerAutostart>().SingleInstance();
        builder.RegisterType<SXRLeaderboardController>().AsSelf();
    }
}
