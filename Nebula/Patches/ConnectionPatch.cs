using Hazel.Udp;

namespace Nebula.Patches;

[HarmonyPatch(typeof(UnityUdpClientConnection), nameof(UnityUdpClientConnection.ConnectAsync))]
public static class UnityUdpClientConnectionConnectAsyncPatch
{
    public static void Prefix(UnityUdpClientConnection __instance, Il2CppStructArray<byte> bytes)
    {
        int value = Patches.NebulaOption.configTimeoutExtension.Value;
        float rate = 1f;
        switch (value)
        {
            case 1:
                rate = 1.5f;
                break;
            case 2:
                rate = 2f;
                break;
            case 3:
                rate = 3f;
                break;
        }
        __instance.KeepAliveInterval = (int)(1000 * rate);
        __instance.DisconnectTimeoutMs = (__instance.EndPoint.Port >= 22000 && __instance.EndPoint.Port < 23000) ? 3000 : 15000;
    }
}