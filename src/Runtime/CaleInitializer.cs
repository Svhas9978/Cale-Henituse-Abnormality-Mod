using System.Reflection;
using Harmony;
using LobotomyBaseMod;

namespace CaleHenituseAbnormality
{
    public sealed class CaleInitializer : ModInitializer
    {
        public override void OnInitialize()
        {
            base.OnInitialize();

            try
            {
                HarmonyInstance.Create("com.svhas9978.calehenituse").PatchAll(
                    Assembly.GetExecutingAssembly());

                CaleLog.Info("Harmony patches installed.");
            }
            catch (System.Exception e)
            {
                CaleLog.Exception("Initialization failed.", e);
            }
        }
    }
}
