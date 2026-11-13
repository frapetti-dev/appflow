using Cysharp.Threading.Tasks;

namespace AppFlow
{
    public static class AppFlowUniTaskExtensions
    {
        public static async UniTask<bool> TransitToUniTask(this AppFlowMachine m, AppStateRef target) => await m.TransitTo(target);
    }
}
