using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Progra_03.Startup))]
namespace Progra_03
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
