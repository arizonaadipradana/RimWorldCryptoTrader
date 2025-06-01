using HarmonyLib;
using RimWorldCryptoTrader.Models;
using System.Reflection;
using Verse;

namespace RimWorldCryptoTrader.Core
{
    [StaticConstructorOnStartup]
    public static class CryptoTraderMod
    {
        static CryptoTraderMod()
        {
            var harmony = new Harmony("rimworld.cryptotrader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("[CryptoTrader] Mod loaded successfully. Press DEL to open trading terminal.");
        }
    }

    public class CryptoTraderGameComponent : GameComponent
    {
        public CryptoTraderGameComponent() { }
        public CryptoTraderGameComponent(Game game) { }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            if (Current.Game.GetComponent<PlayerCryptoData>() == null)
            {
                Current.Game.components.Add(new PlayerCryptoData(Current.Game));
            }
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            if (Current.Game.GetComponent<PlayerCryptoData>() == null)
            {
                Current.Game.components.Add(new PlayerCryptoData(Current.Game));
            }
        }
    }
}