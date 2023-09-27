using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace Nebula.Behaviour;

public class ConsoleTimer : MonoBehaviour
{
    public static ConsoleTimer Instance { get; private set; } = null!;
    static ConsoleTimer() => ClassInjector.RegisterTypeInIl2Cpp<ConsoleTimer>();

    private float storedUsed = 0f;
    private TextMeshPro timerText = null!;
    public ConsoleRestriction.ConsoleType? Type = null;
    public void Awake()
    {
        timerText = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, transform);
        timerText.name = "TimerText";
        TextAttribute.BoldAttr.Reflect(timerText);

        timerText.text = "";

        if (Instance) GameObject.Destroy(Instance.gameObject);
        Instance = this;
    }

    static public void MarkAsNonConsoleMinigame()
    {
        if (Instance) GameObject.Destroy(Instance.gameObject);
        Instance = null!;
    }

    static public bool IsOpenedByAvailableWay()
    {
        if (!Instance) return true;
        if(!Instance.gameObject.activeSelf) return true;
        if (Instance.Type == null) return true;
        return NebulaGameManager.Instance?.ConsoleRestriction?.CanUse(Instance.Type.Value) ?? true;
    }

    public ConsoleTimer SetUp(ConsoleRestriction.ConsoleType type)
    {
        Type = type;
        return this;
    }

    public void Update()
    {
        if (!Type.HasValue) return;

        if (!(NebulaGameManager.Instance?.ConsoleRestriction?.ShouldShowTimer(Type.Value) ?? false)) return;

        if(!AmongUsUtil.InCommSab) storedUsed += Time.deltaTime;
        if(storedUsed > 0.1f)
        {
            NebulaGameManager.Instance?.ConsoleRestriction?.Use(Type.Value, storedUsed);
            storedUsed = 0f;
        }

        float val = NebulaGameManager.Instance!.ConsoleRestriction!.GetLeftTime(Type.Value);
        if (val > 0f)
            timerText.text = string.Format("{0:0.0}", val) + "s";
        else
            timerText.text = "0s";
    }

}
