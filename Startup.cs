using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DocSea.Startup))]
namespace DocSea
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
